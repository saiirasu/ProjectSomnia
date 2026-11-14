using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// visual side of a unit on the arena: markers, colors, popups and click to target.
// clicks need a Collider2D on this object and a Physics2DRaycaster on the camera
[RequireComponent(typeof(CombatUnit))]
public class CombatUnitView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CombatManager _combatManager;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    // shown during this unit's turn
    [SerializeField] private GameObject _activeMarker;
    // shown when this unit can be chosen as a target
    [SerializeField] private GameObject _targetMarker;

    [Header("Popups")]
    [SerializeField] private DamagePopup _popupPrefab;
    [SerializeField] private Transform _popupPoint;

    [Header("Colors")]
    [SerializeField] private Color _downColor = new Color(0.6f, 0.6f, 1f);
    [SerializeField] private Color _deadColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private CombatUnit _unit;
    private Color _normalColor;

    void Start()
    {
        _unit = GetComponent<CombatUnit>();
        _normalColor = _spriteRenderer.color;

        _activeMarker.SetActive(false);
        _targetMarker.SetActive(false);

        _combatManager.OnTurnStarted += HandleTurnStarted;
        _combatManager.OnTargetSelectionStarted += HandleTargetSelectionStarted;
        _combatManager.OnTargetSelectionEnded += HideTargetMarker;
        _combatManager.OnCombatEnded += HandleCombatEnded;

        _unit.OnHit += HandleHit;
        _unit.OnHealed += HandleHealed;
        _unit.OnDownChanged += HandleDownChanged;
        _unit.OnGuardChanged += HandleGuardChanged;
        _unit.Health.OnDeath += UpdateColor;
    }

    void OnDestroy()
    {
        if (_combatManager != null)
        {
            _combatManager.OnTurnStarted -= HandleTurnStarted;
            _combatManager.OnTargetSelectionStarted -= HandleTargetSelectionStarted;
            _combatManager.OnTargetSelectionEnded -= HideTargetMarker;
            _combatManager.OnCombatEnded -= HandleCombatEnded;
        }

        if (_unit != null)
        {
            _unit.OnHit -= HandleHit;
            _unit.OnHealed -= HandleHealed;
            _unit.OnDownChanged -= HandleDownChanged;
            _unit.OnGuardChanged -= HandleGuardChanged;
            _unit.Health.OnDeath -= UpdateColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _combatManager.SelectTarget(_unit);
    }

    void HandleTurnStarted(CombatUnit activeUnit)
    {
        _activeMarker.SetActive(activeUnit == _unit);
    }

    void HandleTargetSelectionStarted(IReadOnlyList<CombatUnit> targets)
    {
        bool isTarget = false;
        foreach (CombatUnit target in targets)
        {
            if (target == _unit) isTarget = true;
        }
        _targetMarker.SetActive(isTarget);
    }

    void HideTargetMarker()
    {
        _targetMarker.SetActive(false);
    }

    void HandleCombatEnded(bool won, int reward, int shards)
    {
        _activeMarker.SetActive(false);
        _targetMarker.SetActive(false);
    }

    void HandleHit(int damage, HitResult result)
    {
        switch (result)
        {
            case HitResult.Nullified:
                SpawnPopup("NULL", Color.gray);
                break;
            case HitResult.Resisted:
                SpawnPopup($"{damage}\nRESIST", Color.gray);
                break;
            case HitResult.Weak:
                SpawnPopup($"{damage}\nWEAK!", Color.yellow);
                break;
            case HitResult.KnockedDown:
                SpawnPopup($"{damage}\nWEAK! DOWN", Color.yellow);
                break;
            default:
                SpawnPopup(damage.ToString(), Color.white);
                break;
        }
    }

    void HandleHealed(int amount)
    {
        SpawnPopup("+" + amount, Color.green);
    }

    void HandleGuardChanged(bool isGuarding)
    {
        if (isGuarding) SpawnPopup("GUARD", Color.cyan);
    }

    void HandleDownChanged(bool isDown)
    {
        UpdateColor();
    }

    void UpdateColor()
    {
        if (_unit.IsDead) _spriteRenderer.color = _deadColor;
        else if (_unit.IsDown) _spriteRenderer.color = _downColor;
        else _spriteRenderer.color = _normalColor;
    }

    void SpawnPopup(string text, Color color)
    {
        if (_popupPrefab == null) return;

        Vector3 position = _popupPoint != null ? _popupPoint.position : transform.position;
        DamagePopup popup = Instantiate(_popupPrefab, position, Quaternion.identity);
        popup.Setup(text, color);
    }
}
