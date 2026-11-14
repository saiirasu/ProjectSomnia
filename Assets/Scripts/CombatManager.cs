using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CombatState { Start, PlayerTurn, TargetSelection, ActionExecution, EnemyTurn, Win, Lose }

public class CombatManager : MonoBehaviour
{
    public event Action<CombatState> OnStateChanged;
    public event Action<CombatUnit> OnTurnStarted;
    public event Action<CombatUnit, AbilityData> OnActionStarted;
    public event Action<CombatUnit> OnGuardUsed;
    public event Action<CombatUnit> OnOneMore;
    public event Action OnAllOutAttack;
    public event Action<IReadOnlyList<CombatUnit>> OnTargetSelectionStarted;
    public event Action OnTargetSelectionEnded;
    // won, currency reward, shard reward
    public event Action<bool, int, int> OnCombatEnded;

    [Header("Arena Slots")]
    // unit objects placed on the scene, unused slots get disabled
    [SerializeField] private List<CombatUnit> _partySlots = new List<CombatUnit>();
    [SerializeField] private List<CombatUnit> _enemySlots = new List<CombatUnit>();

    [Header("Combatants (used when the scene is started directly)")]
    // party starts a new run if none is active, enemies are replaced by RunState.CurrentEncounter
    [SerializeField] private List<CharacterStats> _partyStats = new List<CharacterStats>();
    [SerializeField] private List<CharacterStats> _enemyStats = new List<CharacterStats>();

    [Header("Actions")]
    [SerializeField] private AbilityData _basicAttack;
    [SerializeField] private float _allOutMultiplier = 1.5f;

    [Header("After Victory")]
    // part of max MP restored after a won fight (0.2 = 20%)
    [SerializeField, Range(0f, 1f)] private float _manaRestorePercent = 0.2f;
    // fallen heroes get up with this HP after a won fight
    [SerializeField] private int _reviveHealth = 1;

    [Header("Meta Shards")]
    // shards for a lost run scale with fights won, so dying early cannot be farmed
    [SerializeField] private int _defeatShardsPerWin = 1;

    [Header("Timing")]
    [SerializeField] private float _startDelay = 1f;
    [SerializeField] private float _actionDelay = 0.6f;
    [SerializeField] private float _enemyThinkDelay = 0.8f;

    private readonly List<CombatUnit> _party = new List<CombatUnit>();
    private readonly List<CombatUnit> _enemies = new List<CombatUnit>();
    private readonly List<CombatUnit> _turnQueue = new List<CombatUnit>();
    private readonly List<CombatUnit> _validTargets = new List<CombatUnit>();

    private WaitForSeconds _startWait;
    private WaitForSeconds _actionWait;
    private WaitForSeconds _enemyThinkWait;

    private bool _isPlayerPhase;
    private AbilityData _pendingAbility;

    public CombatState State { get; private set; }
    public CombatUnit ActiveUnit { get; private set; }
    public AbilityData BasicAttack => _basicAttack;

    // all-out attack is offered when every living enemy is down
    public bool CanAllOutAttack => State == CombatState.PlayerTurn && AreAllEnemiesDown();

    // units are set up in Awake, so other scripts can read them in Start
    void Awake()
    {
        _startWait = new WaitForSeconds(_startDelay);
        _actionWait = new WaitForSeconds(_actionDelay);
        _enemyThinkWait = new WaitForSeconds(_enemyThinkDelay);

        // scene started from the editor without the map, begin a test run
        if (!RunState.IsRunActive) RunState.StartNewRun(_partyStats);

        IReadOnlyList<CharacterStats> enemyStats = _enemyStats;
        if (RunState.CurrentEncounter != null) enemyStats = RunState.CurrentEncounter.Enemies;

        SetupTeam(_partySlots, RunState.Party, _party, true);
        SetupTeam(_enemySlots, enemyStats, _enemies, false);
    }

    void Start()
    {
        SetState(CombatState.Start);
        StartCoroutine(StartCombat());
    }

