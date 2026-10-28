using System;
using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    [SerializeField] private int maxMana = 50;
    [SerializeField] private float regenPerSecond = 2f;

    public int MaxMana => maxMana;
    public int CurrentMana => Mathf.FloorToInt(mana);

    // Raised with (current, max) whenever the displayed mana value changes.
    public event Action<int, int> ManaChanged;

    private float mana;
    private PlayerHealth health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        mana = maxMana;
    }

    private void Update()
    {
        if (mana >= maxMana || (health != null && health.IsDead)) return;

        int before = CurrentMana;
        mana = Mathf.Min(mana + regenPerSecond * Time.deltaTime, maxMana);
        if (CurrentMana != before) ManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public bool TrySpendMana(int amount)
    {
        if (amount <= 0 || CurrentMana < amount) return false;

        mana -= amount;
        ManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;

        mana = Mathf.Min(mana + amount, maxMana);
        ManaChanged?.Invoke(CurrentMana, maxMana);
    }

    public void IncreaseMaxMana(int amount, bool refill)
    {
        maxMana += amount;
        if (refill) mana = maxMana;
        ManaChanged?.Invoke(CurrentMana, maxMana);
    }
}
