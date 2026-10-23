using System;
using UnityEngine;

/// <summary>
/// Универсальная система здоровья. Вешается и на игрока, и на мобов.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Время неуязвимости после получения урона (сек). 0 - выключено")]
    [SerializeField] private float invulnerabilityTime = 0f;

    private float current;
    private bool initialized;
    private float lastDamageTime = float.NegativeInfinity;

    public float MaxHealth => maxHealth;
    public float CurrentHealth
    {
        get
        {
            EnsureInitialized();
            return current;
        }
    }
    public float Normalized => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;
    public bool IsDead { get; private set; }

    /// Текущее здоровье, максимум
    public event Action<float, float> Changed;

    /// Размер полученного урона
    public event Action<float> Damaged;

    public event Action<Health> Died;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        invulnerabilityTime = Mathf.Max(0f, invulnerabilityTime);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        current = maxHealth;
    }

    /// true, если урон был нанесён
    public bool TakeDamage(float amount)
    {
        EnsureInitialized();
        if (IsDead || amount <= 0f) return false;
        if (Time.time - lastDamageTime < invulnerabilityTime) return false;

        lastDamageTime = Time.time;
        current = Mathf.Max(0f, current - amount);

        Damaged?.Invoke(amount);
        Changed?.Invoke(current, maxHealth);

        if (current <= 0f)
        {
            IsDead = true;
            Died?.Invoke(this);
        }

        return true;
    }

    public void Heal(float amount)
    {
        EnsureInitialized();
        if (IsDead || amount <= 0f) return;

        float before = current;
        current = Mathf.Min(maxHealth, current + amount);

        if (!Mathf.Approximately(before, current))
        {
            Changed?.Invoke(current, maxHealth);
        }
    }

    public void ResetHealth()
    {
        initialized = true;
        current = maxHealth;
        IsDead = false;
        lastDamageTime = float.NegativeInfinity;
        Changed?.Invoke(current, maxHealth);
    }
}
