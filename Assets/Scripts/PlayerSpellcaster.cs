using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Spell casting: keys 1-3 cast the spell in the matching slot towards the mouse cursor
// (the player's aim direction), paid for with mana. Each slot has its own cooldown.
[RequireComponent(typeof(PlayerController), typeof(SpriteRenderer), typeof(PlayerMana))]
public class PlayerSpellcaster : MonoBehaviour
{
    [SerializeField] private Spell[] spells = new Spell[3];
    [SerializeField] private Key[] keys = { Key.Digit1, Key.Digit2, Key.Digit3 };
    [Tooltip("Where spells start, relative to the player's pivot (roughly the chest).")]
    [SerializeField] private Vector2 castOrigin = new Vector2(0f, 0.1f);

    // Raised with the slot index after a successful cast / when a cast fails for lack of mana.
    public event Action<int> Cast;
    public event Action<int> NotEnoughMana;

    private PlayerController controller;
    private SpriteRenderer spriteRenderer;
    private PlayerMana mana;
    private PlayerHealth health;
    private float[] readyTime;
    private bool[] held;

    public int SlotCount => spells.Length;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mana = GetComponent<PlayerMana>();
        health = GetComponent<PlayerHealth>();
        readyTime = new float[spells.Length];
        held = new bool[spells.Length];
    }

    // Treat the keys as already held, so a key still down from a dialogue doesn't cast.
    private void OnEnable()
    {
        for (int i = 0; i < held.Length; i++) held[i] = true;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 0; i < spells.Length && i < keys.Length; i++)
        {
            // Tracks key state manually instead of wasPressedThisFrame so simulated input is caught too.
            bool down = keyboard[keys[i]].isPressed;
            bool pressed = down && !held[i];
            held[i] = down;
            if (pressed) TryCast(i);
        }
    }

    public Spell GetSpell(int slot) => spells[slot];
    public Key GetKey(int slot) => keys[slot];
    public bool CanAfford(int slot) => spells[slot] != null && mana.CurrentMana >= spells[slot].manaCost;

    // 1 right after casting, 0 when ready.
    public float CooldownFraction(int slot)
    {
        var spell = spells[slot];
        if (spell == null || spell.cooldown <= 0f) return 0f;
        return Mathf.Clamp01((readyTime[slot] - Time.time) / spell.cooldown);
    }

    public bool TryCast(int slot)
    {
        var spell = spells[slot];
        if (spell == null || Time.time < readyTime[slot] || (health != null && health.IsDead)) return false;
        if (!mana.TrySpendMana(spell.manaCost))
        {
            NotEnoughMana?.Invoke(slot);
            return false;
        }

        readyTime[slot] = Time.time + spell.cooldown;
        Vector2 facing = controller.AimDirection;
        Vector2 origin = (Vector2)transform.position + castOrigin;
        for (int i = 0; i < spell.count; i++)
        {
            Vector2 direction = spell.Direction(i, facing);
            SpellProjectile.Spawn(spell, origin + direction * spell.spawnOffset, direction, spriteRenderer);
        }
        Cast?.Invoke(slot);
        return true;
    }
}
