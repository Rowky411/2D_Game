using System.Collections.Generic;
using UnityEngine;

// A villager the player can talk to. Strolls around its spot, and stops to face the player
// while talking. The first conversation plays `lines`; later ones play one of `repeatLines`
// at random (or `lines` again if there are none).
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Npc : MonoBehaviour
{
    [SerializeField] private string displayName = "Villager";
    [SerializeField] private Sprite portrait;
    [TextArea] [SerializeField] private string[] lines;
    [TextArea] [SerializeField] private string[] repeatLines;
    [Tooltip("Fully restores the player's health and mana after each conversation.")]
    [SerializeField] private bool restoresPlayer;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 0.8f;
    [Tooltip("How far the NPC strolls from its spot; 0 keeps it standing still.")]
    [SerializeField] private float wanderRadius = 1.5f;

    [Header("Animation")]
    [Tooltip("Sprite sheet cells, row-major: 4 walk frames (rows) x 4 directions (down, up, left, right).")]
    [SerializeField] private Sprite[] frames = new Sprite[16];
    [SerializeField] private float framesPerSecond = 6f;
    [Tooltip("Shown above the head while the player is close enough to talk.")]
    [SerializeField] private Transform talkPrompt;
    [SerializeField] private float promptHeight = 0.85f;

    // Every NPC in the scene, for the player's interaction search.
    public static readonly List<Npc> All = new List<Npc>();

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Vector2 home;
    private Vector2 wanderTarget;
    private float nextWanderTime;
    private int facing; // 0 down, 1 up, 2 left, 3 right
    private float animTime;
    private bool talking;
    private int conversations;

    public string DisplayName => displayName;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        home = wanderTarget = transform.position;
        nextWanderTime = Time.time + Random.Range(0f, 3f);
        SetPromptVisible(false);
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);

    private void FixedUpdate()
    {
        if (talking || wanderRadius <= 0f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time >= nextWanderTime)
        {
            // Pause between strolls more often than not, so villagers don't pace endlessly.
            wanderTarget = Random.value < 0.6f ? home + Random.insideUnitCircle * wanderRadius : body.position;
            nextWanderTime = Time.time + Random.Range(2f, 5f);
        }
        Vector2 toTarget = wanderTarget - body.position;
        body.linearVelocity = toTarget.sqrMagnitude > 0.02f ? toTarget.normalized * walkSpeed : Vector2.zero;
    }

    private void Update()
    {
        Vector2 velocity = body.linearVelocity;
        if (velocity.sqrMagnitude > 0.01f)
        {
            facing = FacingFor(velocity);
            animTime += Time.deltaTime;
        }
        else
        {
            animTime = 0f;
        }

        int frame = Mathf.FloorToInt(animTime * framesPerSecond) % 4;
        var sprite = frames[frame * 4 + facing];
        if (sprite != null) spriteRenderer.sprite = sprite;

        if (talkPrompt != null && talkPrompt.gameObject.activeSelf)
            talkPrompt.localPosition = new Vector3(0f, promptHeight + Mathf.Sin(Time.time * 5f) * 0.05f, 0f);
    }

    public void SetPromptVisible(bool visible)
    {
        if (talkPrompt != null) talkPrompt.gameObject.SetActive(visible);
    }

    public void Talk(Transform player)
    {
        if (talking || DialogueBox.Instance == null) return;

        talking = true;
        facing = FacingFor(player.position - transform.position);
        string[] conversation = conversations > 0 && repeatLines != null && repeatLines.Length > 0
            ? new[] { repeatLines[Random.Range(0, repeatLines.Length)] }
            : lines;
        conversations++;
        DialogueBox.Instance.Show(displayName, portrait, conversation, () => FinishTalking(player));
    }

    private void FinishTalking(Transform player)
    {
        talking = false;
        nextWanderTime = Time.time + Random.Range(1f, 3f);
        if (!restoresPlayer || player == null) return;

        var health = player.GetComponent<PlayerHealth>();
        var mana = player.GetComponent<PlayerMana>();
        if (health != null) health.Heal(health.MaxHealth);
        if (mana != null) mana.RestoreMana(mana.MaxMana);
    }

    private static int FacingFor(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x > 0 ? 3 : 2;
        return direction.y > 0 ? 1 : 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)home : transform.position, wanderRadius);
    }
}
