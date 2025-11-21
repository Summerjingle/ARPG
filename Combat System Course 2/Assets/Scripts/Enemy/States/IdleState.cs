using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : State<EnemyController>
{
    private EnemyController enemyController;
    public override void Enter(EnemyController owner)
    {
        enemyController = owner;
        
        enemyController?.Animator?.SetBool("combatMode", false);

    }

    public override void Execute()
    {
        enemyController.Target = enemyController.FindTarget();
        if (enemyController.Target != null)
        {
            enemyController.ChangerState(EnemyStates.CombatMovement);
        }
    }
    public override void Exit()
    {

    }
}