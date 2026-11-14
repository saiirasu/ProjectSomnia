using System.Collections.Generic;
using UnityEngine;

public enum EncounterType { Normal, Elite, Boss }

// preset of one fight: which enemies appear, one per enemy slot on the combat scene
[CreateAssetMenu(fileName = "NewEncounter", menuName = "Somnia/Encounter")]
public class EncounterData : ScriptableObject
{
    [SerializeField] private EncounterType _type = EncounterType.Normal;
    [SerializeField] private List<CharacterStats> _enemies = new List<CharacterStats>();
    // meta shards for winning this fight, meant for elite and boss presets
    [SerializeField] private int _shardReward;

    public EncounterType Type => _type;
    public IReadOnlyList<CharacterStats> Enemies => _enemies;
    public int ShardReward => _shardReward;
}
