using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State<EnemyController >
{
	[SerializeField] private float attackDistance = 1f;
	private EnemyController enemy;
    private bool isAttacking;

    public override void Enter(EnemyController owner)
    {
        enemy = owner;
        enemy.NavAgent.stoppingDistance=attackDistance;
    }

    public override void Execute()
    {  
        if (isAttacking) return;
        enemy.NavAgent.SetDestination(enemy.Target.transform.position);
        if (Vector3.Distance(enemy.Target.transform.position, enemy.transform.position) <= attackDistance + 0.03f)
            StartCoroutine(Attack(Random.Range(0,enemy.Fighter.Attacks.Count + 1)));//这里加1，是因为第二个参数是互斥的，开始连击前总会先攻击一次，如果连击数是3，那么加上开始的一次就是4了

    }

    protected IEnumerator Attack(int comboCount=1)
    {
        isAttacking = true;
        enemy.Animator.applyRootMotion=true;
        enemy.Fighter.TryToAttack(enemy.Target);
        for (int i = 0; i < comboCount; i++)
        {
            yield return new WaitUntil(() => enemy.Fighter.Attackstate == AttackStates.Cooldown);
            enemy.Fighter.TryToAttack();
        }

        yield return new WaitUntil( () =>enemy.Fighter.Attackstate==AttackStates.Idle);
        enemy.Animator.applyRootMotion=false;
        isAttacking=false;
        enemy.ChangerState(EnemyStates.RetreatAfterAttack);
    }
    public override void Exit()
    {
        enemy.NavAgent.ResetPath();
    }
}
