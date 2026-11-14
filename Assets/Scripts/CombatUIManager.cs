using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CombatUIManager : MonoBehaviour
{
    [SerializeField] private CombatManager _combatManager;

    [Header("Turn Info")]
    [SerializeField] private TextMeshProUGUI _turnText;
    [SerializeField] private TextMeshProUGUI _messageText;

    [Header("Main Menu")]
    [SerializeField] private GameObject _actionMenu;
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _openSkillsButton;
    [SerializeField] private Button _guardButton;

    [Header("All-Out Attack (optional)")]
    // shown only when every enemy is down, can stay empty until the button exists
    [SerializeField] private Button _allOutAttackButton;

    [Header("Skills Sub-Menu")]
    [SerializeField] private GameObject _skillPanel;
    [SerializeField] private Button[] _abilityButtons;
    [SerializeField] private TextMeshProUGUI[] _abilityButtonTexts;

    [Header("Target Selection")]
    [SerializeField] private GameObject _targetHint;
    [SerializeField] private Button _cancelTargetButton;

    [Header("Result Screen")]
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _resultTitleText;
    [SerializeField] private TextMeshProUGUI _resultRewardText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _continueButtonText;

    [Header("Scenes")]
    // roguelike flow: win goes on to the map, death sends the player back to the hub.
    // empty name reloads this scene, useful while those scenes do not exist yet
    [SerializeField] private string _mapSceneName;
    [SerializeField] private string _hubSceneName;

    private bool _isWon;

    void Start()
    {
        _combatManager.OnStateChanged += HandleStateChanged;
        _combatManager.OnTurnStarted += HandleTurnStarted;
        _combatManager.OnActionStarted += HandleActionStarted;
        _combatManager.OnGuardUsed += HandleGuardUsed;
        _combatManager.OnOneMore += HandleOneMore;
        _combatManager.OnAllOutAttack += HandleAllOutAttack;
        _combatManager.OnCombatEnded += HandleCombatEnded;

        if (_allOutAttackButton != null)
        {
            _allOutAttackButton.onClick.AddListener(_combatManager.SelectAllOutAttack);
            _allOutAttackButton.gameObject.SetActive(false);
        }

        _attackButton.onClick.AddListener(OnAttackClicked);
        _openSkillsButton.onClick.AddListener(ToggleSkillPanel);
        _guardButton.onClick.AddListener(OnGuardClicked);
        _cancelTargetButton.onClick.AddListener(_combatManager.CancelTargetSelection);
        _continueButton.onClick.AddListener(OnContinueClicked);

        _actionMenu.SetActive(false);
        _skillPanel.SetActive(false);
        _targetHint.SetActive(false);
        _resultPanel.SetActive(false);
        _turnText.text = "";
        _messageText.text = "";
    }

    void OnDestroy()
    {
        if (_combatManager == null) return;

        _combatManager.OnStateChanged -= HandleStateChanged;
        _combatManager.OnTurnStarted -= HandleTurnStarted;
        _combatManager.OnActionStarted -= HandleActionStarted;
        _combatManager.OnGuardUsed -= HandleGuardUsed;
        _combatManager.OnOneMore -= HandleOneMore;
        _combatManager.OnAllOutAttack -= HandleAllOutAttack;
        _combatManager.OnCombatEnded -= HandleCombatEnded;
    }

    // ---------- combat events ----------

    void HandleStateChanged(CombatState state)
    {
        _actionMenu.SetActive(state == CombatState.PlayerTurn);
        _targetHint.SetActive(state == CombatState.TargetSelection);
        if (state != CombatState.PlayerTurn) _skillPanel.SetActive(false);

        if (_allOutAttackButton != null)
        {
            _allOutAttackButton.gameObject.SetActive(_combatManager.CanAllOutAttack);
        }
    }

    void HandleTurnStarted(CombatUnit unit)
    {
        _turnText.text = unit.Stats.CharacterName + "'s turn";
        if (unit.IsPlayerTeam) SetupAbilityButtons(unit);
    }

    void HandleActionStarted(CombatUnit unit, AbilityData ability)
    {
        _messageText.text = unit.Stats.CharacterName + ": " + ability.AbilityName;
    }

    void HandleGuardUsed(CombatUnit unit)
    {
        _messageText.text = unit.Stats.CharacterName + " guards";
    }

    void HandleOneMore(CombatUnit unit)
    {
        _messageText.text = "1 MORE!";
    }

    void HandleAllOutAttack()
    {
        _messageText.text = "ALL-OUT ATTACK!";
    }

    void HandleCombatEnded(bool won, int reward, int shards)
    {
        _isWon = won;
        _turnText.text = "";
        _resultPanel.SetActive(true);

        // rewards are already saved, so they are kept even after death
        string shardsLine = shards > 0 ? $"\n+{shards} shards (total {PlayerProgress.Shards})" : "";

        if (won)
        {
            _resultTitleText.text = "VICTORY";
            _resultRewardText.text = $"+{reward} currency (total {PlayerProgress.Currency})" + shardsLine;
            _continueButtonText.text = "Continue";
        }
        else
        {
            _resultTitleText.text = "DEFEAT";
            _resultRewardText.text = $"Currency: {PlayerProgress.Currency}" + shardsLine;
            _continueButtonText.text = "Return to Hub";
        }
    }

    // ---------- buttons ----------

    void OnAttackClicked()
    {
        _skillPanel.SetActive(false);
        _combatManager.SelectAbility(_combatManager.BasicAttack);
    }

    void OnGuardClicked()
    {
        _skillPanel.SetActive(false);
        _combatManager.SelectGuard();
    }

    void ToggleSkillPanel()
    {
        _skillPanel.SetActive(!_skillPanel.activeSelf);
    }

    void OnAbilityClicked(AbilityData ability)
    {
        _skillPanel.SetActive(false);
        _combatManager.SelectAbility(ability);
    }

    void OnContinueClicked()
    {
        string sceneName = _isWon ? _mapSceneName : _hubSceneName;

        if (string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else SceneManager.LoadScene(sceneName);
    }

    // menu is rebuilt for every active hero
    void SetupAbilityButtons(CombatUnit unit)
    {
        foreach (Button button in _abilityButtons)
        {
            button.gameObject.SetActive(false);
        }

        IReadOnlyList<AbilityData> abilities = unit.Stats.Abilities;
        int buttonIndex = 0;

        for (int i = 0; i < abilities.Count && buttonIndex < _abilityButtons.Length; i++)
        {
            AbilityData ability = abilities[i];
            if (!PlayerProgress.IsAbilityUnlocked(ability)) continue;

            Button button = _abilityButtons[buttonIndex];
            button.gameObject.SetActive(true);

            // formatting: name on first line, MP cost in blue on second line
            _abilityButtonTexts[buttonIndex].text = $"{ability.AbilityName}\n<color=#55aaff>{ability.ManaCost} MP</color>";

            // not enough mana: button is visible but disabled
            button.interactable = _combatManager.CanUse(unit, ability);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnAbilityClicked(ability));

            buttonIndex++;
        }
    }
}
