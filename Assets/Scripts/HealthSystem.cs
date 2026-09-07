using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // События, на которые смогут подписываться UI и аниматор
    public event Action<int, int> OnHealthChanged; // Передает (Текущее ХП, Максимальное ХП)
    public event Action OnDied;

    private int currentHealth;
    private CharacterStats stats; // Данные, которые мы создали ранее

    // Инициализация здоровья при старте боя
    public void Initialize(CharacterStats baseStats)
    {
        stats = baseStats;
        currentHealth = stats.maxHealth;

        // Оповещаем систему, что здоровье установилось на максимум
        OnHealthChanged?.Invoke(currentHealth, stats.maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return; // Защита от получения урона после смерти

        // Учитываем броню (базовая формула, которую легко расширить)
        int finalDamage = Mathf.Max(damageAmount - stats.baseDefense, 0);

        currentHealth = Mathf.Max(currentHealth - finalDamage, 0);

        // Оповещаем все остальные скрипты, что ХП изменилось
        OnHealthChanged?.Invoke(currentHealth, stats.maxHealth);

        if (currentHealth == 0)
        {
            OnDied?.Invoke(); // Оповещаем о смерти
        }
    }
}