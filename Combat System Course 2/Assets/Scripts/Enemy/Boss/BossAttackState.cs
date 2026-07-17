using System.Collections;
using UnityEngine;

public class BossAttackState : State<BossController>
{
    private bool isAttacking;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);

        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;

        owner.anim?.SetFloat("Speed", 0);
        owner.fighter.InCounter = false;
        isAttacking = false;
    }

    public override void Execute()
    {
        if (isAttacking) return;
        if (owner.playerTarget == null) return;

        if (!owner.CanAttack())
        {
            owner.ChangeState(owner.chaseState);
            return;
        }

        BossPositionZone zone = owner.DetectPlayerZone();
        var group = owner.GetAttackGroup(zone);
        if (group == null || group.Count == 0)
        {
            owner.ChangeState(owner.chaseState);
            return;
        }

        AttackData selected = group[Random.Range(0, group.Count)];
        owner.fighter.OverrideAttacks(new System.Collections.Generic.List<AttackData> { selected });
        owner.lastAttackTime = Time.time;

        StartCoroutine(DoAttack(selected));
    }

    private IEnumerator DoAttack(AttackData firstAttack)
    {
        isAttacking = true;
        AttackData currentAttack = firstAttack;

        while (true)
        {
            owner.anim.applyRootMotion = false;

            var target = owner.playerTarget?.GetComponent<ICombatSystem>();
            owner.fighter.TryToAttack(target);

            int layer = owner.fighter.AttackAnimLayer;
            while (owner.fighter.Attackstate != AttackStates.Idle)
            {
                float normalizedTime = owner.anim.GetCurrentAnimatorStateInfo(layer).normalizedTime;
                if (!currentAttack.IsSpinAttack && normalizedTime < currentAttack.ImpactStartTime)
                    owner.FacePlayer();
                yield return null;
            }

            // 连击判定：概率触发 + 玩家仍在攻击范围内
            if (Random.value < owner.comboChance && owner.playerTarget != null
                && owner.GetFlatDistanceToPlayer() <= owner.attackRange)
            {
                BossPositionZone zone = owner.DetectPlayerZone();
                var group = owner.GetAttackGroup(zone);
                if (group != null && group.Count > 0)
                {
                    currentAttack = group[Random.Range(0, group.Count)];
                    owner.fighter.OverrideAttacks(
                        new System.Collections.Generic.List<AttackData> { currentAttack });
                    owner.lastAttackTime = Time.time;
                    continue;
                }
            }

            break;
        }

        isAttacking = false;
        owner.ChangeState(owner.GetNextStateAfterAttack());
    }

    public override void Exit()
    {
        StopAllCoroutines();
        isAttacking = false;
        owner.fighter.DisableHitboxes();
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = false;
            owner.agent.ResetPath();
        }
    }
}
