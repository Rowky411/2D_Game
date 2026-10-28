using System;
using UnityEngine;
using UnityEngine.UI;

// HUD spell slots: icon, hotkey, mana cost and a cooldown sweep. A slot dims while the
// player can't afford it and flashes red when a cast fails for lack of mana.
public class SpellBar : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public Image frame;
        public Image icon;
        public Image cooldown;
        public Text key;
        public Text cost;
    }

    [SerializeField] private PlayerSpellcaster caster;
    [SerializeField] private Slot[] slots;
    [SerializeField] private Color frameColor = new Color(0.1f, 0.08f, 0.12f, 0.9f);
    [SerializeField] private Color failColor = new Color(0.8f, 0.15f, 0.15f, 0.95f);
    [SerializeField] private Color unaffordableTint = new Color(0.45f, 0.45f, 0.5f, 1f);
    [SerializeField] private float failFlashTime = 0.25f;

    private float[] flashUntil;

    private void Start()
    {
        flashUntil = new float[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var spell = i < caster.SlotCount ? caster.GetSpell(i) : null;
            slots[i].icon.enabled = spell != null && spell.icon != null;
            if (spell != null) slots[i].icon.sprite = spell.icon;
            slots[i].key.text = i < caster.SlotCount ? KeyLabel(caster.GetKey(i).ToString()) : "";
            slots[i].cost.text = spell != null ? spell.manaCost.ToString() : "";
        }
        caster.NotEnoughMana += Flash;
    }

    private void OnDestroy()
    {
        if (caster != null) caster.NotEnoughMana -= Flash;
    }

    private void Update()
    {
        for (int i = 0; i < slots.Length && i < caster.SlotCount; i++)
        {
            slots[i].cooldown.fillAmount = caster.CooldownFraction(i);
            slots[i].icon.color = caster.CanAfford(i) ? Color.white : unaffordableTint;
            slots[i].frame.color = Time.time < flashUntil[i] ? failColor : frameColor;
        }
    }

    private void Flash(int slot)
    {
        if (slot < flashUntil.Length) flashUntil[slot] = Time.time + failFlashTime;
    }

    // "Digit1" -> "1".
    private static string KeyLabel(string key) => key.StartsWith("Digit") ? key.Substring(5) : key;
}