    void SetupTeam(List<CombatUnit> slots, IReadOnlyList<CharacterStats> stats, List<CombatUnit> team, bool isPlayerTeam)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < stats.Count && stats[i] != null)
            {
                // heroes keep HP and MP from previous fights of the run
                if (isPlayerTeam) slots[i].Initialize(stats[i], true, RunState.GetHealth(stats[i]), RunState.GetMana(stats[i]));
                else slots[i].Initialize(stats[i], false);

                team.Add(slots[i]);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    IEnumerator StartCombat()
    {
        yield return _startWait;
        StartPhase(true);
    }

    // ---------- turn order ----------

    void StartPhase(bool isPlayerPhase)
    {
        _isPlayerPhase = isPlayerPhase;
        _turnQueue.Clear();

        List<CombatUnit> team = isPlayerPhase ? _party : _enemies;
        foreach (CombatUnit unit in team)
        {
            if (!unit.IsDead) _turnQueue.Add(unit);
        }

        // faster units act first inside a phase
        _turnQueue.Sort((a, b) => b.Stats.Speed.CompareTo(a.Stats.Speed));

        NextTurn();
    }

    void NextTurn()
    {
        // skip units that died before their turn came
        while (_turnQueue.Count > 0 && _turnQueue[0].IsDead)
        {
            _turnQueue.RemoveAt(0);
        }

        if (_turnQueue.Count == 0)
        {
            StartPhase(!_isPlayerPhase);
            return;
        }

        ActiveUnit = _turnQueue[0];
        _turnQueue.RemoveAt(0);

        ActiveUnit.BeginTurn();
        BeginActiveUnitAction();
    }

    // also used for One More: the same unit acts again without BeginTurn
    void BeginActiveUnitAction()
    {
        if (ActiveUnit.IsPlayerTeam)
        {
            SetState(CombatState.PlayerTurn);
            OnTurnStarted?.Invoke(ActiveUnit);
        }
        else
        {
            SetState(CombatState.EnemyTurn);
            OnTurnStarted?.Invoke(ActiveUnit);
            StartCoroutine(EnemyTurn());
        }
    }

    // ---------- player commands (called from UI) ----------

    public bool CanUse(CombatUnit unit, AbilityData ability)
    {
        return unit.Health.CurrentMana >= ability.ManaCost;
    }

    public void SelectAbility(AbilityData ability)
    {
        if (State != CombatState.PlayerTurn) return;
        if (!CanUse(ActiveUnit, ability)) return;

        if (ability.NeedsTargetSelection)
        {
            _pendingAbility = ability;
            FillTargets(ActiveUnit, ability.TargetType, _validTargets);
            SetState(CombatState.TargetSelection);
            OnTargetSelectionStarted?.Invoke(_validTargets);
            return;
        }

        List<CombatUnit> targets = new List<CombatUnit>();
        FillTargets(ActiveUnit, ability.TargetType, targets);
        StartCoroutine(ExecuteAction(ActiveUnit, ability, targets));
    }

    public void SelectTarget(CombatUnit target)
    {
        if (State != CombatState.TargetSelection) return;
        if (!_validTargets.Contains(target)) return;

        OnTargetSelectionEnded?.Invoke();

        List<CombatUnit> targets = new List<CombatUnit> { target };
        StartCoroutine(ExecuteAction(ActiveUnit, _pendingAbility, targets));
        _pendingAbility = null;
    }

    public void CancelTargetSelection()
    {
        if (State != CombatState.TargetSelection) return;

        _pendingAbility = null;
        OnTargetSelectionEnded?.Invoke();
        SetState(CombatState.PlayerTurn);
    }

    public void SelectGuard()
    {
        if (State != CombatState.PlayerTurn) return;
        StartCoroutine(GuardAction());
    }

    public void SelectAllOutAttack()
    {
        if (!CanAllOutAttack) return;
        StartCoroutine(AllOutAttack());
    }

    // ---------- action execution ----------

    IEnumerator ExecuteAction(CombatUnit user, AbilityData ability, List<CombatUnit> targets)
    {
        SetState(CombatState.ActionExecution);

        user.Health.UseMana(ability.ManaCost);
        OnActionStarted?.Invoke(user, ability);
        yield return _actionWait;

        bool oneMore = false;
        foreach (CombatUnit target in targets)
        {
            if (ability.Effect == AbilityEffect.Heal)
            {
                target.ReceiveHeal(ability.Power);
                continue;
            }

            int rawDamage = ability.Power + user.Stats.Attack;
            HitResult result = target.TakeHit(rawDamage, ability.Element);
            if (result == HitResult.KnockedDown) oneMore = true;
        }
        yield return _actionWait;

        if (CheckCombatEnd()) yield break;

        if (oneMore)
        {
            OnOneMore?.Invoke(user);
            yield return _actionWait;
            BeginActiveUnitAction();
        }
        else
        {
            NextTurn();
        }
    }

    IEnumerator GuardAction()
    {
        SetState(CombatState.ActionExecution);

        ActiveUnit.Guard();
        OnGuardUsed?.Invoke(ActiveUnit);
        yield return _actionWait;

        NextTurn();
    }

    // whole living party hits every enemy with almighty damage, then enemies stand up
    IEnumerator AllOutAttack()
    {
        SetState(CombatState.ActionExecution);
        OnAllOutAttack?.Invoke();
        yield return _actionWait;

        int partyPower = 0;
        foreach (CombatUnit hero in _party)
        {
            if (!hero.IsDead) partyPower += hero.Stats.Attack + _basicAttack.Power;
        }
        int rawDamage = Mathf.RoundToInt(partyPower * _allOutMultiplier);

        foreach (CombatUnit enemy in _enemies)
        {
            if (enemy.IsDead) continue;
            enemy.TakeHit(rawDamage, ElementType.Almighty);
            enemy.StandUp();
        }
        yield return _actionWait;

        if (CheckCombatEnd()) yield break;
        NextTurn();
    }

    // ---------- enemy AI ----------

    IEnumerator EnemyTurn()
    {
        yield return _enemyThinkWait;

        CombatUnit enemy = ActiveUnit;
        List<CombatUnit> targets = new List<CombatUnit>();
        AbilityData ability = ChooseEnemyAbility(enemy, targets);

        StartCoroutine(ExecuteAction(enemy, ability, targets));
    }

    // simple priority AI: heal a wounded ally, else hit a weakness, else random attack
    AbilityData ChooseEnemyAbility(CombatUnit enemy, List<CombatUnit> targets)
    {
        List<AbilityData> usable = new List<AbilityData> { _basicAttack };
        foreach (AbilityData ability in enemy.Stats.Abilities)
        {
            if (CanUse(enemy, ability)) usable.Add(ability);
        }

        CombatUnit woundedAlly = FindWoundedAlly(enemy);
        if (woundedAlly != null)
        {
            foreach (AbilityData ability in usable)
            {
                if (ability.Effect != AbilityEffect.Heal) continue;
                SetTargets(enemy, ability, woundedAlly, targets);
                return ability;
            }
        }

        foreach (AbilityData ability in usable)
        {
            if (ability.Effect != AbilityEffect.Damage || !ability.TargetsEnemies) continue;

            foreach (CombatUnit hero in _party)
            {
                if (hero.IsDead || hero.IsDown || hero.IsGuarding) continue;
                if (hero.Stats.GetAffinity(ability.Element) != Affinity.Weak) continue;

                SetTargets(enemy, ability, hero, targets);
                return ability;
            }
        }

        List<AbilityData> attacks = new List<AbilityData>();
        foreach (AbilityData ability in usable)
        {
            if (ability.Effect == AbilityEffect.Damage && ability.TargetsEnemies) attacks.Add(ability);
        }

        AbilityData chosen = attacks[UnityEngine.Random.Range(0, attacks.Count)];
        FillTargets(enemy, chosen.TargetType, targets);

        if (chosen.NeedsTargetSelection)
        {
            CombatUnit randomTarget = targets[UnityEngine.Random.Range(0, targets.Count)];
            targets.Clear();
            targets.Add(randomTarget);
        }
        return chosen;
    }

    CombatUnit FindWoundedAlly(CombatUnit user)
    {
        List<CombatUnit> allies = user.IsPlayerTeam ? _party : _enemies;
        foreach (CombatUnit ally in allies)
        {
            if (ally.IsDead) continue;
            if (ally.Health.CurrentHealth * 2 < ally.Health.MaxHealth) return ally;
        }
        return null;
    }

    // ---------- helpers ----------

    void SetTargets(CombatUnit user, AbilityData ability, CombatUnit mainTarget, List<CombatUnit> targets)
    {
        if (ability.NeedsTargetSelection)
        {
            targets.Clear();
            targets.Add(mainTarget);
        }
        else
        {
            FillTargets(user, ability.TargetType, targets);
        }
    }

    // "enemies" and "allies" are relative to the user's team
    void FillTargets(CombatUnit user, TargetType targetType, List<CombatUnit> result)
    {
        result.Clear();

        List<CombatUnit> allies = user.IsPlayerTeam ? _party : _enemies;
        List<CombatUnit> opponents = user.IsPlayerTeam ? _enemies : _party;

        switch (targetType)
        {
            case TargetType.Self:
                result.Add(user);
                break;
            case TargetType.SingleEnemy:
            case TargetType.AllEnemies:
                AddAlive(opponents, result);
                break;
            case TargetType.SingleAlly:
            case TargetType.AllAllies:
                AddAlive(allies, result);
                break;
        }
    }

    void AddAlive(List<CombatUnit> source, List<CombatUnit> result)
    {
        foreach (CombatUnit unit in source)
        {
            if (!unit.IsDead) result.Add(unit);
        }
    }

    bool AreAllEnemiesDown()
    {
        foreach (CombatUnit enemy in _enemies)
        {
            if (!enemy.IsDead && !enemy.IsDown) return false;
        }
        return true;
    }

    // wounds stay for the next fight, a little MP comes back, fallen heroes get up
    void SavePartyToRun()
    {
        foreach (CombatUnit hero in _party)
        {
            int health = hero.IsDead ? _reviveHealth : hero.Health.CurrentHealth;
            int manaBonus = Mathf.RoundToInt(hero.Stats.MaxMana * _manaRestorePercent);
            RunState.SaveUnit(hero.Stats, health, hero.Health.CurrentMana + manaBonus);
        }
    }

    bool IsTeamDead(List<CombatUnit> team)
    {
        foreach (CombatUnit unit in team)
        {
            if (!unit.IsDead) return false;
        }
        return true;
    }

    bool CheckCombatEnd()
    {
        if (IsTeamDead(_enemies))
        {
            int reward = 0;
            foreach (CombatUnit enemy in _enemies)
            {
                reward += enemy.Stats.RewardCurrency;
            }
            PlayerProgress.AddCurrency(reward);

            // shards come only from presets that have them (elites, bosses)
            int shards = RunState.CurrentEncounter != null ? RunState.CurrentEncounter.ShardReward : 0;
            PlayerProgress.AddShards(shards);

            SavePartyToRun();
            RunState.RegisterWin();
            RunState.CurrentEncounter = null;

            SetState(CombatState.Win);
            OnCombatEnded?.Invoke(true, reward, shards);
            return true;
        }

        if (IsTeamDead(_party))
        {
            int shards = RunState.FightsWon * _defeatShardsPerWin;
            PlayerProgress.AddShards(shards);

            // death ends the run, currency and shards are already saved in PlayerProgress
            RunState.EndRun();

            SetState(CombatState.Lose);
            OnCombatEnded?.Invoke(false, 0, shards);
            return true;
        }

        return false;
    }

    void SetState(CombatState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
