using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerMana mana;
    [SerializeField] private PlayerExperience experience;

    [SerializeField] private StatBar healthBar;
    [SerializeField] private StatBar manaBar;
    [SerializeField] private StatBar expBar;
    [SerializeField] private Text levelLabel;
    [SerializeField] private GameObject reviveHint;

    // Subscribe in Start so the player components have run Awake.
    private void Start()
    {
        health.HealthChanged += healthBar.SetValue;
        health.Died += ShowReviveHint;
        health.ReviveStarted += HideReviveHint;
        mana.ManaChanged += manaBar.SetValue;
        experience.ExpChanged += expBar.SetValue;
        experience.LeveledUp += SetLevel;

        healthBar.SetValue(health.CurrentHealth, health.MaxHealth);
        manaBar.SetValue(mana.CurrentMana, mana.MaxMana);
        expBar.SetValue(experience.CurrentExp, experience.ExpToNextLevel);
        SetLevel(experience.Level);
        reviveHint.SetActive(health.IsDead);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.HealthChanged -= healthBar.SetValue;
            health.Died -= ShowReviveHint;
            health.ReviveStarted -= HideReviveHint;
        }
        if (mana != null) mana.ManaChanged -= manaBar.SetValue;
        if (experience != null)
        {
            experience.ExpChanged -= expBar.SetValue;
            experience.LeveledUp -= SetLevel;
        }
    }

    private void SetLevel(int level) => levelLabel.text = $"Lv {level}";
    private void ShowReviveHint() => reviveHint.SetActive(true);
    private void HideReviveHint() => reviveHint.SetActive(false);
}
