using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

// template of a character, runtime values live in CombatUnit and HealthSystem
[CreateAssetMenu(fileName = "NewStats", menuName = "Somnia/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Base Information")]
    [FormerlySerializedAs("characterName")] [SerializeField] private string _characterName;
    [FormerlySerializedAs("personaName")] [SerializeField] private string _personaName;

    [Header("Combat Stats")]
    [FormerlySerializedAs("maxHealth")] [SerializeField] private int _maxHealth;
    [FormerlySerializedAs("maxMana")] [SerializeField] private int _maxMana;
    [FormerlySerializedAs("baseDamage")] [SerializeField] private int _attack;
    [FormerlySerializedAs("baseDefense")] [SerializeField] private int _defense;
    [SerializeField] private int _speed = 10;

    [Header("Abilities")]
    // fixed list of 2 or 3 skills for this persona
    [FormerlySerializedAs("abilities")] [SerializeField] private List<AbilityData> _abilities = new List<AbilityData>();

    [Header("Affinities")]
    // elements not listed here count as Normal
    [SerializeField] private List<ElementAffinity> _affinities = new List<ElementAffinity>();

    [Header("Reward")]
    // currency the player gets for defeating this character
    [SerializeField] private int _rewardCurrency;

    public string CharacterName => _characterName;
    public string PersonaName => _personaName;
    public int MaxHealth => _maxHealth;
    public int MaxMana => _maxMana;
    public int Attack => _attack;
    public int Defense => _defense;
    public int Speed => _speed;
    public IReadOnlyList<AbilityData> Abilities => _abilities;
    public int RewardCurrency => _rewardCurrency;

    public Affinity GetAffinity(ElementType element)
    {
        for (int i = 0; i < _affinities.Count; i++)
        {
            if (_affinities[i].Element == element) return _affinities[i].Affinity;
        }
        return Affinity.Normal;
    }
}
