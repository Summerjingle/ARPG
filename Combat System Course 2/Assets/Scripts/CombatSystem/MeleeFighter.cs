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
   



    private PlayerProperty playerProperty;
    public bool isPlayer;



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
        playerProperty = GetComponent<PlayerProperty>();
        isPlayer = playerProperty != null;
        healthSystem.OnDeath += HandleDeath;
        healthSystem.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        // 初始化身体部位的碰撞器（这些与武器无关，应该始终初始化）
        InitializeBodyColliders();

        // 禁用所有碰撞器
        DisableAllHitboxes();
        if (!isPlayer)
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
    public void TryToAttack(MeleeFighter target = null)//尝试进行攻击，此方法是 被调用的
    {
        // 阶段2：优先使用新的专用组件
        var playerFighter = GetComponent<PlayerFighter>();
        var enemyFighter = GetComponent<EnemyFighter>();

        if (playerFighter != null)
        {
            playerFighter.PlayerTryToAttack(target);
            return;
        }
        else if (enemyFighter != null)
        {
            enemyFighter.EnemyTryToAttack(target);
            return;
        }

        // 回退到原有逻辑（保持兼容）
        if (!InAction && HasUsableWeapon())//
        {
            StartCoroutine(Attack(target));//调用攻击，进入攻击状态
        }
        else if (Attackstate == AttackStates.Impact || Attackstate == AttackStates.Cooldown)//如果已经在攻击
        {
            docombo = true;//进入连击
        }
    }

    private void HandleDeath(HealthSystem hs)
    {
        // 同步 PlayerProperty
        if (isPlayer && playerProperty != null)
        {
            playerProperty.hpValue = 0;
        }

        // 处理敌人死亡逻辑
        if (!isPlayer)
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
    private void HandleHealthChanged(HealthSystem hs)
    {
        if (isPlayer && playerProperty != null)
        {
            playerProperty.hpValue = Mathf.RoundToInt(hs.Health);
        }
    }
    public MeleeFighter currTarget;
    public IEnumerator Attack(MeleeFighter target = null)
    {
        // 阶段2：使用专用组件的方法

        // 1. 攻击准备阶段
        var playerFighter = GetComponent<PlayerFighter>();
        var enemyFighter = GetComponent<EnemyFighter>();

        if (playerFighter != null)
        {
            playerFighter.PreparePlayerAttack(target);
        }
        else if (enemyFighter != null)
        {
            enemyFighter.PrepareEnemyAttack(target);
        }

        InAction = true;
        currTarget = target;
        Attackstate = AttackStates.Windup;

        // 2. 攻击数据选择 - 使用专用方法
        var attack = attacks[comboCount];
        Vector3 attackDir = transform.forward;
        Vector3 startPos = transform.position;
        Vector3 targetPos = Vector3.zero;

        // 使用专用组件计算攻击数据
        if (playerFighter != null)
        {
            attack = playerFighter.SelectPlayerAttack(target, attacks, longRangeAttacks, comboCount, LongRangeAttackThreshold);
            attackDir = playerFighter.CalculatePlayerAttackDirection(target);

            if (target != null && attack.MoveToTarget)
            {
                targetPos = playerFighter.CalculatePlayerAttackPosition(target, attack, attackDir, startPos);
            }
        }
        else if (enemyFighter != null)
        {
            attack = enemyFighter.SelectEnemyAttack(target, attacks, longRangeAttacks, comboCount);
            attackDir = enemyFighter.CalculateEnemyAttackDirection(target);
            targetPos = enemyFighter.CalculateEnemyAttackPosition(target, attack, attackDir, startPos);
        }
        else
        {
            // 回退到原有逻辑（保持兼容）
            if (target != null)
            {
                var vecToTarget = target.transform.position - transform.position;
                vecToTarget.y = 0;
                attackDir = vecToTarget.normalized;
                float distance = vecToTarget.magnitude;
                if (distance > LongRangeAttackThreshold && longRangeAttacks.Count > 0)
                {
                    attack = longRangeAttacks[0];
                }
                if (attack.MoveToTarget)
                {
                    if (distance < attack.MaxMoveDistance)
                        targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;
                    else
                        targetPos = startPos + attackDir * attack.MaxMoveDistance;
                }
            }
        }

        // 3. 动画播放（保持不变）
        animator.CrossFade(attack.AttackName, 0.2f);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);

        // 4. 攻击执行循环 - 包含状态管理分离
        float timer = 0f;
        while (timer <= animstate.length)
        {
            if (IsTakingHit) break;
            timer += Time.deltaTime;
            float normalizedTime = timer / animstate.length;

            // 移动逻辑（玩家需要，敌人通常不需要）
            if (target != null && attack.MoveToTarget)
            {
                if (playerFighter != null) // 只有玩家需要手动移动
                {
                    float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);
                    Vector3 desiredPosition = Vector3.Lerp(startPos, targetPos, percTime);
                    Vector3 moveDelta = desiredPosition - transform.position;

                    CharacterController controller = GetComponent<CharacterController>();
                    if (controller != null)
                    {
                        controller.Move(moveDelta);
                    }
                    else
                    {
                        transform.position = desiredPosition;
                    }
                }
            }

            // 转向控制（保持不变）
            if (attackDir != null)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(attackDir), 500f * Time.deltaTime);
            }

            // =============================================
            // 阶段2：攻击状态管理分离 - 关键修改部分
            // =============================================
            if (playerFighter != null)
            {
                // 使用玩家专属状态管理
                playerFighter.UpdatePlayerAttackState(normalizedTime, attack);

                // 玩家连击检查
                if (playerFighter.CheckPlayerComboCondition())
                {
                    docombo = false;
                    comboCount = (comboCount + 1) % attacks.Count;
                    StartCoroutine(Attack(target));
                    yield break;
                }
            }
            else if (enemyFighter != null)
            {
                // 使用敌人专属状态管理
                enemyFighter.UpdateEnemyAttackState(normalizedTime, attack);

                // 敌人连击由AttackState控制，这里不处理
                // 原有的连击逻辑被移除，因为敌人连击在AttackState中处理
            }
            else
            {
                // 回退到原有状态管理逻辑（保持兼容）
                if (Attackstate == AttackStates.Windup)
                {
                    if (InCounter) break;
                    if (normalizedTime >= attack.ImpactStartTime)
                    {
                        Attackstate = AttackStates.Impact;
                        EnableHitbox(attack);
                    }
                }
                else if (Attackstate == AttackStates.Impact)
                {
                    if (normalizedTime >= attack.ImpactEndTime)
                    {
                        Attackstate = AttackStates.Cooldown;
                        DisableAllHitboxes();
                    }
                }
                else if (Attackstate == AttackStates.Cooldown)
                {
                    if (docombo)
                    {
                        docombo = false;
                        comboCount = (comboCount + 1) % attacks.Count;
                        StartCoroutine(Attack(target));
                        yield break;
                    }
                }
            }
            // =============================================
            // 状态管理分离结束
            // =============================================

            yield return null;
        }

        // 5. 攻击结束处理 - 使用专用状态重置
        if (playerFighter != null)
        {
            playerFighter.ResetPlayerAttackState();
        }
        else if (enemyFighter != null)
        {
            enemyFighter.ResetEnemyAttackState();
        }
        else
        {
            // 回退到原有状态重置逻辑
            Attackstate = AttackStates.Idle;
            comboCount = 0;
            InAction = false;
        }

        currTarget = null;

        // 攻击结束后的清理
        if (playerFighter != null)
        {
            playerFighter.FinishPlayerAttack();
        }
        else if (enemyFighter != null)
        {
            enemyFighter.FinishEnemyAttack();
        }
    }

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
        // 阶段2：尝试使用新的专用组件
        var playerFighter = GetComponent<PlayerFighter>();
        var enemyFighter = GetComponent<EnemyFighter>();

        if (playerFighter != null)
            return playerFighter.GetWeaponDamage();
        else if (enemyFighter != null)
            return enemyFighter.GetWeaponDamage();

        // 回退到原有逻辑（保持兼容）
        if (isPlayer)
        {
            return WeaponEquipmentManager.Instance?.GetWeaponDamage() ?? 1f;
        }
        else
        {
            return enemyWeapon?.GetDamage() ?? 1f;
        }
    }
    public void TakeDamage(float damage)
    {
        if (healthSystem.IsDead) return;

        int currentArmor = 0;
        if (isPlayer && playerProperty != null)
        {
            currentArmor = playerProperty.armorValue;
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
        // 阶段2：优先使用新的专用组件
        var playerFighter = GetComponent<PlayerFighter>();
        var enemyFighter = GetComponent<EnemyFighter>();

        if (playerFighter != null)
            return playerFighter.PlayerHasUsableWeapon();
        else if (enemyFighter != null)
            return enemyFighter.EnemyHasUsableWeapon();

        // 回退到原有逻辑（保持兼容）
        if (isPlayer)
        {
            return WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null;
        }
        else
        {
            // 敌人检查自己的武器
            var weapon = GetComponentInChildren<Weapon>();
            if (weapon != null)
            {
                Debug.Log($"敌人武器: {weapon.name}, 伤害: {weapon.GetDamage()}");
                return true;
            }

            // 或者检查WolfWeapon
            var wolfWeapon = GetComponentInChildren<Weapon>();
            if (wolfWeapon != null)
            {
                Debug.Log($"狼武器伤害: {wolfWeapon.GetDamage()}");
                return true;
            }

            Debug.LogWarning("敌人没有找到可用武器！");
            return false;
        }
    }
    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandleDeath;
            healthSystem.OnHealthChanged -= HandleHealthChanged;
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