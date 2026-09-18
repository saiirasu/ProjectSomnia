using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnManaChanged;
    public event Action OnDied;

    // properties open for UI reading
    public int currentHealth { get; private set; }
    public int currentMana { get; private set; }

    public CharacterStats stats { get; private set; }

    public void Initialize(CharacterStats baseStats)
    {
        stats = baseStats;
        currentHealth = stats.maxHealth;
        currentMana = stats.maxMana;

        OnHealthChanged?.Invoke(currentHealth, stats.maxHealth);
        OnManaChanged?.Invoke(currentMana, stats.maxMana);
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;

        int finalDamage = Mathf.Max(damageAmount - stats.baseDefense, 0);
        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);

        OnHealthChanged?.Invoke(currentHealth, stats.maxHealth);

        if (currentHealth <= 0)
        {
            OnDied?.Invoke();
        }
    }

    public bool TryConsumeMana(int amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            OnManaChanged?.Invoke(currentMana, stats.maxMana);
            return true;
        }

        Debug.Log("not enough mana");
        return false;
    }
}