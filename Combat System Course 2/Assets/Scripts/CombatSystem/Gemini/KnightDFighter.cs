using UnityEngine;
using System.Collections;

public class KnightDFighter : EnemyFighter
{
    [Header("骑士特殊参数")]
    [SerializeField] private float shieldBlockChance = 0.25f; // 25% 几率格挡

    private bool pendingBlock = false;

    // --- 受击时格挡逻辑 ---
    public override void TakeDamage(float damage)
    {
        // 如果之前已经判定要格挡，则不受伤害
        if (pendingBlock)
        {
            PlayInstantBlockEffect();
            pendingBlock = false;
            return;
        }

        // 随机几率格挡
        if (Random.value < shieldBlockChance && !HealthSystem.IsDead && !IsTakingHit)
        {
            PlayBlockEffect();
            return; // 完全格挡，无伤害
        }

        base.TakeDamage(damage);
    }

    // 普通格挡效果
    private void PlayBlockEffect()
    {
        animator.CrossFade("Block", 0.1f, 1);
    }

    // 即时格挡效果
    private void PlayInstantBlockEffect()
    {
        animator.CrossFade("Block", 0.05f, 1);
    }

    // ==================【读指令格挡机制】=====================

    // 玩家攻击开始时调用：决定是否要格挡
    public void TryPredictiveBlock()
    {
        if (!HealthSystem.IsDead && !IsTakingHit && Random.value < shieldBlockChance)
        {
            pendingBlock = true;
            animator.CrossFade("Block", 0.05f, 1);
        }
    }

    // 触发读指令格挡效果
    public void PerformPredictiveBlock(Animator playerAnimator)
    {
        pendingBlock = false;

        // 玩家攻击停顿（hitstop）
        if (playerAnimator != null)
        {
            playerAnimator.speed = 0f;
            StartCoroutine(ReversePlayerAttack(playerAnimator));
        }
    }

    private IEnumerator ReversePlayerAttack(Animator player)
    {
        yield return new WaitForSeconds(0.1f);
        player.speed = -1f;
        yield return new WaitForSeconds(0.25f);
        player.speed = 1f;
    }
}