using UnityEngine;

public class BossIdleState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        // 进入 Idle 时确保代理停止，防止滑行
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
        }
    }

    public override void Execute()
{
    if (owner.playerTarget == null) return;

    // 平面距离计算
    Vector3 bossPos = owner.transform.position;
    Vector3 playerPos = owner.playerTarget.position;
    bossPos.y = 0; playerPos.y = 0;
    float distance = Vector3.Distance(bossPos, playerPos);

    // 🌟 修正点：只有进入 detectRange 才会触发状态切换
    if (distance <= owner.detectRange) 
    {
        if (distance > owner.attackRange)
        {
            owner.ChangeState(owner.chaseState);
        }
        else
        {
            owner.ChangeState(owner.meleeState);
        }
    }
    // 否则：如果不满足 detectRange，Boss 会一直留在 Idle 状态，什么也不做
}

    public override void Exit()
    {
        // 退出 Idle 时，记得重新开启导航动力
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = false;
        }
    }
}