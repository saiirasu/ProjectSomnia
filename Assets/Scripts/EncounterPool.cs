using System.Collections.Generic;
using UnityEngine;

// list of fight presets for one node type, the map picks a random one
[CreateAssetMenu(fileName = "NewEncounterPool", menuName = "Somnia/Encounter Pool")]
public class EncounterPool : ScriptableObject
{
    [SerializeField] private List<EncounterData> _encounters = new List<EncounterData>();

    public EncounterData GetRandom()
    {
        if (_encounters.Count == 0) return null;
        return _encounters[Random.Range(0, _encounters.Count)];
    }
}
