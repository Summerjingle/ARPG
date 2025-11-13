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

    IEnumerator Attack(int comboCount = 1)
    {
        isAttacking = true;
        enemyController.Animator.applyRootMotion = true;

        Debug.Log($"=== 攻击开始，计划连击: {comboCount}次 ===");

        // 第一次攻击
        enemyController.CombatSystem?.TryToAttack(enemyController.Target);
        Debug.Log($"第一次攻击触发");

        // 连击循环
        for (int i = 1; i < comboCount; i++)
        {
            Debug.Log($"等待第{i + 1}次攻击条件...");

            // 等待进入Cooldown状态
            yield return new WaitUntil(() =>
            {
                bool condition = enemyController.Fighter.Attackstate == AttackStates.Cooldown;
                if (condition) Debug.Log($"检测到Cooldown状态，准备第{i + 1}次攻击");
                return condition;
            });

            Debug.Log($"执行第{i + 1}次攻击");
            enemyController.CombatSystem?.TryToAttack(enemyController.Target);
        }

        Debug.Log($"等待攻击完全结束...");
        yield return new WaitUntil(() => enemyController.Fighter.Attackstate == AttackStates.Idle);

        enemyController.Animator.applyRootMotion = false;
        isAttacking = false;
        Debug.Log($"=== 攻击序列完成 ===");

        if (enemyController.IsInState(EnemyStates.Attack))
        {
            enemyController.ChangerState(EnemyStates.RetreatAfterAttack);
        }
    }
    public override void Exit()
    {
        enemyController.NavAgent.ResetPath();
    }
}
