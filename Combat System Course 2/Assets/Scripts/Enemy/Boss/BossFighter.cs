using UnityEngine;

/// <summary>
/// Boss 专用 Fighter —— 继承 EnemyFighter，去掉受击动画/音效（由 BossController 状态机管理动画）。
/// </summary>
public class BossFighter : EnemyFighter
{
    protected override void Awake()
    {
        base.Awake();
        attackAnimLayer = 1; // Boss 攻击动画在 Action Layer
    }

    /// <summary>Boss 不需要 PlayHitReaction（状态机自己管动画），但保留音效和特效。</summary>
    protected override void OnHitReaction(ICombatSystem attacker, string specialReaction)
    {
        // 音效 + 特效照常
        AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.8f);
        if (hitFxPrefab != null)
            Instantiate(hitFxPrefab, transform.position, Quaternion.identity);

        if (HealthSystem.IsDead)
            PlayDeathAnimation(attacker);
        // Boss 不播受击动画，Stunned/Die 由 BossController 状态机管理
    }
}
