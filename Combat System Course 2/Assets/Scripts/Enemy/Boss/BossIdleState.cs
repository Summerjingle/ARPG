using UnityEngine;

public class BossIdleState : State<BossController>
{


    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        owner.anim?.SetFloat("Speed", 0);
        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;
    }

    public override void Execute()
    {
        if (owner.playerTarget == null) return;

        owner.FacePlayer();

        float dist = owner.GetFlatDistanceToPlayer();
        if (dist > owner.detectRange) return;

        if (dist > owner.attackRange)
            owner.ChangeState(owner.chaseState);
        else
            owner.ChangeState(owner.attackState);
    }

    public override void Exit()
    {
        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = false;
    }
}
