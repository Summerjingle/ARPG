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

        StartCoroutine(DoAttack(selected, zone));
    }

    private IEnumerator DoAttack(AttackData currentAttack, BossPositionZone zone)
    {
        isAttacking = true;
        owner.anim.applyRootMotion = false;

        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        owner.fighter.TryToAttack(target);

        bool isBackAttack = (zone == BossPositionZone.Back);

        int layer = owner.fighter.AttackAnimLayer;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            float normalizedTime = owner.anim.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            if (!isBackAttack && !currentAttack.IsSpinAttack && normalizedTime < currentAttack.ImpactStartTime)
                owner.FacePlayer();
            yield return null;
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
