using UnityEngine;

public class BossChaseState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        owner.detectRange=30;
        Debug.Log("Boss 进入了 [Chase] 状态：准备开始追逐！");
        
    }
    public override void Execute()
    {
        
        if (owner.playerTarget == null) return;
        
        owner.agent.SetDestination(owner.playerTarget.position);
        float velocity = owner.agent.velocity.magnitude;
        owner.SetAnimFloat("Speed", velocity);
        // 🌟 修复点 1：只计算 X 和 Z 轴的平面距离，忽略 Y 轴高度差
        Vector3 bossPos = owner.transform.position;
        Vector3 playerPos = owner.playerTarget.position;
        bossPos.y = 0; 
        playerPos.y = 0;

        float horizontalDistance = Vector3.Distance(bossPos, playerPos);

        // 🌟 修复点 2：这里建议你在 Inspector 里把 attackRange 设为 2.5 到 3.0 左右
        if (horizontalDistance <= owner.attackRange)
        {
            // 🌟 修复点 3：追到了就让导航停下，防止在攻击时 Boss 还在往前滑步
            owner.agent.isStopped = true; 
            
            Debug.Log($"追到了！当前平面距离: {horizontalDistance}");
            owner.ChangeState(owner.turnToTargetState); 
        }
        else if (horizontalDistance > owner.detectRange * 1.5f)
        {
            // 玩家跑太远，脱战
            owner.agent.isStopped = true;
            owner.ChangeState(owner.idleState);
        }
        else
        {
            // 还在追击中，确保代理是开启状态
            owner.agent.isStopped = false;
        }
    }
}