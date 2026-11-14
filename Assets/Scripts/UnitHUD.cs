using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HP/MP panel of one unit, any field except _unit can be left empty
public class UnitHUD : MonoBehaviour
{
    [SerializeField] private CombatUnit _unit;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _mpText;
    // Image Type must be Filled
    [SerializeField] private Image _hpFill;
    [SerializeField] private Image _mpFill;

    void Start()
    {
        // slot is not used in this fight
        if (!_unit.IsInitialized)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_nameText != null) _nameText.text = _unit.Stats.CharacterName;

        _unit.Health.OnHealthChanged += UpdateHealth;
        _unit.Health.OnManaChanged += UpdateMana;

        UpdateHealth(_unit.Health.CurrentHealth, _unit.Health.MaxHealth);
        UpdateMana(_unit.Health.CurrentMana, _unit.Health.MaxMana);
    }

    void OnDestroy()
    {
        if (_unit == null) return;

        _unit.Health.OnHealthChanged -= UpdateHealth;
        _unit.Health.OnManaChanged -= UpdateMana;
    }

    void UpdateHealth(int current, int max)
    {
        if (_hpText != null) _hpText.text = $"HP {current} / {max}";
        if (_hpFill != null) _hpFill.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    void UpdateMana(int current, int max)
    {
        if (_mpText != null) _mpText.text = $"MP {current} / {max}";
        if (_mpFill != null) _mpFill.fillAmount = max > 0 ? (float)current / max : 0f;
    }
}
