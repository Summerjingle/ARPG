using UnityEngine;

public class BossDieState : State<BossController>
{
    [Header("Death Petrification")]
    [SerializeField] private float petrificationDuration = 2f;

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

        // 移除大招形态材质
        if (owner.ultimateState != null)
            owner.ultimateState.RemovePhaseMaterial();

        owner.fighter.PlayDeathAnimation(null);

        Debug.Log("<color=red>[Boss] 死亡</color>");

    }

    /// <summary>死亡动画播完后由 Animation Event 调用，开始石化</summary>
    public void StartPetrification()
    {
        var petrification = owner.GetComponent<PetrificationController>();
        if (petrification != null)
            petrification.PetrifyOverTime(petrificationDuration);
    }
}
