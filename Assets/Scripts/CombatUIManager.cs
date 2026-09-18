using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatUIManager : MonoBehaviour
{
    [Header("HUD")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI mpText;

    [Header("Main Menu")]
    public Button attackButton;
    public Button openSkillsButton;

    [Header("Skills Sub-Menu")]
    public GameObject skillPanel;
    public Button[] abilityButtons;
    public TextMeshProUGUI[] abilityButtonTexts;

    private HealthSystem trackedTarget;
    private CombatManager combatManager;

    public void InitializeTarget(HealthSystem target, CombatManager manager)
    {
        trackedTarget = target;
        combatManager = manager;

        nameText.text = trackedTarget.stats.characterName;

        trackedTarget.OnHealthChanged += UpdateHPText;
        trackedTarget.OnManaChanged += UpdateMPText;

        UpdateHPText(trackedTarget.currentHealth, trackedTarget.stats.maxHealth);
        UpdateMPText(trackedTarget.currentMana, trackedTarget.stats.maxMana);

        attackButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(OnBasicAttackClicked);

        openSkillsButton.onClick.RemoveAllListeners();
        openSkillsButton.onClick.AddListener(ToggleSkillPanel);

        skillPanel.SetActive(false);
        SetupAbilityButtons();
    }

    void OnBasicAttackClicked()
    {
        skillPanel.SetActive(false);
        combatManager.OnPlayerBasicAttack();
    }

    void ToggleSkillPanel()
    {
        skillPanel.SetActive(!skillPanel.activeSelf);
    }

    void SetupAbilityButtons()
    {
        foreach (var btn in abilityButtons)
        {
            btn.gameObject.SetActive(false);
        }

        for (int i = 0; i < trackedTarget.stats.abilities.Count; i++)
        {
            if (i >= abilityButtons.Length) break;

            AbilityData ability = trackedTarget.stats.abilities[i];
            if (!ability.isUnlockedByDefault) continue;

            abilityButtons[i].gameObject.SetActive(true);

            // formatting: name on first line, MP cost in blue on second line
            abilityButtonTexts[i].text = $"{ability.abilityName}\n<color=#55aaff>{ability.mpCost} MP</color>";

            abilityButtons[i].onClick.RemoveAllListeners();
            abilityButtons[i].onClick.AddListener(() => {
                skillPanel.SetActive(false);
                combatManager.OnPlayerUseAbility(ability);
            });
        }
    }

    void UpdateHPText(int currentHP, int maxHP)
    {
        hpText.text = $"HP {currentHP} / {maxHP}";
    }

    void UpdateMPText(int currentMP, int maxMP)
    {
        mpText.text = $"MP {currentMP} / {maxMP}";
    }

    void OnDestroy()
    {
        if (trackedTarget != null)
        {
            trackedTarget.OnHealthChanged -= UpdateHPText;
            trackedTarget.OnManaChanged -= UpdateMPText;
        }
    }
}