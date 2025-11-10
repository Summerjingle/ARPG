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
        enemyController.Fighter.OnHitComplete += () => StartCoroutine(GoToCombatMovement());
    }
    IEnumerator GoToCombatMovement()
    {
        yield return new WaitForSeconds(stunnTime);

        // 多重检查确保可以安全切换状态
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