using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFighterNew : MonoBehaviour, ICombatSystem
{
    private WeaponEquipmentManager weaponManager;
    private PlayerProperty playerProperty;

    // 核心战斗系统属性
    public HealthSystem HealthSystem { get; private set; }
    private HashSet<int> hitTargets = new HashSet<int>();
    
    // 状态标志 (PlayerAttack 会修改 InAction)
    public bool InAction { get; set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;

    // 特殊受击动画（Animation Event 设置，null/空 = 使用默认受击动画）
    public string CurrentSpecialHitReaction { get; set; }

    // 这些状态在新系统中可能不再严格需要，但为了接口保留
    public bool IsCounterable => false; 
    public AttackStates Attackstate { get; set; }
    public bool docombo { get; set; }
    public int comboCount { get; set; } = 0;

    // 目标管理
    public ICombatSystem currTarget { get; set; }

    // 攻击数据 (若动画机中不依赖这些数据，后续也可移除)
    [SerializeField] private List<AttackData> attacks;
    [SerializeField] private List<AttackData> longRangeAttacks;
    [SerializeField] private float longRangeAttackThreshold = 1.5f;
    
    public List<AttackData> Attacks => attacks;
    public List<AttackData> LongRangeAttacks => longRangeAttacks;
    public float LongRangeAttackThreshold => longRangeAttackThreshold;
    
    // 碰撞体引用
    public Animator animator { get; private set; }
    public BoxCollider WeaponCollider { get; private set; }
    public SphereCollider leftHandCollider { get; private set; }
    public SphereCollider rightHandCollider { get; private set; }
    public SphereCollider leftFootCollider { get; private set; }
    public SphereCollider rightFootCollider { get; private set; }
    
    // 事件
    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;

    private void Awake()
    {
        weaponManager = WeaponEquipmentManager.Instance;
        playerProperty = GetComponent<PlayerProperty>();
        HealthSystem = GetComponent<HealthSystem>();
        if (HealthSystem == null)
            HealthSystem = gameObject.AddComponent<HealthSystem>();

        animator = GetComponent<Animator>();
        InitializeBodyColliders();
    }

    private void InitializeBodyColliders()
    {
        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<SphereCollider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<SphereCollider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<SphereCollider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<SphereCollider>();

        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
        }
    }

    private void Update()
    {
        // 现在 Update 只负责索敌，输入和攻击执行全部交给了 PlayerAttack
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        currTarget = FindNearestTarget();
    }

    private ICombatSystem FindNearestTarget()
    {
        var enemies = FindObjectsOfType<EnemyFighter>();
        ICombatSystem nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            var fighter = enemy.GetComponent<ICombatSystem>();
            if (fighter != null && !fighter.HealthSystem.IsDead)
            {
                float distance = Vector3.Distance(transform.position, fighter.transform.position);
                if (distance < nearestDistance && distance < 8f) // 8米内索敌
                {
                    nearestDistance = distance;
                    nearest = fighter;
                }
            }
        }
        return nearest;
    }

    public void TakeDamage(float damage)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = GetPlayerArmor();
        HealthSystem.TakeDamage(damage, currentArmor);
        OnGotHit?.Invoke(this); 

        
        Debug.Log($"玩家受到伤害: {damage}, 当前护甲: {currentArmor}, 剩余血量: {HealthSystem.Health}");
    }


    public int GetPlayerArmor()
    {
        return playerProperty?.armorValue ?? 0;
    }

    public IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null)
    {
        InAction = true;
        IsTakingHit = true;

        string hitAnim = string.IsNullOrEmpty(specialHitReaction) ? "hit_light_B_body" : specialHitReaction;
        animator.CrossFade(hitAnim, 0.2f, 4);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(4);
        yield return new WaitForSeconds(animstate.length * 0.8f);

        OnHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }

    public void PlayDeathAnimation(ICombatSystem attacker)
    {
        animator.CrossFade("Death", 0.2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;
        
        if (other.CompareTag("Hitbox") && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();

            // 防止同一刀命中同一目标多次
            if (!attacker.RegisterHit(this.gameObject)) return;

            TakeDamage(attackerDamage);
            
            if (!HealthSystem.IsDead)
            {
                string specialReaction = attacker.CurrentSpecialHitReaction;
                attacker.CurrentSpecialHitReaction = null;
                StartCoroutine(PlayHitReaction(attacker, specialReaction));
            }
            else
            {
                PlayDeathAnimation(attacker);
            }
        }
    }

    public float GetWeaponDamage()
    {
        return WeaponEquipmentManager.Instance?.GetWeaponDamage() ?? 1f;
    }

    public bool HasUsableWeapon()
    {
        return WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null;
    }

    // ==========================================
    // Hitbox 管理 (建议在动画片段中通过 Animation Event 调用)
    // ==========================================

    // 保留原始基于 AttackData 的开启方式以满足接口
    public void EnableHitbox(AttackData attack)
    {
        EnableHitboxByType(attack.HitboxToUse);
    }

    // 新增：专门为了给 Animation Event 调用的重载，传入枚举对应的 int 或直接提供具体方法
    public void EnableHitboxByType(AttackHitbox hitboxType)
    {
        switch (hitboxType)
        {
            case AttackHitbox.LeftHand: if (leftHandCollider != null) leftHandCollider.enabled = true; break;
            case AttackHitbox.RightHand: if (rightHandCollider != null) rightHandCollider.enabled = true; break;
            case AttackHitbox.LeftFoot: if (leftFootCollider != null) leftFootCollider.enabled = true; break;
            case AttackHitbox.RightFoot: if (rightFootCollider != null) rightFootCollider.enabled = true; break;
            case AttackHitbox.Sword:
                var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
                if (weaponCollider != null) weaponCollider.enabled = true;
                break;
        }
    }
    public void AE_EnableHitbox(string hitboxName)
    {
        hitTargets.Clear(); //每次挥刀开始，清空上一刀的记录
        // 将动画传来的字符串转为枚举
        if (System.Enum.TryParse(hitboxName, out AttackHitbox type))
        {
            EnableHitboxByType(type); // 调用 Switch 逻辑
        }
    }

    // Animation Event 调用：设置本次攻击命中时的特殊受击动画
    // 在 AE_EnableHitbox 同一帧或之前调用即可
    // 传空字符串 = 清空，使用默认受击动画
    public void AE_SetHitReaction(string animName)
    {
        if (string.IsNullOrEmpty(animName))
        {
            CurrentSpecialHitReaction = null;
        }
        else
        {
            CurrentSpecialHitReaction = animName;
        }
    }

    // 建议在动画 Event 中调用此方法关闭碰撞
    public void DisableHitboxes()
    {
        if (leftHandCollider != null) leftHandCollider.enabled = false;
        if (rightHandCollider != null) rightHandCollider.enabled = false;
        if (leftFootCollider != null) leftFootCollider.enabled = false;
        if (rightFootCollider != null) rightFootCollider.enabled = false;

        var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
        if (weaponCollider != null) weaponCollider.enabled = false;

        // 清除特殊受击标记，防止挥空后残留到下一刀
        CurrentSpecialHitReaction = null;
    }
    public bool RegisterHit(GameObject target)
    {
        int id = target.GetInstanceID();
        if (hitTargets.Contains(id)) 
        {
            return false; // 打过了，拒绝再次生效
        }
        
        hitTargets.Add(id); // 没打过，记录下来
        return true; // 允许生效
    }

    #region ICombatSystem 遗留接口实现 (已废弃/转移，仅为防止报错空实现)
    
    public bool CanAttack() => !InAction && HasUsableWeapon();
    public void TryToAttack(ICombatSystem target = null) { /* 转移至 PlayerAttack */ }
    public void UpdateAttackState(float normalizedTime, AttackData attack) { /* 转移至 PlayerAttack/动画机 */ }
    public void ResetAttackState() { }
    public AttackData SelectAttack(ICombatSystem target, int comboCount) => null;
    public Vector3 CalculateAttackDirection(ICombatSystem target) => transform.forward;
    public Vector3 CalculateAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos) => Vector3.zero;
    public void PrepareAttack(ICombatSystem target) { }
    public void FinishAttack() { }
    public bool CheckComboCondition() => false;
    public IEnumerator ExecuteAttack(ICombatSystem target, int comboCount) { yield break; }
    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;

    #endregion
}