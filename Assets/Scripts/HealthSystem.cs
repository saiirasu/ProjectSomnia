using System;
using UnityEngine;

// only HP and MP, combat rules (elements, down, guard) are in CombatUnit
public class HealthSystem : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnManaChanged;
    public event Action OnDeath;

    public int CurrentHealth { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxHealth { get; private set; }
    public int MaxMana { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    public void Initialize(int maxHealth, int maxMana)
    {
        Initialize(maxHealth, maxMana, maxHealth, maxMana);
    }

    // used for heroes, who keep their wounds between fights
    public void Initialize(int maxHealth, int maxMana, int currentHealth, int currentMana)
    {
        MaxHealth = maxHealth;
        MaxMana = maxMana;
        CurrentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        CurrentMana = Mathf.Clamp(currentMana, 0, maxMana);

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (IsDead) OnDeath?.Invoke();
    }

    // returns how much was actually healed, dead characters cannot be healed
    public int Heal(int amount)
    {
        if (IsDead || amount <= 0) return 0;

        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        return CurrentHealth - before;
    }

    public bool UseMana(int amount)
    {
        if (CurrentMana < amount) return false;

        CurrentMana -= amount;
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        return true;
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;

        CurrentMana = Mathf.Min(CurrentMana + amount, MaxMana);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }
}
