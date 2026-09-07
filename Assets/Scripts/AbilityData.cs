using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Somnia/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public int mpCost;
    public int damageAmount;

    // flag to check if player needs to buy it in hub
    public bool isUnlockedByDefault;

    // easy to add vfx or tags later
    // public string elementalTag
}