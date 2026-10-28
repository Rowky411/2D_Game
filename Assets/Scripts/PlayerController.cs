using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintMultiplier = 1.7f;
    [SerializeField] private ParticleSystem sprintDust;
    [Tooltip("Face the mouse cursor instead of the movement direction.")]
    [SerializeField] private bool faceMouse = true;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");

    private Rigidbody2D body;
    private Animator animator;
    private Vector2 input;
    private bool sprinting;
    private Camera mainCamera;

    // Unit vector the player is looking along: towards the mouse, or the last move direction.
    public Vector2 AimDirection { get; private set; } = Vector2.down;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
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

        // Facing drives the walk/idle direction; without a mouse aim it keeps the last move direction.
        if (faceMouse && TryGetMouseAim(out Vector2 aim)) AimDirection = aim;
        else if (input != Vector2.zero) AimDirection = input.normalized;
        // The sprites have 8 directions; face the one nearest the aim.
        float aimAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        float facingAngle = Mathf.Round(aimAngle / 45f) * 45f;
        animator.SetFloat(MoveX, Mathf.Cos(facingAngle * Mathf.Deg2Rad));
        animator.SetFloat(MoveY, Mathf.Sin(facingAngle * Mathf.Deg2Rad));
        animator.SetFloat(Speed, input.sqrMagnitude);
        SetSprinting(sprintHeld && input != Vector2.zero);
    }

    private void FixedUpdate()
    {
        body.linearVelocity = input * (sprinting ? moveSpeed * sprintMultiplier : moveSpeed);
    }

    private void OnDisable()
    {
        // Stop in place (e.g. while talking) rather than sliding with the last velocity.
        input = Vector2.zero;
        body.linearVelocity = Vector2.zero;
        if (animator.isActiveAndEnabled) animator.SetFloat(Speed, 0f);
        SetSprinting(false);
    }

    private bool TryGetMouseAim(out Vector2 aim)
    {
        aim = default;
        if (Mouse.current == null) return false;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 toMouse = (Vector2)mainCamera.ScreenToWorldPoint(screen) - (Vector2)transform.position;
        // Cursor right on top of the player: keep the current facing instead of flickering.
        if (toMouse.sqrMagnitude < 0.04f) return false;

        aim = toMouse.normalized;
        return true;
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
