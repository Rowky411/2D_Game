using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintMultiplier = 1.7f;
    [SerializeField] private ParticleSystem sprintDust;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");

    private Rigidbody2D body;
    private Animator animator;
    private Vector2 input;
    private bool sprinting;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        input = Vector2.zero;
        bool sprintHeld = false;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            sprintHeld = keyboard.shiftKey.isPressed;
        }
        input = Vector2.ClampMagnitude(input, 1f);

        // Keep last facing direction when idle so the idle pose matches.
        if (input != Vector2.zero)
        {
            animator.SetFloat(MoveX, input.x);
            animator.SetFloat(MoveY, input.y);
        }
        animator.SetFloat(Speed, input.sqrMagnitude);
        SetSprinting(sprintHeld && input != Vector2.zero);
    }

    private void FixedUpdate()
    {
        body.linearVelocity = input * (sprinting ? moveSpeed * sprintMultiplier : moveSpeed);
    }

    private void OnDisable()
    {
        input = Vector2.zero;
        SetSprinting(false);
    }

    private void SetSprinting(bool value)
    {
        sprinting = value;
        if (animator.isActiveAndEnabled) animator.SetBool(IsSprinting, value);
        if (sprintDust == null) return;

        var emission = sprintDust.emission;
        emission.enabled = value;
    }
}
