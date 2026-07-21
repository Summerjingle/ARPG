using UnityEngine;

/// <summary>
/// Boss 专用 Fighter —— 继承 EnemyFighter，去掉受击动画/音效（由 BossController 状态机管理动画）。
/// </summary>
public class BossFighter : EnemyFighter
{
    [Header("Spine Mask Hit Reaction")]
    [SerializeField] string hitReactionAnim = "hit_light_front";
    [SerializeField] int spineMaskLayer = 2;

    public override bool CanBeExecuted => false;

    protected override void Awake()
    {
        base.Awake();
        attackAnimLayer = 1; // Boss 攻击动画在 Action Layer
    }

    /// <summary>Boss 不需要 PlayHitReaction（状态机自己管动画）。音效和血液特效已在基类 OnTriggerEnter 中处理。</summary>
    protected override void OnHitReaction(ICombatSystem attacker, string specialReaction)
    {
        if (HealthSystem.IsDead)
            PlayDeathAnimation(attacker);
        else
            animator.Play(hitReactionAnim, spineMaskLayer, 0f);
    }
}
