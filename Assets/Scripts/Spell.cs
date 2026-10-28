using UnityEngine;

// Data for one castable spell. Every spell fires `count` projectiles: in a fan around the
// facing direction, or evenly spaced around the caster when `ring` is set.
[CreateAssetMenu(menuName = "Spells/Spell")]
public class Spell : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public int manaCost = 10;
    public float cooldown = 1f;

    [Header("Projectiles")]
    public int count = 1;
    public bool ring;
    [Tooltip("Total fan angle in degrees when count > 1 and not a ring.")]
    public float spread;
    [Tooltip("Distance from the caster where projectiles appear.")]
    public float spawnOffset = 0.5f;
    public float speed = 8f;
    public float range = 6f;
    public float hitRadius = 0.3f;
    [Tooltip("Keeps flying through mobs, hitting each one once.")]
    public bool pierce;

    [Header("Damage")]
    public int damage = 1;
    public float knockback = 4f;

    [Header("Visuals")]
    public Sprite[] frames;
    public float framesPerSecond = 12f;
    [Tooltip("Direction the sprite art points, in degrees (0 = right, 90 = up).")]
    public float spriteAngle;

    // Flight direction of projectile `index` when cast towards `facing`.
    public Vector2 Direction(int index, Vector2 facing)
    {
        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        if (ring) angle += 360f * index / count;
        else if (count > 1) angle += Mathf.Lerp(-spread * 0.5f, spread * 0.5f, (float)index / (count - 1));

        float radians = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }
}
