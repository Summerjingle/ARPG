using UnityEngine;

public class BossChaseState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        owner.detectRange = 30f;

        if (owner.agent != null)
        {
            owner.agent.isStopped = false;
            owner.agent.speed = owner.runSpeed;
            owner.agent.stoppingDistance = 0f;
        }
    }

    public override void Execute()
    {
        if (owner.playerTarget == null) return;

        float dist = owner.GetFlatDistanceToPlayer();
        owner.anim?.SetFloat("Speed", owner.agent.velocity.magnitude);

        // 自己控制转向，不用 NavAgent 的 updateRotation
        owner.FacePlayer();

        if (dist <= owner.attackRange)
        {
            // 冷却中不急着切 Attack，继续追逐移动
            if (owner.CanAttack())
            {
                owner.agent.isStopped = true;
                owner.ChangeState(owner.attackState);
                return;
            }
        }

        if (dist > owner.detectRange * 1.5f)
        {
            owner.agent.isStopped = true;
            owner.ChangeState(owner.idleState);
            return;
        }

        owner.agent.isStopped = false;
        owner.agent.SetDestination(owner.playerTarget.position);
    }

    public override void Exit()
    {
        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;
    }
}
