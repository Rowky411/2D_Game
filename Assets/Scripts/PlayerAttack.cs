using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Sword attack: Space, J or left mouse button swings the big sword in the facing direction
// and damages every mob in front of the player once per swing.
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float swingDuration = 0.18f;
    [SerializeField] private float swingArc = 140f;
    [SerializeField] private float reach = 0.75f;
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private float knockback = 6f;

    [Header("Visuals")]
    [Tooltip("Sword sprite renderer on a child object; its sprite should point up with the pivot at the hilt.")]
    [SerializeField] private SpriteRenderer sword;
    [Tooltip("Attack poses: down, up, left, right.")]
    [SerializeField] private Sprite[] attackPoses = new Sprite[4];
    [Tooltip("Frames played where a hit lands.")]
    [SerializeField] private Sprite[] hitEffectFrames;
    [SerializeField] private float hitEffectFps = 20f;

    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth health;
    private readonly HashSet<Mob> hitThisSwing = new HashSet<Mob>();
    private readonly Collider2D[] overlaps = new Collider2D[16];
    private float nextAttackTime;
    private bool swinging;
    private int facing; // 0 down, 1 up, 2 left, 3 right
    private bool held;

    public bool IsAttacking => swinging;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<PlayerHealth>();
        if (sword != null) sword.enabled = false;
    }

    private void Update()
    {
        // Tracks button state manually instead of wasPressedThisFrame so simulated input is caught too.
        bool down = (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.jKey.isPressed))
            || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        bool pressed = down && !held;
        held = down;

        if (pressed && !swinging && Time.time >= nextAttackTime && (health == null || !health.IsDead))
            StartCoroutine(Swing());
    }

    // Runs after the Animator so the attack pose overrides the walk/idle frame.
    private void LateUpdate()
    {
        if (swinging && attackPoses[facing] != null) spriteRenderer.sprite = attackPoses[facing];
    }

    private void OnDisable()
    {
        swinging = false;
        if (sword != null) sword.enabled = false;
    }

    private IEnumerator Swing()
    {
        swinging = true;
        nextAttackTime = Time.time + cooldown;
        facing = CurrentFacing();
        hitThisSwing.Clear();

        Vector2 direction = FacingVector(facing);
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        if (sword != null)
        {
            sword.enabled = true;
            // Behind the player when facing away from the camera.
            sword.sortingOrder = spriteRenderer.sortingOrder + (facing == 1 ? -1 : 1);
        }

        for (float t = 0f; t < swingDuration; t += Time.deltaTime)
        {
            if (sword != null)
            {
                float angle = baseAngle + Mathf.Lerp(swingArc * 0.5f, -swingArc * 0.5f, t / swingDuration);
                sword.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            HitMobs(direction);
            yield return null;
        }

        if (sword != null) sword.enabled = false;
        swinging = false;
    }

    private void HitMobs(Vector2 direction)
    {
        Vector2 center = (Vector2)transform.position + direction * reach;
        int count = Physics2D.OverlapCircle(center, hitRadius, ContactFilter2D.noFilter, overlaps);
        for (int i = 0; i < count; i++)
        {
            var mob = overlaps[i].GetComponentInParent<Mob>();
            if (mob == null || !hitThisSwing.Add(mob)) continue;

            Vector2 push = ((Vector2)mob.transform.position - (Vector2)transform.position).normalized * knockback;
            PlayHitEffect(mob.transform.position);
            mob.TakeDamage(damage, push);
        }
    }

    private void PlayHitEffect(Vector3 position)
    {
        if (hitEffectFrames == null || hitEffectFrames.Length == 0) return;
        var effect = new GameObject("HitEffect", typeof(SpriteRenderer));
        effect.transform.position = position;
        var renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        StartCoroutine(AnimateEffect(renderer));
    }

    private IEnumerator AnimateEffect(SpriteRenderer renderer)
    {
        foreach (var frame in hitEffectFrames)
        {
            renderer.sprite = frame;
            yield return new WaitForSeconds(1f / hitEffectFps);
        }
        Destroy(renderer.gameObject);
    }

    private int CurrentFacing()
    {
        float x = animator.GetFloat(MoveX), y = animator.GetFloat(MoveY);
        if (Mathf.Abs(x) > Mathf.Abs(y)) return x > 0 ? 3 : 2;
        return y > 0 ? 1 : 0;
    }

    private static Vector2 FacingVector(int facing)
    {
        switch (facing)
        {
            case 1: return Vector2.up;
            case 2: return Vector2.left;
            case 3: return Vector2.right;
            default: return Vector2.down;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * reach, hitRadius);
    }
}
