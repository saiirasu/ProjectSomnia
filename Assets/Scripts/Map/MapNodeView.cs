using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MapNodeState { Locked, Available, Visited, Current }

// one clickable node on the map screen, colored by room type until real icons exist
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _label;
    // optional ring or arrow shown on the node the party stands on
    [SerializeField] private GameObject _currentMarker;

    [Header("Type Colors")]
    [SerializeField] private Color _fightColor = new Color(0.85f, 0.35f, 0.3f);
    [SerializeField] private Color _eliteColor = new Color(0.65f, 0.25f, 0.75f);
    [SerializeField] private Color _restColor = new Color(0.3f, 0.75f, 0.4f);
    [SerializeField] private Color _treasureColor = new Color(0.95f, 0.8f, 0.25f);
    [SerializeField] private Color _bossColor = new Color(0.55f, 0.1f, 0.1f);

    [Header("States")]
    [SerializeField] private Color _visitedColor = new Color(0.45f, 0.45f, 0.45f);
    [SerializeField, Range(0f, 1f)] private float _lockedAlpha = 0.35f;
    [SerializeField] private float _pulseSpeed = 4f;
    [SerializeField] private float _pulseAmount = 0.12f;

    private int _index;
    private Action<int> _onClick;
    private Color _typeColor;
    private bool _isAvailable;

    public void Setup(int index, MapNode node, Action<int> onClick)
    {
        _index = index;
        _onClick = onClick;
        _typeColor = GetTypeColor(node.Type);

        if (_label != null) _label.text = GetLabel(node.Type);
        _button.onClick.AddListener(HandleClick);
    }

    public void SetState(MapNodeState state)
    {
        _isAvailable = state == MapNodeState.Available;
        _button.interactable = _isAvailable;
        if (!_isAvailable) transform.localScale = Vector3.one;

        if (_currentMarker != null) _currentMarker.SetActive(state == MapNodeState.Current);

        switch (state)
        {
            case MapNodeState.Locked:
                Color faded = _typeColor;
                faded.a = _lockedAlpha;
                _background.color = faded;
                break;
            case MapNodeState.Visited:
                _background.color = _visitedColor;
                break;
            default:
                _background.color = _typeColor;
                break;
        }
    }

    // available nodes pulse so the player sees where they can go
    void Update()
    {
        if (!_isAvailable) return;

        float scale = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    void HandleClick()
    {
        _onClick?.Invoke(_index);
    }

    Color GetTypeColor(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Elite: return _eliteColor;
            case MapNodeType.Rest: return _restColor;
            case MapNodeType.Treasure: return _treasureColor;
            case MapNodeType.Boss: return _bossColor;
            default: return _fightColor;
        }
    }

    string GetLabel(MapNodeType type)
    {
        switch (type)
        {
            case MapNodeType.Elite: return "ELITE";
            case MapNodeType.Rest: return "REST";
            case MapNodeType.Treasure: return "LOOT";
            case MapNodeType.Boss: return "BOSS";
            default: return "FIGHT";
        }
    }
}
