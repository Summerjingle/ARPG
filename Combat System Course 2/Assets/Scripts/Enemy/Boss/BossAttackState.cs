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

    private IEnumerator DoAttack(AttackData attack)
    {
        isAttacking = true;

        owner.anim.applyRootMotion = true;

        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        owner.fighter.TryToAttack(target);

        // 仅在 ImpactStartTime 之前追踪玩家朝向，进入 impact 后停止旋转
        int layer = owner.fighter.AttackAnimLayer;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            float normalizedTime = owner.anim.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            if (!attack.IsSpinAttack && normalizedTime < attack.ImpactStartTime)
                owner.FacePlayer();
            yield return null;
        }

        owner.anim.applyRootMotion = false;
        isAttacking = false;
        owner.ChangeState(owner.chaseState);
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
