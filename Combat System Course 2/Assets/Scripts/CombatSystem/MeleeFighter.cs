using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum AttackStates { Idle, Windup, Impact, Cooldown }//枚举武器的状态
public class MeleeFighter : MonoBehaviour
{
    [SerializeField] public List<AttackData> attacks;
    [SerializeField] public List<AttackData> longRangeAttacks;
    [SerializeField] public float LongRangeAttackThreshold = 1.5f;
    [SerializeField] private HealthSystem healthSystem;
    public HealthSystem HealthSystem => healthSystem;
    public BoxCollider WeaponCollider;
    public SphereCollider leftHandCollider, rightHandCollider, leftFootCollider, rightFootCollider;
    public Animator animator;
    private Weapon enemyWeapon;




    public AttackStates Attackstate { get; set; }
    public event Action<MeleeFighter> OnGotHit;//收到打击事件
    public event Action OnHitComplete;//收到打击完成事件
   
    public bool InAction { get; set; } = false;
   
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    public bool docombo;

    public int comboCount = 0;

    private ICombatSystem GetCombatSystem()
    {
        // 直接获取接口，不再分别检查具体组件
        var combatSystem = GetComponent<ICombatSystem>();

        if (combatSystem == null)
        {
            Debug.LogError($"[战斗系统] {gameObject.name} 缺少 ICombatSystem 实现！");
            Debug.LogError("请添加 PlayerFighter 或 EnemyFighter 组件");
        }

        return combatSystem;
    }
    public void Awake()
    {
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();
        
        healthSystem.OnDeath += HandleDeath;
        
    }

    private void Start()
    {
        // 初始化身体部位的碰撞器（这些与武器无关，应该始终初始化）
        InitializeBodyColliders();

        // 禁用所有碰撞器
        DisableAllHitboxes();
        var enemyFighter = GetComponent<EnemyFighter>();
        if (enemyFighter != null)
        {
            enemyWeapon = GetComponentInChildren<Weapon>();
            Debug.Log($"敌人武器初始化: {enemyWeapon?.name ?? "未找到"}, 伤害: {enemyWeapon?.GetDamage() ?? 0}");
        }

    }


