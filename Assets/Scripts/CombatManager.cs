using UnityEngine;
using System.Collections;

public enum CombatState { Start, PlayerTurn, EnemyTurn, Won, Lost }

public class CombatManager : MonoBehaviour
{
    public CombatState state;

    public HealthSystem player;
    public HealthSystem enemy;
    public CombatUIManager uiManager;

    public CharacterStats playerStats;
    public CharacterStats enemyStats;

    void Start()
    {
        state = CombatState.Start;

        player.Initialize(playerStats);
        enemy.Initialize(enemyStats);

        uiManager.InitializeTarget(player, this);

        StartCoroutine(SetupCombat());
    }

    IEnumerator SetupCombat()
    {
        yield return new WaitForSeconds(1f);
        state = CombatState.PlayerTurn;
    }

    // method for standard attack without mana
    public void OnPlayerBasicAttack()
    {
        if (state != CombatState.PlayerTurn) return;

        enemy.TakeDamage(player.stats.baseDamage);
        Debug.Log("player used basic attack");

        state = CombatState.EnemyTurn;
        StartCoroutine(EnemyAction());
    }

    // method for persona skills
    public void OnPlayerUseAbility(AbilityData ability)
    {
        if (state != CombatState.PlayerTurn) return;

        if (ability.mpCost > 0)
        {
            bool hasMana = player.TryConsumeMana(ability.mpCost);
            if (!hasMana)
            {
                Debug.Log("not enough mana to cast " + ability.abilityName);
                return;
            }
        }

        enemy.TakeDamage(ability.damageAmount);
        Debug.Log("player used " + ability.abilityName);

        state = CombatState.EnemyTurn;
        StartCoroutine(EnemyAction());
    }

    IEnumerator EnemyAction()
    {
        yield return new WaitForSeconds(1.5f);
        player.TakeDamage(3);
        state = CombatState.PlayerTurn;
    }
}