using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State<EnemyController >
{
	[SerializeField] float attackDistance = 1f;

	private EnemyController enemyController;
	private bool isAttacking;
	public override void Enter(EnemyController owner)
	{
		enemyController = owner;
		enemyController.NavAgent.stoppingDistance=attackDistance;
	}
    public override void Execute()
    {
		if (isAttacking) return;
		enemyController.NavAgent.SetDestination(enemyController.Target.transform.position);
		if (Vector3.Distance(enemyController.Target.transform.position, enemyController.transform.position) <= attackDistance + 0.03f)
		{
			StartCoroutine(Attack(Random.Range(0,enemyController.Fighter.Attacks.Count+1	 )));  
		}
    }
	
	IEnumerator Attack(int comboCount=1)
	{
		isAttacking= true;
		enemyController.Animator.applyRootMotion=true;
		enemyController.Fighter.TryToAttack(enemyController.Target);
		for (int i = 1; i < comboCount; i++)
		{
            yield return new WaitUntil(() => enemyController.Fighter.Attackstate == AttackStates.Cooldown);
            enemyController.Fighter.TryToAttack(enemyController.Target);
        }
		yield return new WaitUntil(() => enemyController.Fighter.Attackstate == AttackStates.Idle);
        enemyController.Animator.applyRootMotion = false;
        isAttacking = false;

		if (enemyController.IsInState(EnemyStates.Attack)) { enemyController.ChangerState(EnemyStates.RetreatAfterAttack); }
		
	}
    public override void Exit()
    {
        enemyController.NavAgent.ResetPath();
    }
}
