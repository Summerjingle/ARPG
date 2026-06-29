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
        enemyController.Fighter.InCounter = true;
        SetBlockObjectActive(true);
        targetPos = enemyController.Target.transform.position;
    }
    public override void Execute()
    {
        if (enemyController.Target == null || enemyController.Target.HealthSystem.IsDead)
        {
            SetBlockObjectActive(false);
            enemyController.ChangerState(EnemyStates.CombatMovement);
            return;
        }
        if (Vector3.Distance(enemyController.transform.position, targetPos) >= distanceToRetreat)
        {
            SetBlockObjectActive(false);
            enemyController.ChangerState(EnemyStates.CombatMovement);
            return;
        }
       var vecToTarget= enemyController.Target.transform.position - enemyController.transform.position;
        enemyController.NavAgent.Move(-vecToTarget.normalized * backwardSpeed * Time.deltaTime);
        vecToTarget.y = 0f;
        transform.rotation=Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vecToTarget), rotationSpeed * Time.deltaTime);
    }

    public override void Exit()
    {
        enemyController.Fighter.InCounter = false;
        SetBlockObjectActive(false);
    }

    private void SetBlockObjectActive(bool active)
    {
        var fighter = enemyController.Fighter as EnemyFighter;
        if (fighter != null && fighter.blockObject != null)
            fighter.blockObject.SetActive(active);
    }

}
