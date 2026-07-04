using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State<EnemyController >
{
	[SerializeField] private float attackDistance = 1f;
	private EnemyController enemy;
    private bool isAttacking;
    private EnemyFighter ef;

    public override void Enter(EnemyController owner)
    {
        enemy = owner;
        ef = enemy.Fighter as EnemyFighter;
        enemy.NavAgent.stoppingDistance = attackDistance;

        // 攻击状态不是防御状态：关闭格挡并重置反制标记
        enemy.Fighter.InCounter = false;
        if (ef != null && ef.blockObject != null)
            ef.blockObject.SetActive(false);
    }

    public override void Execute()
    {
        if (isAttacking) return;
        enemy.NavAgent.SetDestination(enemy.Target.transform.position);
        if (Vector3.Distance(enemy.Target.transform.position, enemy.transform.position) <= attackDistance + 0.03f)
        {
            int maxCombo = ef != null ? ef.Attacks.Count : 1;
            StartCoroutine(Attack(Random.Range(0, maxCombo + 1)));
        }
    }

    protected IEnumerator Attack(int comboCount=1)
    {
        isAttacking = true;
        enemy.Animator.applyRootMotion=true;
        ef.TryToAttack(enemy.Target);
        for (int i = 0; i < comboCount; i++)
        {
            yield return new WaitUntil(() =>
                ef.Attackstate == AttackStates.Cooldown ||
                ef.Attackstate == AttackStates.Idle);

            if (ef.Attackstate == AttackStates.Idle) break;

            ef.TryToAttack();
        }

        yield return new WaitUntil( () =>ef.Attackstate==AttackStates.Idle);
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
        ef.DisableHitboxes();
        enemy.NavAgent.isStopped = false;
        enemy.NavAgent.ResetPath();
    }
}
