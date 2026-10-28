using System;
using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField] private int baseExpToLevel = 100;
    [SerializeField] private int extraExpPerLevel = 50;

    [Header("Level-up rewards")]
    [SerializeField] private int healthPerLevel = 2;
    [SerializeField] private int manaPerLevel = 5;

    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; }
    public int ExpToNextLevel => baseExpToLevel + (Level - 1) * extraExpPerLevel;

    // Raised with (currentExp, expToNextLevel).
    public event Action<int, int> ExpChanged;
    public event Action<int> LeveledUp;

    private PlayerHealth health;
    private PlayerMana mana;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        mana = GetComponent<PlayerMana>();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;
        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
            if (health != null) health.IncreaseMaxHealth(healthPerLevel, refill: true);
            if (mana != null) mana.IncreaseMaxMana(manaPerLevel, refill: true);
            LeveledUp?.Invoke(Level);
        }
        ExpChanged?.Invoke(CurrentExp, ExpToNextLevel);
    }
}
