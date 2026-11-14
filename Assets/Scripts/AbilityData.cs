using UnityEngine;
using UnityEngine.Serialization;

public enum TargetType { SingleEnemy, AllEnemies, Self, SingleAlly, AllAllies }

public enum AbilityEffect { Damage, Heal }

[CreateAssetMenu(fileName = "NewAbility", menuName = "Somnia/Ability")]
public class AbilityData : ScriptableObject
{
    // FormerlySerializedAs keeps values in assets created before the rename
    [FormerlySerializedAs("abilityName")] [SerializeField] private string _abilityName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;

    [FormerlySerializedAs("mpCost")] [SerializeField] private int _manaCost;
    [FormerlySerializedAs("damageAmount")] [SerializeField] private int _power;
    [SerializeField] private ElementType _element = ElementType.Physical;
    [SerializeField] private AbilityEffect _effect = AbilityEffect.Damage;
    [SerializeField] private TargetType _targetType = TargetType.SingleEnemy;

    // flag to check if player needs to buy it in hub
    [FormerlySerializedAs("isUnlockedByDefault")] [SerializeField] private bool _isUnlockedByDefault;

    public string AbilityName => _abilityName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int ManaCost => _manaCost;
    public int Power => _power;
    public ElementType Element => _element;
    public AbilityEffect Effect => _effect;
    public TargetType TargetType => _targetType;
    public bool IsUnlockedByDefault => _isUnlockedByDefault;

    public bool NeedsTargetSelection => _targetType == TargetType.SingleEnemy || _targetType == TargetType.SingleAlly;
    public bool TargetsEnemies => _targetType == TargetType.SingleEnemy || _targetType == TargetType.AllEnemies;
}
