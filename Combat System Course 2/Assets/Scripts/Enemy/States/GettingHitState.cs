using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GettingHitState : State<EnemyController>
{
    private EnemyController enemyController;
    [SerializeField] float stunnTime = 0.5f;
    public override void Enter(EnemyController owner)
    {
        StopAllCoroutines();
        enemyController = owner;
        enemyController.Fighter.OnHitComplete += OnHitCompleteHandler;
    }

    public override void Exit()
    {
        if (enemyController != null && enemyController.Fighter != null)
            enemyController.Fighter.OnHitComplete -= OnHitCompleteHandler;
    }

    void OnHitCompleteHandler()
    {
        StartCoroutine(GoToCombatMovement());
    }

    IEnumerator GoToCombatMovement()
    {
        yield return new WaitForSeconds(stunnTime);

        if (enemyController != null &&
            enemyController.isActiveAndEnabled &&
            !enemyController.IsInState(EnemyStates.Dead) &&
            enemyController.Fighter != null &&
            !enemyController.Fighter.HealthSystem.IsDead)
        {
            enemyController.ChangerState(EnemyStates.CombatMovement);
        }
    }
}