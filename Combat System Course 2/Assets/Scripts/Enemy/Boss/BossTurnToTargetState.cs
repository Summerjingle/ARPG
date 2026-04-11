using UnityEngine;

public class BossTurnToTargetState : State<BossController>
{
     public override void Enter(BossController owner)
    {
        base.Enter(owner);
        Debug.Log("<color=red>Boss 进入了 [turntotarget] 状态：开始寻找目标！</color>");
    }
    public override void Execute()
{
    if (owner.playerTarget == null) return;

    // 1. 计算从 Boss 指向玩家的向量
    Vector3 targetDir = owner.playerTarget.position - owner.transform.position;
    targetDir.y = 0; // 忽略高度差，只在水平面上转动

    if (targetDir != Vector3.zero)
    {
        // 2. 计算目标旋转角度
        Quaternion targetRotation = Quaternion.LookRotation(targetDir);
        
        // 3. 平滑转向 (Lerp/Slerp)
        // turnSpeed 建议设为 5 到 10 左右
        owner.transform.rotation = Quaternion.Slerp(
            owner.transform.rotation, 
            targetRotation, 
            Time.deltaTime * 5f 
        );

        // 4. 判断是否“转正”了
        float angle = Vector3.Angle(owner.transform.forward, targetDir);
        if (angle < 5f) // 如果角度小于5度，说明基本对齐了
        {
            Debug.Log("转向完成，发起近战攻击！");
            owner.ChangeState(owner.meleeState); 
        }
    }
}
}