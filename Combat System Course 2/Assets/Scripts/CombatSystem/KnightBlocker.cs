using UnityEngine;
using System.Collections;

public class KnightBlocker : EnemyFighter
{
    [Header("骑士完美格挡 - 核弹版（100%生效）")]
    [Range(0f, 1f)] public float blockChance = 1f;
    public string blockAnimationName = "Block";   // 随便填，下面会强制播放
    [SerializeField] private AudioClip blockSound; // 格挡音效（可选）

    private new Animator animator;
    private bool isBlocking = false;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    // 核心：完全接管 OnTriggerEnter，格挡成功就直接 return！
    private new void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;

        if (other.CompareTag("Hitbox"))
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget?.gameObject != this.gameObject) return;

            // 只有玩家攻击才可能格挡
            if (attacker.gameObject.CompareTag("Player") && Random.value < blockChance && !isBlocking)
            {
                // 完美格挡！什么都不执行，直接吞掉！
                isBlocking = true;

                // 强制播放格挡动画
                animator.CrossFade(blockAnimationName, 0.05f, 1);

                // 格挡音效
                if (blockSound) AudioSource.PlayClipAtPoint(blockSound, transform.position);

                // 玩家被弹开（最稳的假倒放）
                var playerAnim = attacker.gameObject.GetComponent<Animator>();
                if (playerAnim) StartCoroutine(RealParryEffect(playerAnim));

                Debug.Log("骑士完美格挡！攻击被完全吞掉！");

                Invoke(nameof(ResetBlock), 0.8f);
                return; // 关键！直接 return，后面的所有扣血、音效、特效、状态切换全都不执行！
            }

            // 没格挡到，才走原逻辑
            base.OnTriggerEnter(other);
        }
    }

    private void ResetBlock()
    {
        isBlocking = false;
    }

    // 2025年最稳的“被弹开”效果
    private IEnumerator RealParryEffect(Animator playerAnim)
    {
        playerAnim.speed = 0f;
        yield return new WaitForSeconds(0.1f);

        AnimatorStateInfo state = playerAnim.GetCurrentAnimatorStateInfo(0);
        playerAnim.Play(state.shortNameHash, 0, Mathf.Max(0f, state.normalizedTime - 0.35f));
        playerAnim.speed = 8f;
        yield return new WaitForSeconds(0.1f);
        playerAnim.speed = 1f;
    }
}