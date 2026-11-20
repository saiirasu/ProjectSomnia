using System.Collections.Generic;
using UnityEngine;

// data of the current run (party, wounds, next fight), lives in memory until the run ends
public static class RunState
{
    private class UnitState
    {
        public int Health;
        public int Mana;
    }

    private static readonly List<CharacterStats> _party = new List<CharacterStats>();
    private static readonly Dictionary<CharacterStats, UnitState> _unitStates = new Dictionary<CharacterStats, UnitState>();

    public static bool IsRunActive { get; private set; }
    public static IReadOnlyList<CharacterStats> Party => _party;
    public static int FightsWon { get; private set; }

    // set by the node map before loading the combat scene
    public static EncounterData CurrentEncounter { get; set; }

    // map of this run, generated once by the map scene and kept between scene loads
    public static MapData Map { get; set; }

    // static data survives between play sessions when domain reload is disabled in the editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        EndRun();
    }

    public static void StartNewRun(IReadOnlyList<CharacterStats> party)
    {
        _party.Clear();
        _unitStates.Clear();

        foreach (CharacterStats hero in party)
        {
            if (hero == null) continue;

            _party.Add(hero);
            _unitStates[hero] = new UnitState { Health = hero.MaxHealth, Mana = hero.MaxMana };
        }

        CurrentEncounter = null;
        Map = null;
        FightsWon = 0;
        IsRunActive = true;
    }

    public static void EndRun()
    {
        _party.Clear();
        _unitStates.Clear();
        CurrentEncounter = null;
        Map = null;
        FightsWon = 0;
        IsRunActive = false;
    }

    public static void RegisterWin()
    {
        FightsWon++;
    }

    public static int GetHealth(CharacterStats hero)
    {
        return _unitStates.TryGetValue(hero, out UnitState state) ? state.Health : hero.MaxHealth;
    }

    public static int GetMana(CharacterStats hero)
    {
        return _unitStates.TryGetValue(hero, out UnitState state) ? state.Mana : hero.MaxMana;
    }

    public static void SaveUnit(CharacterStats hero, int health, int mana)
    {
        if (!_unitStates.TryGetValue(hero, out UnitState state)) return;

        state.Health = Mathf.Clamp(health, 0, hero.MaxHealth);
        state.Mana = Mathf.Clamp(mana, 0, hero.MaxMana);
    }

    // rest site option: heal every hero by a percent of max HP (0.3 = 30%)
    public static void HealParty(float percent)
    {
        foreach (CharacterStats hero in _party)
        {
            UnitState state = _unitStates[hero];
            int amount = Mathf.RoundToInt(hero.MaxHealth * percent);
            state.Health = Mathf.Min(state.Health + amount, hero.MaxHealth);
        }
    }

    // rest site option: restore MP of every hero by a percent of max MP
    public static void RestoreManaParty(float percent)
    {
        foreach (CharacterStats hero in _party)
        {
            UnitState state = _unitStates[hero];
            int amount = Mathf.RoundToInt(hero.MaxMana * percent);
            state.Mana = Mathf.Min(state.Mana + amount, hero.MaxMana);
        }
    }
}
