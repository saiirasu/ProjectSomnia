using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStats", menuName = "Somnia/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Base Information")]
    public string characterName;
    public string personaName;
    public bool isPlayerTeam;

    [Header("Combat Stats")]
    public int maxHealth;
    public int maxMana;
    public int baseDamage;
    public int baseDefense;

    [Header("Abilities")]
    // fixed list of 2 or 3 skills for this persona
    public List<AbilityData> abilities;
}