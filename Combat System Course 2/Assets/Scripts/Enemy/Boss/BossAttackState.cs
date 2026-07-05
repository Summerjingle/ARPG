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

        // 攻击全程追踪玩家朝向（非旋转攻击时）
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            if (!attack.IsSpinAttack)
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
