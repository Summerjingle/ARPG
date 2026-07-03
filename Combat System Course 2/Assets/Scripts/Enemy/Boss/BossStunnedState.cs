using UnityEngine;

public class BossStunnedState : State<BossController>
{
    public float stunDuration = 3f;
    private float timer;
    private bool hasTriggeredEnd = false;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        timer = stunDuration;
        hasTriggeredEnd = false;  

        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;

        // 中断当前攻击
        owner.fighter.DisableHitboxes();
        owner.fighter.ForceResetAttackState();
        owner.anim.Play("Stun_Start",1);
        Debug.Log("<color=red>[Boss] 破势！进入 Stunned</color>");
    }

    public override void Execute()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            owner.ChangeState(owner.idleState);
    }

    public override void Exit()
    {
        owner.anim.ResetTrigger("StunEnd");
        Debug.Log("[Boss] 从 Stunned 恢复");
    }
}
