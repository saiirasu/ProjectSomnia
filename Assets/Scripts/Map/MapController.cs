using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// rules of the map screen: creates the run map once, moves the party and starts what a node holds
public class MapController : MonoBehaviour
{
    [SerializeField] private MapConfig _config;
    [SerializeField] private MapView _mapView;

    [Header("Test Run (used when the scene is started directly)")]
    [SerializeField] private List<CharacterStats> _testParty = new List<CharacterStats>();
    // 0 = new random map every run, any other number always gives the same map
    [SerializeField] private int _fixedSeed;

    [Header("Scenes")]
    [SerializeField] private string _combatSceneName = "Combat";
    // empty name reloads this scene with a new test run, useful until the hub exists
    [SerializeField] private string _hubSceneName;

    [Header("Party Info (optional)")]
    [SerializeField] private TextMeshProUGUI _partyText;
    [SerializeField] private TextMeshProUGUI _seedText;

    [Header("Rest Site")]
    [SerializeField] private GameObject _restPanel;
    [SerializeField] private Button _healButton;
    [SerializeField] private Button _manaButton;
    [SerializeField] private TextMeshProUGUI _healButtonText;
    [SerializeField] private TextMeshProUGUI _manaButtonText;

    [Header("Message Panel (treasure, end of run)")]
    [SerializeField] private GameObject _messagePanel;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _messageButton;
    [SerializeField] private TextMeshProUGUI _messageButtonText;

    private MapData _map;
    private bool _isRunFinished;

    void Start()
    {
        // scene started from the editor without the hub, begin a test run
        if (!RunState.IsRunActive) RunState.StartNewRun(_testParty);

        if (RunState.Map == null)
        {
            int seed = _fixedSeed != 0 ? _fixedSeed : Random.Range(1, int.MaxValue);
            RunState.Map = MapGenerator.Generate(_config, seed);
        }
        _map = RunState.Map;

        _healButton.onClick.AddListener(OnHealClicked);
        _manaButton.onClick.AddListener(OnManaClicked);
        _messageButton.onClick.AddListener(OnMessageClosed);

        if (_healButtonText != null) _healButtonText.text = $"Rest: heal {ToPercent(_config.RestHealPercent)} HP";
        if (_manaButtonText != null) _manaButtonText.text = $"Meditate: restore {ToPercent(_config.RestManaPercent)} MP";
        if (_seedText != null) _seedText.text = "Seed: " + _map.Seed;

        _restPanel.SetActive(false);
        _messagePanel.SetActive(false);

        _mapView.OnNodeClicked += HandleNodeClicked;
        _mapView.Build(_map);
        _mapView.Refresh(_map);
        _mapView.ScrollToCurrent(_map);
        UpdatePartyText();

        // a lost fight sends the player to the hub, so being back on the boss node means the boss is beaten
        if (_map.IsBossReached) FinishRun();
    }

    void OnDestroy()
    {
        if (_mapView != null) _mapView.OnNodeClicked -= HandleNodeClicked;
    }

    void HandleNodeClicked(int index)
    {
        if (IsPanelOpen() || !_map.IsAvailable(index)) return;

        _map.MoveTo(index);
        _mapView.Refresh(_map);

        MapNode node = _map.Nodes[index];
        switch (node.Type)
        {
            case MapNodeType.Fight:
            case MapNodeType.Elite:
            case MapNodeType.Boss:
                StartFight(node);
                break;
            case MapNodeType.Rest:
                _restPanel.SetActive(true);
                break;
            case MapNodeType.Treasure:
                OpenTreasure();
                break;
        }
    }

    void StartFight(MapNode node)
    {
        EncounterPool pool = _config.GetPool(node);
        if (pool == null) Debug.LogWarning($"MapController: no encounter pool for {node.Type}, combat scene will use its test enemies");

        RunState.CurrentEncounter = pool != null ? pool.GetRandom() : null;
        SceneManager.LoadScene(_combatSceneName);
    }

    // ---------- rest site ----------

    void OnHealClicked()
    {
        RunState.HealParty(_config.RestHealPercent);
        CloseRest();
    }

    void OnManaClicked()
    {
        RunState.RestoreManaParty(_config.RestManaPercent);
        CloseRest();
    }

    void CloseRest()
    {
        _restPanel.SetActive(false);
        UpdatePartyText();
    }

    // ---------- treasure ----------

    void OpenTreasure()
    {
        int currency = Random.Range(_config.TreasureCurrencyMin, _config.TreasureCurrencyMax + 1);
        PlayerProgress.AddCurrency(currency);
        PlayerProgress.AddShards(_config.TreasureShards);

        string text = $"TREASURE\n+{currency} currency";
        if (_config.TreasureShards > 0) text += $"\n+{_config.TreasureShards} shards";
        ShowMessage(text, "Take");
    }

    // ---------- end of run ----------

    void FinishRun()
    {
        _isRunFinished = true;
        ShowMessage($"THE WORLD IS CLEARED\nFights won: {RunState.FightsWon}\nCurrency: {PlayerProgress.Currency}  Shards: {PlayerProgress.Shards}", "Return to hub");
    }

    void OnMessageClosed()
    {
        _messagePanel.SetActive(false);
        if (!_isRunFinished) return;

        RunState.EndRun();
        if (string.IsNullOrEmpty(_hubSceneName)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else SceneManager.LoadScene(_hubSceneName);
    }

    // ---------- helpers ----------

    void ShowMessage(string text, string buttonText)
    {
        _messageText.text = text;
        if (_messageButtonText != null) _messageButtonText.text = buttonText;
        _messagePanel.SetActive(true);
    }

    bool IsPanelOpen()
    {
        return _restPanel.activeSelf || _messagePanel.activeSelf;
    }

    void UpdatePartyText()
    {
        if (_partyText == null) return;

        StringBuilder builder = new StringBuilder();
        foreach (CharacterStats hero in RunState.Party)
        {
            builder.AppendLine($"{hero.CharacterName}  HP {RunState.GetHealth(hero)}/{hero.MaxHealth}  MP {RunState.GetMana(hero)}/{hero.MaxMana}");
        }
        _partyText.text = builder.ToString();
    }

    string ToPercent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }
}
