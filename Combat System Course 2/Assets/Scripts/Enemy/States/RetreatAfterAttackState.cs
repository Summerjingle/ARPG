using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetreatAfterAttackState : State<EnemyController>
{
    [SerializeField] private float distanceToRetreat = 3f;
    [SerializeField] private float backwardSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 500f;

    EnemyController enemyController;
    Vector3 targetPos;

    public override void Enter(EnemyController owner)
    {
       
        enemyController = owner;
        targetPos = enemyController.Target.transform.position;
    }
    public override void Execute()
    {
        if (Vector3.Distance(enemyController.transform.position, targetPos) >= distanceToRetreat)
        {
            enemyController.ChangerState(EnemyStates.CombatMovement);
            return;
        }
       var vecToTarget= enemyController.Target.transform.position - enemyController.transform.position;
        enemyController.NavAgent.Move(-vecToTarget.normalized * backwardSpeed * Time.deltaTime);
        vecToTarget.y = 0f;
        transform.rotation=Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vecToTarget), rotationSpeed * Time.deltaTime);
    }
    
}
