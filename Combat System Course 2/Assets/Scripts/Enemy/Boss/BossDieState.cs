using UnityEngine;

public class BossDieState : State<BossController>
{
    
    public override void Enter(BossController owner)
    {
        
        base.Enter(owner);

        owner.StopAllCoroutines();
        owner.fighter.DisableHitboxes();
        owner.fighter.ForceResetAttackState();

        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
            owner.agent.ResetPath();
            owner.agent.enabled = false;
        }

        // 禁用碰撞体
        var col = owner.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        owner.fighter.PlayDeathAnimation(null);

        Debug.Log("<color=red>[Boss] 死亡</color>");
        
    }
}
