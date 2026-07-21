using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    private Rigidbody rb;
    public HealthSystem HealthSystem { get; private set; }
    public Animator animator { get; private set; }
    [SerializeField] private float knockbackForce = 10f; // 击退力度
    [SerializeField] private bool isTestDummy = false;   // 测试假人：不死，无限处决+VFX测试

    public event System.Action<EnemyTest> OnGotHit;

    // UI 指示器
    [Header("UI")]
    [SerializeField] private GameObject executableImage;

    // 处决支持
    public bool CanBeExecuted => true;
    public bool InAction { get; set; }

    void Start()
    {

        rb = GetComponent<Rigidbody>();
        HealthSystem=GetComponent<HealthSystem>();
        animator = GetComponent<Animator>();

        // 建议：增加 Rigidbody 的 Drag（空气阻力），防止敌人被打飞后滑行停不下来
        if (rb != null) rb.drag = 5f;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hitbox"))
        {
            // 获取攻击者的战斗系统组件
            var attacker = other.GetComponentInParent<PlayerFighter>();
            
            if (attacker != null)
            {   
                Animator attackerAnimator = (attacker as MonoBehaviour)?.GetComponent<Animator>();
                Animator selfAnimator = GetComponent<Animator>();
               var attackerDamage = attacker.GetWeaponDamage();

                // 询问玩家：我在这轮攻击的“黑名单”里吗？
                if (attacker.RegisterHit(this.gameObject))
                {
                    // 只有 RegisterHit 返回 true，才执行伤害逻辑
                    Debug.Log($"【命中生效】被 {other.name} 击中！");
                    attacker.NotifyDamageDealt(this.gameObject);
                    if (attackerAnimator != null)
                        HitDelay.Instance.Stop(0.07f, attackerAnimator);
                    if (selfAnimator != null)
                        selfAnimator.SetTrigger("Damage");
                    if (selfAnimator != null)
                        HitDelay.Instance.Stop(0.04f, selfAnimator);
                    ApplyPhysicsKnockback(attacker.transform.position);
                    bool isCrit = Random.value < (attacker.CritRate / 100f);
                    float finalDamage=isCrit?attackerDamage*attacker.CritDamage:attackerDamage;
                    TakeDamage(finalDamage, isCrit);

                }
                else
                {
                    // 如果已经打过了，这一帧就什么都不做
                    //Debug.Log("这一刀已经打过我了，跳过重复伤害");
                }
            }
            
        }
        
        
    }
     public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = 0;

        HealthSystem.TakeDamage(damage, currentArmor, isCrit);
        OnGotHit?.Invoke(this);

        // 测试假人：扣到0血时回满
        if (isTestDummy && HealthSystem.Health <= 0)
            HealthSystem.Health = HealthSystem.MaxHealth;

        Debug.Log($"敌人({gameObject.name})受到伤害: {damage}, 护甲减免: {currentArmor}, 剩余生命: {HealthSystem.Health}");
    }

    /// <summary>处决：播放被处决动画（layer 1）</summary>
    public void PlayExecutionReaction()
    {
        InAction = true;
        var col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.isKinematic = true;  // 处决期间不让物理干扰动画

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.CrossFade("Executed", 0.1f, 1);
        }
    }

    /// <summary>处决结束清理</summary>
    public void OnExecutionEnd()
    {
        if (HealthSystem != null && HealthSystem.IsDead) return;
        var col = GetComponent<CapsuleCollider>();
        if (col != null) col.enabled = true;
        if (rb != null) rb.isKinematic = false;
        if (animator != null)
            animator.applyRootMotion = false;
        InAction = false;
    }

    /// <summary>显示/隐藏处决提示图标</summary>
    public void SetExecutableIndicator(bool visible)
    {
        if (executableImage != null && executableImage.activeSelf != visible)
            executableImage.SetActive(visible);
    }
    private void ApplyPhysicsKnockback(Vector3 attackerPos)
    {
        if (rb == null) return;

        // 计算击退方向：从攻击者指向敌人[cite: 3]
        Vector3 direction = (transform.position - attackerPos).normalized;
        direction.y = 0; // 锁定水平位移，防止敌人斜着飞出去

        // 使用 Impulse 模式产生瞬间爆发力
        // 这种模式会自动忽略物体的质量（Mass）差异，让打击感更统一
        rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
    }
}
