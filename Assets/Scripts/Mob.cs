using System;
using UnityEngine;

// A monster that wanders around its spawn point, chases the player when they come close
// and hurts them on contact. Wanders back home when the player leaves its territory.
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Mob : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private int expReward = 10;
    [SerializeField] private float stunTime = 0.25f;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float chaseSpeed = 2.2f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float aggroRange = 4f;
    [Tooltip("Stops chasing once the player is this far from the mob's home.")]
    [SerializeField] private float leashRange = 9f;

    [Header("Animation")]
    [Tooltip("Sprite sheet cells, row-major: 4 walk frames (rows) x 4 directions (down, up, left, right).")]
    [SerializeField] private Sprite[] frames = new Sprite[16];
    [SerializeField] private float framesPerSecond = 6f;
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f);

    public event Action<Mob> Died;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth player;
    private Vector2 home;
    private Vector2 wanderTarget;
    private float nextWanderTime;
    private float hurtUntil;
    private float stunnedUntil;
    private int health;
    private int facing; // 0 down, 1 up, 2 left, 3 right
    private float animTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        home = wanderTarget = transform.position;
        health = maxHealth;
        nextWanderTime = Time.time + UnityEngine.Random.Range(0f, 2f);
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
    }

    private void FixedUpdate()
    {
        // Knocked back: let the push play out.
        if (Time.time < stunnedUntil) return;

        Vector2 position = body.position;
        Vector2 velocity;

        if (IsChasing())
        {
            velocity = ((Vector2)player.transform.position - position).normalized * chaseSpeed;
            wanderTarget = position;
        }
        else
        {
            if (Time.time >= nextWanderTime)
            {
                wanderTarget = home + UnityEngine.Random.insideUnitCircle * wanderRadius;
                nextWanderTime = Time.time + UnityEngine.Random.Range(2f, 4f);
            }
            Vector2 toTarget = wanderTarget - position;
            velocity = toTarget.sqrMagnitude > 0.05f ? toTarget.normalized * wanderSpeed : Vector2.zero;
        }

        body.linearVelocity = velocity;
    }

    private void Update()
    {
        Vector2 velocity = body.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.01f;
        if (moving)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y)) facing = velocity.x > 0 ? 3 : 2;
            else facing = velocity.y > 0 ? 1 : 0;
            animTime += Time.deltaTime;
        }
        else
        {
            animTime = 0f;
        }

        int frame = Mathf.FloorToInt(animTime * framesPerSecond) % 4;
        var sprite = frames[frame * 4 + facing];
        if (sprite != null) spriteRenderer.sprite = sprite;
        spriteRenderer.color = Time.time < hurtUntil ? hurtColor : Color.white;
    }

    private bool IsChasing()
    {
        if (player == null || player.IsDead) return false;
        Vector2 playerPos = player.transform.position;
        return Vector2.Distance(playerPos, body.position) <= aggroRange
            && Vector2.Distance(playerPos, home) <= leashRange;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < stunnedUntil) return;
        var target = collision.collider.GetComponent<PlayerHealth>();
        if (target != null) target.TakeDamage(contactDamage);
    }

    public void TakeDamage(int amount, Vector2 knockback = default)
    {
        if (amount <= 0 || health <= 0) return;

        health -= amount;
        hurtUntil = Time.time + 0.1f;
        if (health > 0)
        {
            stunnedUntil = Time.time + stunTime;
            body.linearVelocity = knockback;
            return;
        }

        if (player != null)
        {
            var experience = player.GetComponent<PlayerExperience>();
            if (experience != null) experience.AddExp(expReward);
        }
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)home : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}
