using UnityEngine;

// settings of one world: map shape, room rules, fight pools and rewards
[CreateAssetMenu(fileName = "NewMapConfig", menuName = "Somnia/Map Config")]
public class MapConfig : ScriptableObject
{
    [Header("Grid")]
    // floors before the boss, the boss is one more floor on top
    [SerializeField, Min(4)] private int _floorCount = 12;
    [SerializeField, Min(3)] private int _columnCount = 7;
    // how many walks from bottom to top are drawn, more paths = more choice
    [SerializeField, Min(2)] private int _pathCount = 6;

    [Header("Fixed Floors (0 = first floor)")]
    // the first floor is always a fight and the last floor is always a rest
    [SerializeField] private int _treasureFloor = 6;
    // elites and rests do not appear below this floor
    [SerializeField] private int _minEliteRestFloor = 4;

    [Header("Room Weights (other floors)")]
    [SerializeField, Min(0)] private int _fightWeight = 55;
    [SerializeField, Min(0)] private int _eliteWeight = 16;
    [SerializeField, Min(0)] private int _restWeight = 14;
    [SerializeField, Min(0)] private int _treasureWeight = 5;

    [Header("Encounters")]
    [SerializeField] private EncounterPool _easyFightPool;
    [SerializeField] private EncounterPool _hardFightPool;
    // fights on floors from this one take presets from the hard pool
    [SerializeField] private int _hardFightsFromFloor = 3;
    [SerializeField] private EncounterPool _elitePool;
    [SerializeField] private EncounterPool _bossPool;

    [Header("Rest Site")]
    [SerializeField, Range(0f, 1f)] private float _restHealPercent = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _restManaPercent = 0.5f;

    [Header("Treasure")]
    [SerializeField] private int _treasureCurrencyMin = 20;
    [SerializeField] private int _treasureCurrencyMax = 40;
    [SerializeField] private int _treasureShards = 1;

    public int FloorCount => _floorCount;
    public int ColumnCount => _columnCount;
    public int PathCount => _pathCount;
    public int TreasureFloor => _treasureFloor;
    public int MinEliteRestFloor => _minEliteRestFloor;

    public float RestHealPercent => _restHealPercent;
    public float RestManaPercent => _restManaPercent;
    public int TreasureCurrencyMin => _treasureCurrencyMin;
    public int TreasureCurrencyMax => _treasureCurrencyMax;
    public int TreasureShards => _treasureShards;

    public int GetWeight(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Fight: return _fightWeight;
            case MapNodeType.Elite: return _eliteWeight;
            case MapNodeType.Rest: return _restWeight;
            case MapNodeType.Treasure: return _treasureWeight;
            default: return 0;
        }
    }

    // pool for a node, normal fights get harder after the first floors
    public EncounterPool GetPool(MapNode node)
    {
        switch (node.Type)
        {
            case MapNodeType.Fight: return node.Floor < _hardFightsFromFloor || _hardFightPool == null ? _easyFightPool : _hardFightPool;
            case MapNodeType.Elite: return _elitePool;
            case MapNodeType.Boss: return _bossPool;
            default: return null;
        }
    }

    void OnValidate()
    {
        _treasureFloor = Mathf.Clamp(_treasureFloor, 1, _floorCount - 2);
        _minEliteRestFloor = Mathf.Clamp(_minEliteRestFloor, 1, _floorCount - 1);
        if (_treasureCurrencyMax < _treasureCurrencyMin) _treasureCurrencyMax = _treasureCurrencyMin;
    }
}
