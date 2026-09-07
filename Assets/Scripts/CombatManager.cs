using UnityEngine;
using System.Collections;

public enum CombatState { Start, PlayerTurn, EnemyTurn, Won, Lost }

public class CombatManager : MonoBehaviour
{
    public CombatState state;

    // scene refs
    public HealthSystem player;
    public HealthSystem enemy;

    // data refs
    public CharacterStats playerStats;
    public CharacterStats enemyStats;

    void Start()
    {
        state = CombatState.Start;

        // feed data to health systems
        player.Initialize(playerStats);
        enemy.Initialize(enemyStats);

        StartCoroutine(SetupCombat());
    }

    IEnumerator SetupCombat()
    {
        // wait before starting
        yield return new WaitForSeconds(1f);

        state = CombatState.PlayerTurn;
        Debug.Log("player turn");
    }

    public void OnPlayerAttack()
    {
        // block spam clicks
        if (state != CombatState.PlayerTurn) return;

        enemy.TakeDamage(5);
        Debug.Log("player attacked");

        state = CombatState.EnemyTurn;
        StartCoroutine(EnemyAction());
    }

    IEnumerator EnemyAction()
    {
        // fake thinking delay
        yield return new WaitForSeconds(1.5f);

        player.TakeDamage(3);
        Debug.Log("enemy attacked player turn");

        state = CombatState.PlayerTurn;
    }
}