    // 单独的方法来初始化身体碰撞器
    private void InitializeBodyColliders()
    {

        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<SphereCollider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<SphereCollider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<SphereCollider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<SphereCollider>();

        // 添加空检查日志
        if (leftHandCollider == null) Debug.LogWarning("左手碰撞器未找到");
        if (leftFootCollider == null) Debug.LogWarning("左脚碰撞器未找到");
        if (rightHandCollider == null) Debug.LogWarning("右手碰撞器未找到");
        if (rightFootCollider == null) Debug.LogWarning("右脚碰撞器未找到");

        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
            Debug.Log($"初始化武器碰撞器: {(WeaponCollider != null ? WeaponCollider.name : "null")}");
        }
    }
    public void TryToAttack(MeleeFighter target = null)
    {
        var combatSystem = GetComponent<ICombatSystem>();
        if (combatSystem != null)
        {
            combatSystem.TryToAttack(target);
            return;
        }

        Debug.LogError($"[战斗系统] {gameObject.name} 缺少 ICombatSystem 实现组件！");
        Debug.LogError("请添加 PlayerFighter 或 EnemyFighter 组件");
    }

    private void HandleDeath(HealthSystem hs)
    {

        var enemyFighter = GetComponent<EnemyFighter>();
        if (enemyFighter != null)
        {
            var wolfController = GetComponent<WolfController>();
            if (wolfController != null)
            {
                wolfController.HandleWolfDeath();
            }
            else
            {
                var enemyController = GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.ChangerState(EnemyStates.Dead);
                }
            }
        }
    }
  
    public MeleeFighter currTarget;
    
    private void OnTriggerEnter(Collider other)
    {
        if (healthSystem.IsDead) return;

        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<MeleeFighter>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker?.GetWeaponDamage() ?? 5f;
            TakeDamage(attackerDamage);

            if (!healthSystem.IsDead)
            {
                StartCoroutine(PlayHitReaction(attacker));
            }
            else
            {
                PlayDeathAnimation(attacker);
            }
        }
    }
    public float GetWeaponDamage()
    {
        var combatSystem = GetComponent<ICombatSystem>();
        if (combatSystem != null)
        {
            return combatSystem.GetWeaponDamage();
        }

        Debug.LogError($"[战斗系统] {gameObject.name} 无法获取武器伤害：缺少战斗组件");
        return 0f;
    }
    public void TakeDamage(float damage)
    {
        if (healthSystem.IsDead) return;

        int currentArmor = 0;
        // 通过接口获取护甲值
        var playerFighter = GetComponent<PlayerFighter>();
        if (playerFighter != null)
        {
            // 玩家护甲逻辑可以移到PlayerFighter中
            var playerProperty = GetComponent<PlayerProperty>();
            if (playerProperty != null)
            {
                currentArmor = playerProperty.armorValue;
            }
        }

        healthSystem.TakeDamage(damage, currentArmor);
        OnGotHit?.Invoke(this);
    }
    
    public void RestoreHealth(int amount)
    {
        healthSystem.RestoreHealth(amount);
    }


    public void PlayDeathAnimation(MeleeFighter attacker)
    {
        animator.CrossFade("Death", 0.2f);
        
    }
    public IEnumerator PlayHitReaction(MeleeFighter attacker)
    {
        InAction = true;
        IsTakingHit = true;

        var dispVec = attacker.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);

        // 检查角色类型
        bool isHumanoid = GetComponent<MeleeFighter>() != null && GetComponent<WolfController>() == null;

        if (isHumanoid)
        {
            // 人形敌人：使用图层1
            animator.CrossFade("SwordImpact", 0.2f, 1);
            yield return null;
            var animstate = animator.GetNextAnimatorStateInfo(1);
            yield return new WaitForSeconds(animstate.length * 0.8f);
        }
        else
        {
            // 狼或其他非人形：使用图层0或固定时间
            animator.CrossFade("SwordImpact", 0.2f, 0);
            yield return null;
            var animstate = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(animstate.length * 0.8f);
        }

        OnHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }


    public IEnumerator PerfromCounterAttack(EnemyController opponent)
    {
        // 检查对手是否是狼，如果是狼则不执行处决动画
        if (opponent.GetComponent<WolfController>() != null)
        {
            Debug.LogWarning("Counterattack 对狼无效，改为普通攻击");
            // 对狼执行普通攻击
            TryToAttack(opponent.Fighter);
            yield break; // 直接返回，不执行处决流程
        }

        InAction = true;
        InCounter = true;
        opponent.healthBar.healthBarBG.enabled = false;
        opponent.healthBar.healthBarFill.enabled = false;
        opponent.healthBar.myName.enabled = false;
        opponent.Fighter.InCounter = true;
        opponent.ChangerState(EnemyStates.Dead);

        var dispVec = opponent.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);
        opponent.transform.rotation = Quaternion.LookRotation(-dispVec);

        var targetPos = opponent.transform.position - dispVec.normalized * 1f;

        animator.CrossFade("Counterattack", 0.2f);
        opponent.Animator.CrossFade("CounterattackVictim", 0.2f);

        yield return null;//等待一帧

        var animstate = animator.GetNextAnimatorStateInfo(1);

        float timer = 0f;
        while (timer <= animstate.length)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 5 * Time.deltaTime);
            yield return null;
            timer += Time.deltaTime;
        }

        InCounter = false;
        opponent.Fighter.InCounter = false;
        InAction = false;
    }
    public void EnableHitbox(AttackData attack)
    {
        // 添加空检查，防止访问已销毁的碰撞器
        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (leftHandCollider != null) leftHandCollider.enabled = true;
                break;
            case AttackHitbox.RightHand:
                if (rightHandCollider != null) rightHandCollider.enabled = true;
                break;
            case AttackHitbox.LeftFoot:
                if (leftFootCollider != null) leftFootCollider.enabled = true;
                break;
            case AttackHitbox.RightFoot:
                if (rightFootCollider != null) rightFootCollider.enabled = true;
                break;
            case AttackHitbox.Sword:
                var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
                if (weaponCollider != null) weaponCollider.enabled = true;
                else Debug.LogWarning("武器碰撞器为null，无法启用");
                break;
            default:
                break;
        }
    }
    public bool HasUsableWeapon()
    {
        var combatSystem = GetComponent<ICombatSystem>();
        if (combatSystem != null)
        {
            return combatSystem.HasUsableWeapon();
        }

        Debug.LogError($"[战斗系统] {gameObject.name} 武器检查失败：缺少战斗组件");
        return false;
    }
    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandleDeath;
        }
    }
    public void DisableAllHitboxes()//游戏开始时默认禁用所有碰撞器
    {
        if (WeaponCollider != null)
            WeaponCollider.enabled = false;
        if (leftHandCollider != null)
            leftHandCollider.enabled = false;
        if (rightHandCollider != null)
            rightHandCollider.enabled = false;
        if (leftFootCollider != null)
            leftFootCollider.enabled = false;
        if (rightFootCollider != null)
            rightFootCollider.enabled = false;
    }


    public List<AttackData> Attacks => attacks;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;
   
}