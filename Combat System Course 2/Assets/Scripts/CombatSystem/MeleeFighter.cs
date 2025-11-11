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
    private ICombatSystem combatSystem;
    private Weapon enemyWeapon;




    public AttackStates Attackstate { get; set; }
    public event Action<ICombatSystem> OnGotHit;//收到打击事件
    public event Action OnHitComplete;//收到打击完成事件
   
    public bool InAction { get; set; } = false;
   
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    public bool docombo;

    public int comboCount = 0;

    
    public void Awake()
    {
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        if (healthSystem == null)
            healthSystem = gameObject.AddComponent<HealthSystem>();
        combatSystem = GetComponent<ICombatSystem>();
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