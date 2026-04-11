using UnityEngine;

public class BossStunnedState : State<BossController>
{
    public float stunDuration = 3.0f; // 瘫痪3秒
    private float currentTimer;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        currentTimer = stunDuration;
        
        Debug.Log("<color=red>Boss 护盾被击破！进入 Stunned 瘫痪状态！</color>");
        
        // 关键：强制让 Boss 停下所有移动脚步
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
        }
    }

    public override void Execute()
    {
        currentTimer -= Time.deltaTime;
        
        if (currentTimer <= 0)
        {
            Debug.Log("Boss 从瘫痪中恢复，重新振作！");
            // 恢复后切回 Idle，它会自己决定是继续追还是发呆
            owner.ChangeState(owner.idleState); 
        }
    }
}