using UnityEngine;

public class BossMeleeState : State<BossController>
{
    private float attackTimer;
    private float totalAttackDuration = 1.5f; 
    private float trackingDuration = 0.8f;    
    private float rotationSpeed = 10f;        
    private bool hasDealtDamage;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        attackTimer = 0f;
        hasDealtDamage = false;

        // 1. 在上半身层播放攻击动画
        owner.PlayAnim("MeleeAttack", 0.1f, 1); 
        
        // 2. 暂时接管旋转权，防止导航系统与手动旋转冲突
        owner.agent.updateRotation = false;
        
        // 3. 初始攻击速度设为走速的一半
        owner.agent.speed = owner.walkSpeed * 0.5f; 
    }

    public override void Execute()
    {
        attackTimer += Time.deltaTime;

        // --- 动态移动与转向逻辑 ---
        if (owner.playerTarget != null)
        {
            float distance = Vector3.Distance(owner.transform.position, owner.playerTarget.position);

            // 如果玩家远离，则让 Boss 边走边砍追上去
            if (distance > 1.5f) 
            {
                owner.agent.isStopped = false;
                owner.agent.SetDestination(owner.playerTarget.position);
                // 动态调整速度，距离越远速度越快（最高不超过跑步速度）
                owner.agent.speed = Mathf.Lerp(owner.walkSpeed * 0.5f, owner.runSpeed, (distance - 1.5f) / 5f);
            }
            else
            {
                // 距离很近时减速，增强打击感
                owner.agent.speed = owner.walkSpeed * 0.3f;
            }

            // 在前摇阶段执行平滑转向
            if (attackTimer <= trackingDuration)
            {
                LookAtPlayerSmoothly();
            }
        }

        // 同步速度到 Animator，驱动 Base 层的混合树（走路/跑步动画）
        owner.SetAnimFloat("Speed", owner.agent.velocity.magnitude);

        // --- 伤害判定 ---
        if (!hasDealtDamage && attackTimer >= totalAttackDuration * 0.45f) 
        {
            PerformDamageCheck();
            hasDealtDamage = true;
        }

        // --- 状态结束 ---
        if (attackTimer >= totalAttackDuration)
        {
            owner.ChangeState(owner.chaseState);
        }
    }

    public override void Exit() 
    {
        owner.agent.updateRotation = true;
        owner.agent.speed = owner.runSpeed;
        
        Debug.Log("攻击结束，还原导航设置");
    }

    private void LookAtPlayerSmoothly()
    {
        if (owner.playerTarget == null) return;

        Vector3 dir = (owner.playerTarget.position - owner.transform.position).normalized;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    void PerformDamageCheck()
    {
        Debug.Log("<color=red>攻击判定生效</color>");
    }
}