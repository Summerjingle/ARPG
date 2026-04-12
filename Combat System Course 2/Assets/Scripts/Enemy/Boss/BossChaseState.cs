using UnityEngine;

public class BossChaseState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        // 追击时确保 detectRange 足够大
        owner.detectRange = 30f;
        
        // 🌟 核心修复：进入追击状态时，重置导航代理的基本属性
        if (owner.agent != null)
        {
            owner.agent.isStopped = false;
            owner.agent.stoppingDistance = 0f; // 追逐时先设为0，靠近了由 Melee 接管
            owner.agent.speed = owner.runSpeed;
            owner.agent.updateRotation = true; // 追逐时允许导航组件自动旋转模型
        }
        
        Debug.Log("Boss 进入了 [Chase] 状态：准备开始追逐！");
    }

    public override void Execute()
    {
        if (owner.playerTarget == null) return;
        
        // 🌟 核心修复：使用 Controller 里的统一平面距离算法
        float horizontalDistance = owner.GetFlatDistanceToPlayer();

        // 更新动画：根据 agent 的实时速度决定动画混合
        float velocity = owner.agent.velocity.magnitude;
        owner.SetAnimFloat("Speed", velocity);

        // 1. 距离判断：是否进入攻击范围
        if (horizontalDistance <= owner.attackRange )   // 改成 0.9f 或更小
{
    owner.agent.isStopped = true;
    Debug.Log($"追到了！当前平面距离: {horizontalDistance:F3}，切入近战状态！");
    owner.ChangeState(owner.meleeState);
}
        // 2. 距离判断：玩家是否跑得太远（脱战）
        else if (horizontalDistance > owner.detectRange * 1.5f)
        {
            owner.agent.isStopped = true;
            owner.ChangeState(owner.idleState);
        }
        // 3. 正常追击
        else
        {
            owner.agent.isStopped = false;
            // 实时更新玩家位置作为导航目标
            owner.agent.SetDestination(owner.playerTarget.position);
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 退出追击时，先停掉导航，防止滑步
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
        }
    }
}