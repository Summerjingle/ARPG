using UnityEngine;
using System.Collections;

public class KnightDFighter : EnemyFighter
{
    [Header("��ʿ�������")]
    [SerializeField] private float shieldBlockChance = 1f; // % ���ʸ�

    private bool pendingBlock = false;

    // --- �ܻ�ʱ���߼� ---
    public override void TakeDamage(float damage, bool isCrit = false)
    {
        //���֮ǰ�Ѿ��ж�Ҫ�񵲣������˺�
        if (pendingBlock)
        {
            PlayInstantBlockEffect();
            pendingBlock = false;
            return;
        }

        // ������ʸ�
        if (Random.value < shieldBlockChance && !HealthSystem.IsDead && !IsTakingHit)
        {
            PlayBlockEffect();
            return; // ��ȫ�񵲣����˺�
        }

        base.TakeDamage(damage, isCrit);
    }

    // ��ͨ��Ч��
    private void PlayBlockEffect()
    {
        animator.CrossFade("Block", 0.1f, 1);
    }

    // ��ʱ��Ч��
    private void PlayInstantBlockEffect()
    {
        animator.CrossFade("Block", 0.05f, 1);
    }

    // ==================����ָ��񵲻��ơ�=====================

    // ��ҹ�����ʼʱ���ã������Ƿ�Ҫ��
    public void TryPredictiveBlock()
    {
        if (!HealthSystem.IsDead && !IsTakingHit && Random.value < shieldBlockChance)
        {
            pendingBlock = true;
            animator.CrossFade("Block", 0.05f, 1);
        }
    }

    // ������ָ���Ч��
    public void PerformPredictiveBlock(Animator playerAnimator)
    {
        pendingBlock = false;

        // ��ҹ���ͣ�٣�hitstop��
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