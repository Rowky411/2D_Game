using UnityEngine;
using UnityEngine.InputSystem;

// Test keys, editor and development builds only:
// K = take 1 damage, H = heal 1, M = spend 10 mana, X = gain 25 EXP.
public class PlayerDebugKeys : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private PlayerHealth health;
    private PlayerMana mana;
    private PlayerExperience experience;
    private readonly bool[] held = new bool[4];

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        mana = GetComponent<PlayerMana>();
        experience = GetComponent<PlayerExperience>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (Pressed(0, keyboard.kKey) && health != null) health.TakeDamage(1);
        if (Pressed(1, keyboard.hKey) && health != null) health.Heal(1);
        if (Pressed(2, keyboard.mKey) && mana != null) mana.TrySpendMana(10);
        if (Pressed(3, keyboard.xKey) && experience != null) experience.AddExp(25);
    }

    // Tracks key state manually instead of wasPressedThisFrame so simulated input is caught too.
    private bool Pressed(int index, UnityEngine.InputSystem.Controls.KeyControl key)
    {
        bool down = key.isPressed;
        bool pressed = down && !held[index];
        held[index] = down;
        return pressed;
    }
#endif
}
