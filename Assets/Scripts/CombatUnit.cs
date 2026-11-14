using System;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class CombatUnit : MonoBehaviour
{
    public event Action<int, HitResult> OnHit;
    public event Action<int> OnHealed;
    public event Action<bool> OnDownChanged;
    public event Action<bool> OnGuardChanged;

    private const float WeakMultiplier = 1.5f;
    private const float ResistMultiplier = 0.5f;
    private const float GuardMultiplier = 0.5f;

    private HealthSystem _health;

    public CharacterStats Stats { get; private set; }
    public bool IsPlayerTeam { get; private set; }
    public bool IsDown { get; private set; }
    public bool IsGuarding { get; private set; }

    public bool IsInitialized => Stats != null;
    public bool IsDead => Health.IsDead;

    // lazy getter, CombatManager can initialize units before their own Awake
    public HealthSystem Health
    {
        get
        {
            if (_health == null) _health = GetComponent<HealthSystem>();
            return _health;
        }
    }

    public void Initialize(CharacterStats stats, bool isPlayerTeam)
    {
        Initialize(stats, isPlayerTeam, stats.MaxHealth, stats.MaxMana);
    }

    public void Initialize(CharacterStats stats, bool isPlayerTeam, int currentHealth, int currentMana)
    {
        Stats = stats;
        IsPlayerTeam = isPlayerTeam;
        IsDown = false;
        IsGuarding = false;

        Health.Initialize(stats.MaxHealth, stats.MaxMana, currentHealth, currentMana);
    }

    // called at the start of this unit's own turn
    public void BeginTurn()
    {
        SetDown(false);
        SetGuard(false);
    }

    public void Guard()
    {
        SetGuard(true);
    }

    public void StandUp()
    {
        SetDown(false);
    }

    public HitResult TakeHit(int rawDamage, ElementType element)
    {
        if (IsDead) return HitResult.Normal;

        Affinity affinity = Stats.GetAffinity(element);

        if (affinity == Affinity.Null)
        {
            OnHit?.Invoke(0, HitResult.Nullified);
            return HitResult.Nullified;
        }

        float multiplier = 1f;
        if (affinity == Affinity.Weak) multiplier = WeakMultiplier;
        else if (affinity == Affinity.Resist) multiplier = ResistMultiplier;
        if (IsGuarding) multiplier *= GuardMultiplier;

        // defense works as percent reduction, so damage never drops to zero
        float reducedDamage = rawDamage * 100f / (100f + Stats.Defense);
        int finalDamage = Mathf.Max(Mathf.RoundToInt(reducedDamage * multiplier), 1);

        Health.TakeDamage(finalDamage);

        HitResult result;
        if (affinity == Affinity.Resist) result = HitResult.Resisted;
        else if (affinity != Affinity.Weak) result = HitResult.Normal;
        // guard blocks knockdown, downed or killed target cannot be knocked down again
        else if (IsGuarding || IsDown || IsDead) result = HitResult.Weak;
        else
        {
            result = HitResult.KnockedDown;
            SetDown(true);
        }

        if (IsDead) SetDown(false);

        OnHit?.Invoke(finalDamage, result);
        return result;
    }

    public void ReceiveHeal(int amount)
    {
        if (IsDead) return;

        int healed = Health.Heal(amount);
        OnHealed?.Invoke(healed);
    }

    void SetDown(bool value)
    {
        if (IsDown == value) return;
        IsDown = value;
        OnDownChanged?.Invoke(value);
    }

    void SetGuard(bool value)
    {
        if (IsGuarding == value) return;
        IsGuarding = value;
        OnGuardChanged?.Invoke(value);
    }
}
