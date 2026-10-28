using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float invulnerabilityTime = 0.5f;
    [SerializeField] private Color hurtColor = new Color(1f, 0.35f, 0.35f);

    [Header("Revive")]
    [SerializeField] private Key reviveKey = Key.R;
    [SerializeField] private float reviveDuration = 1f;
    [SerializeField] private float reviveInvulnerabilityTime = 1.5f;

    private static readonly int IsDeadParam = Animator.StringToHash("IsDead");

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsReviving { get; private set; }

    // Raised with (current, max) whenever health changes.
    public event Action<int, int> HealthChanged;
    public event Action Died;
    public event Action ReviveStarted;
    public event Action Revived;

    private Animator animator;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private PlayerController controller;
    private float invulnerableUntil;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (IsDead && !IsReviving && Keyboard.current != null && Keyboard.current[reviveKey].isPressed)
            Revive();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || Time.time < invulnerableUntil) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        invulnerableUntil = Time.time + invulnerabilityTime;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth == 0)
            Die();
        else
            StartCoroutine(HurtFlash());
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(int amount, bool refill)
    {
        maxHealth += amount;
        if (refill && !IsDead) CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Revive()
    {
        if (!IsDead || IsReviving) return;
        StartCoroutine(ReviveRoutine());
    }

    private void Die()
    {
        IsDead = true;
        StopAllCoroutines();
        spriteRenderer.color = Color.white;

        if (controller != null) controller.enabled = false;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;

        animator.SetBool(IsDeadParam, true);
        Died?.Invoke();
    }

    private IEnumerator ReviveRoutine()
    {
        IsReviving = true;
        animator.SetBool(IsDeadParam, false);
        ReviveStarted?.Invoke();

        yield return new WaitForSeconds(reviveDuration);

        CurrentHealth = maxHealth;
        IsDead = false;
        IsReviving = false;
        invulnerableUntil = Time.time + reviveInvulnerabilityTime;
        body.simulated = true;
        if (controller != null) controller.enabled = true;

        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Revived?.Invoke();
    }

    private IEnumerator HurtFlash()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    [ContextMenu("Debug/Take 1 Damage")]
    private void DebugTakeDamage() => TakeDamage(1);

    [ContextMenu("Debug/Kill")]
    private void DebugKill() => TakeDamage(CurrentHealth);
}
