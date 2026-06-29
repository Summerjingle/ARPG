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
        enemy.NavAgent.stoppingDistance = attackDistance;

        // 攻击状态不是防御状态：关闭格挡并重置反制标记
        enemy.Fighter.InCounter = false;
        var fighter = enemy.Fighter as EnemyFighter;
        if (fighter != null && fighter.blockObject != null)
            fighter.blockObject.SetActive(false);
    }

    public override void Execute()
    {  
        if (isAttacking) return;
        enemy.NavAgent.SetDestination(enemy.Target.transform.position);
        if (Vector3.Distance(enemy.Target.transform.position, enemy.transform.position) <= attackDistance + 0.03f)
            StartCoroutine(Attack(Random.Range(0,enemy.Fighter.Attacks.Count + 1)));//�����1������Ϊ�ڶ��������ǻ���ģ���ʼ����ǰ�ܻ��ȹ���һ�Σ������������3����ô���Ͽ�ʼ��һ�ξ���4��

    }

    protected IEnumerator Attack(int comboCount=1)
    {
        isAttacking = true;
        enemy.Animator.applyRootMotion=true;
        enemy.Fighter.TryToAttack(enemy.Target);
        for (int i = 0; i < comboCount; i++)
        {
            // 同时监听 Cooldown 和 Idle：弹反会跳过 Cooldown 直接设 Idle
            yield return new WaitUntil(() =>
                enemy.Fighter.Attackstate == AttackStates.Cooldown ||
                enemy.Fighter.Attackstate == AttackStates.Idle);

            if (enemy.Fighter.Attackstate == AttackStates.Idle) break;

            enemy.Fighter.TryToAttack();
        }

        yield return new WaitUntil( () =>enemy.Fighter.Attackstate==AttackStates.Idle);
        enemy.Animator.applyRootMotion=false;
        isAttacking=false;
        if (!enemy.IsInState(EnemyStates.Dead))
            enemy.ChangerState(EnemyStates.RetreatAfterAttack);
    }
    public override void Exit()
    {
        StopAllCoroutines();
        isAttacking = false;
        enemy.Animator.applyRootMotion = false;
        enemy.Fighter.DisableHitboxes();
        enemy.NavAgent.isStopped = false;
        enemy.NavAgent.ResetPath();
    }
}
