using System.Collections.Generic;
using UnityEngine;

// A flying spell: moves in a straight line, loops its animation, damages mobs it touches
// and breaks on walls. Piercing projectiles hit each mob once and keep going.
[RequireComponent(typeof(SpriteRenderer))]
public class SpellProjectile : MonoBehaviour
{
    private static readonly Collider2D[] overlaps = new Collider2D[16];

    private readonly HashSet<Mob> hitMobs = new HashSet<Mob>();
    private Spell spell;
    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private float travelled;
    private float age;
    private int passThroughLayers;

    public static void Spawn(Spell spell, Vector2 position, Vector2 direction, SpriteRenderer caster)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - spell.spriteAngle;
        var go = new GameObject(spell.name, typeof(SpriteRenderer));
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));

        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = caster.sharedMaterial;
        renderer.sortingLayerID = caster.sortingLayerID;
        renderer.sortingOrder = caster.sortingOrder + 2;

        var projectile = go.AddComponent<SpellProjectile>();
        projectile.spell = spell;
        projectile.spriteRenderer = renderer;
        projectile.direction = direction.normalized;
        // Spells fly over water; only walls and solid props stop them.
        projectile.passThroughLayers = LayerMask.GetMask("Water");
        projectile.Animate();
    }

    private void Update()
    {
        age += Time.deltaTime;
        Animate();

        float step = spell.speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        travelled += step;

        if (HitSomething() || travelled >= spell.range) Destroy(gameObject);
    }

    private void Animate()
    {
        if (spell.frames == null || spell.frames.Length == 0) return;
        int frame = Mathf.FloorToInt(age * spell.framesPerSecond) % spell.frames.Length;
        spriteRenderer.sprite = spell.frames[frame];
    }

    // True when the projectile is used up.
    private bool HitSomething()
    {
        int count = Physics2D.OverlapCircle(transform.position, spell.hitRadius, ContactFilter2D.noFilter, overlaps);
        for (int i = 0; i < count; i++)
        {
            var other = overlaps[i];
            var mob = other.GetComponentInParent<Mob>();
            if (mob != null)
            {
                if (!hitMobs.Add(mob)) continue;
                mob.TakeDamage(spell.damage, direction * spell.knockback);
                if (!spell.pierce) return true;
            }
            else if (!other.isTrigger
                && (passThroughLayers & (1 << other.gameObject.layer)) == 0
                && other.GetComponentInParent<PlayerHealth>() == null)
            {
                return true;
            }
        }
        return false;
    }
}
