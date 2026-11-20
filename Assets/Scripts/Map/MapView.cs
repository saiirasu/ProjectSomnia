using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// draws MapData inside a vertical ScrollRect: lines first, nodes on top.
// it only shows the map and reports clicks, the rules live in MapController
public class MapView : MonoBehaviour
{
    private class LineView
    {
        public int From;
        public int To;
        public Image Image;
    }

    public event Action<int> OnNodeClicked;

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private MapNodeView _nodePrefab;
    [SerializeField] private Image _linePrefab;

    [Header("Layout")]
    [SerializeField] private float _floorSpacing = 140f;
    [SerializeField] private float _columnSpacing = 140f;
    // max random shift of a node in pixels
    [SerializeField] private float _jitter = 30f;
    // empty space below the first floor and above the boss
    [SerializeField] private float _margin = 150f;
    [SerializeField] private float _lineThickness = 6f;
    [SerializeField] private float _bossScale = 1.6f;

    [Header("Line Colors")]
    [SerializeField] private Color _lineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color _visitedLineColor = new Color(1f, 0.85f, 0.4f, 1f);

    private readonly List<MapNodeView> _nodeViews = new List<MapNodeView>();
    private readonly List<LineView> _lines = new List<LineView>();
    private readonly List<Vector2> _positions = new List<Vector2>();

    public void Build(MapData map)
    {
        Clear();

        float height = _margin * 2f + map.FloorCount * _floorSpacing;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, height);

        foreach (MapNode node in map.Nodes)
        {
            _positions.Add(GetPosition(node, map.ColumnCount));
        }

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            foreach (int next in map.Nodes[i].Next) CreateLine(i, next);
        }

        for (int i = 0; i < map.Nodes.Count; i++)
        {
            MapNodeView view = Instantiate(_nodePrefab, _content);
            RectTransform rect = (RectTransform)view.transform;
            PlaceAtBottom(rect, _positions[i]);
            if (i == map.BossIndex) rect.sizeDelta *= _bossScale;

            view.Setup(i, map.Nodes[i], HandleNodeClicked);
            _nodeViews.Add(view);
        }
    }

    public void Refresh(MapData map)
    {
        List<int> available = map.GetAvailableNodes();

        for (int i = 0; i < _nodeViews.Count; i++)
        {
            MapNodeState state = MapNodeState.Locked;
            if (i == map.CurrentIndex) state = MapNodeState.Current;
            else if (map.IsVisited(i)) state = MapNodeState.Visited;
            else if (available.Contains(i)) state = MapNodeState.Available;

            _nodeViews[i].SetState(state);
        }

        // the player visits one node per floor, so two linked visited nodes mean the walked path
        foreach (LineView line in _lines)
        {
            bool walked = map.IsVisited(line.From) && map.IsVisited(line.To);
            line.Image.color = walked ? _visitedLineColor : _lineColor;
        }
    }

    // keeps the floor the player stands on in the middle of the screen
    public void ScrollToCurrent(MapData map)
    {
        Canvas.ForceUpdateCanvases();

        float targetY = map.CurrentIndex >= 0 ? _positions[map.CurrentIndex].y : _margin;
        RectTransform viewport = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)_scrollRect.transform;
        float scrollable = _content.rect.height - viewport.rect.height;
        if (scrollable <= 0f) return;

        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01((targetY - viewport.rect.height * 0.5f) / scrollable);
    }

    Vector2 GetPosition(MapNode node, int columnCount)
    {
        float x = (node.Column - (columnCount - 1) * 0.5f) * _columnSpacing + node.Offset.x * _jitter;
        float y = _margin + node.Floor * _floorSpacing + node.Offset.y * _jitter;
        return new Vector2(x, y);
    }

    void CreateLine(int from, int to)
    {
        Vector2 start = _positions[from];
        Vector2 end = _positions[to];
        Vector2 delta = end - start;

        Image image = Instantiate(_linePrefab, _content);
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        PlaceAtBottom(rect, (start + end) * 0.5f);
        rect.sizeDelta = new Vector2(delta.magnitude, _lineThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        _lines.Add(new LineView { From = from, To = to, Image = image });
    }

    // positions are counted from the bottom center of the content
    void PlaceAtBottom(RectTransform rect, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }

    void HandleNodeClicked(int index)
    {
        OnNodeClicked?.Invoke(index);
    }

    void Clear()
    {
        foreach (MapNodeView view in _nodeViews) Destroy(view.gameObject);
        foreach (LineView line in _lines) Destroy(line.Image.gameObject);

        _nodeViews.Clear();
        _lines.Clear();
        _positions.Clear();
    }
}
