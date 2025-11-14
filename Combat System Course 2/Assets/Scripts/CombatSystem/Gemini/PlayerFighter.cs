using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFighter : MonoBehaviour, ICombatSystem
{
    private WeaponEquipmentManager weaponManager;
    private PlayerProperty playerProperty;

    private bool attackInput;
    private float lastAttackTime;
    private const float INPUT_BUFFER_TIME = 0.2f;
    private ICombatSystem currentTarget;

     // 健康系统
    public HealthSystem HealthSystem { get; private set; }
    
    // 战斗状态
    public bool InAction { get; set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;
    
    // 攻击状态
    public AttackStates Attackstate { get; set; }
    public bool docombo { get; set; }
    public int comboCount { get; set; } = 0;

    // 目标管理
    public ICombatSystem currTarget { get; set; }

    // 攻击数据
    [SerializeField] private List<AttackData> attacks;
    [SerializeField] private List<AttackData> longRangeAttacks;
    [SerializeField] private float longRangeAttackThreshold = 1.5f;
    
    public List<AttackData> Attacks => attacks;
    public List<AttackData> LongRangeAttacks => longRangeAttacks;
    public float LongRangeAttackThreshold => longRangeAttackThreshold;
    
    // 组件引用
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
        // 从 MeleeFighter 迁移过来的碰撞器初始化逻辑
        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<SphereCollider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<SphereCollider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<SphereCollider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<SphereCollider>();

        // 玩家使用装备管理器的武器
        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
        }
    }// 初始化碰撞器
    private void Update()
    {
        HandlePlayerInput();
        UpdateTarget();
    }

    private void HandlePlayerInput()
    {
        // 检测攻击输入
        if (Input.GetMouseButtonDown(1) && !IsUIActive())
        {
            attackInput = true;
            lastAttackTime = Time.time;
            Debug.Log("玩家攻击输入检测到");
        }

        // 处理缓冲输入
        if (attackInput && Time.time - lastAttackTime <= INPUT_BUFFER_TIME)
        {
            if (PlayerCanAttack())
            {
                TryToAttack(currentTarget);
                attackInput = false;
            }
        }
        else if (attackInput)
        {
            // 输入超时
            attackInput = false;
        }
    }

    private void UpdateTarget()
    {
        // 自动寻找最近的目标
        currentTarget = FindNearestTarget();
    }

    // 新增：UI状态检查
    private bool IsUIActive()
    {
        return UIStateManager.IsAnyUIActive;
    }
    public void TakeDamage(float damage)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = GetPlayerArmor();
        HealthSystem.TakeDamage(damage, currentArmor);
        OnGotHit?.Invoke(this);  // this 就是 ICombatSystem

        // 同步玩家属性中的生命值
        SyncPlayerHealth(HealthSystem.Health);

        Debug.Log($"玩家受到伤害: {damage}, 护甲减免: {currentArmor}, 剩余生命: {HealthSystem.Health}");
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
                if (distance < nearestDistance && distance < 8f) // 8米内
                {
                    nearestDistance = distance;
                    nearest = fighter;
                }
            }
        }

        return nearest;
    }
    public void SyncPlayerHealth(float health)
    {
        if (playerProperty != null)
        {
            playerProperty.hpValue = Mathf.RoundToInt(health);
        }
    }
    public int GetPlayerArmor()
    {
        return playerProperty?.armorValue ?? 0;
    }
    public ICombatSystem GetCurrentTarget()
    {
        return currentTarget;
    }

    public IEnumerator PlayHitReaction(ICombatSystem attacker)
    {
        InAction = true;
        IsTakingHit = true;

        var dispVec = attacker.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);

        // 玩家使用图层1
        animator.CrossFade("SwordImpact", 0.2f, 1);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);
        yield return new WaitForSeconds(animstate.length * 0.8f);

        OnHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }//开始受伤反应
    public void PlayDeathAnimation(ICombatSystem attacker)
    {
        animator.CrossFade("Death", 0.2f);
    }//播放死亡动画
    private void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;

        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();
            TakeDamage(attackerDamage);

            if (!HealthSystem.IsDead)
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
        return WeaponEquipmentManager.Instance?.GetWeaponDamage() ?? 1f;
    }

    public bool PlayerHasUsableWeapon()
    {
        return WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null;
    }


    public bool PlayerCanAttack()
    {
        return !InAction && PlayerHasUsableWeapon();
    }


    public void PlayerTryToAttack(ICombatSystem target = null)
    {
        

        if (PlayerCanAttack())
        {
            StartCoroutine(ExecutePlayerAttack(target, comboCount));
        }
        else if (Attackstate == AttackStates.Impact ||
                 Attackstate == AttackStates.Cooldown)
        {
            docombo = true;
        }
    }


    public Vector3 CalculatePlayerAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
    {
        // 玩家需要手动计算移动位置
        Vector3 targetPos = Vector3.zero;

        if (target != null && attack.MoveToTarget)
        {
            float distance = Vector3.Distance(target.transform.position, startPos);
            if (distance < attack.MaxMoveDistance)
                targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;
            else
                targetPos = startPos + attackDir * attack.MaxMoveDistance;
        }

        return targetPos;
    }


    public Vector3 CalculatePlayerAttackDirection(ICombatSystem target)
    {
        if (target != null)
        {
            var vecToTarget = target.transform.position - transform.position;
            vecToTarget.y = 0;
            return vecToTarget.normalized;
        }
        return transform.forward;
    }

    // 玩家攻击数据选择 - 考虑距离和连击
    public AttackData SelectPlayerAttack(ICombatSystem target, List<AttackData> attacks, List<AttackData> longRangeAttacks, int comboCount, float longRangeThreshold)
    {
        
        var attack = attacks[comboCount % attacks.Count];

        // 玩家需要根据距离选择近战或远程攻击
        if (target != null)
        {
            float distance = Vector3.Distance(target.transform.position, transform.position);

            // 如果距离超过阈值且有远程攻击可用，选择远程攻击
            if (distance > longRangeThreshold && longRangeAttacks.Count > 0)
            {
                attack = longRangeAttacks[0];
                Debug.Log($"玩家选择远程攻击: {attack.AttackName}, 距离: {distance}");
            }
            else
            {
                Debug.Log($"玩家选择近战攻击: {attack.AttackName}, 连击数: {comboCount}");
            }
        }

        return attack;
    }

    // 玩家攻击准备逻辑
    public void PreparePlayerAttack(ICombatSystem target)
    {
        

        // 玩家可能需要重置某些状态
        docombo = false;

        // 设置攻击目标
        currTarget = target;

        Debug.Log($"玩家准备攻击: {(target != null ? target.gameObject.name : "无目标")}");
    }


    // 玩家攻击结束逻辑
    public void FinishPlayerAttack()
    {
        // 玩家可能需要在攻击后重置输入状态等
        Debug.Log("玩家攻击结束");
    }


    // 玩家专属状态管理
    public void UpdatePlayerAttackState(float normalizedTime, AttackData attack)
    {
        
        if (Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                Attackstate = AttackStates.Impact;
                Debug.Log("玩家攻击进入Impact状态");

                EnablePlayerHitbox(attack);
            }
        }
        else if (Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                Attackstate = AttackStates.Cooldown;
                Debug.Log("玩家攻击进入Cooldown状态");
                DisablePlayerHitboxes();
            }
        }
    }


    // 玩家状态重置
    public void ResetPlayerAttackState()
    {
        Attackstate = AttackStates.Idle;
        InAction = false;
        comboCount = 0;
        docombo = false;
        Debug.Log("玩家攻击状态重置");
    }


    // 玩家连击状态检查
    public bool CheckPlayerComboCondition()
    {
        
        return docombo &&
               (Attackstate == AttackStates.Impact ||
                Attackstate == AttackStates.Cooldown);
    }


    // 玩家专属Hitbox启用
    public void EnablePlayerHitbox(AttackData attack)
    {
        

        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (leftHandCollider != null)
                {
                    leftHandCollider.enabled = true;
                    Debug.Log("启用玩家左手Hitbox");
                }
                break;
            case AttackHitbox.RightHand:
                if (rightHandCollider != null)
                {
                    rightHandCollider.enabled = true;
                    Debug.Log("启用玩家右手Hitbox");
                }
                break;
            case AttackHitbox.LeftFoot:
                if (leftFootCollider != null)
                {
                    leftFootCollider.enabled = true;
                    Debug.Log("启用玩家左脚Hitbox");
                }
                break;
            case AttackHitbox.RightFoot:
                if (rightFootCollider != null)
                {
                    rightFootCollider.enabled = true;
                    Debug.Log("启用玩家右脚Hitbox");
                }
                break;
            case AttackHitbox.Sword:
                // 玩家使用装备管理器获取武器碰撞器
                var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
                if (weaponCollider != null)
                {
                    weaponCollider.enabled = true;
                    Debug.Log("启用玩家武器Hitbox");
                }
                else
                {
                    Debug.LogWarning("玩家武器碰撞器为null，无法启用");
                }
                break;
            default:
                Debug.Log($"玩家使用未知Hitbox类型: {attack.HitboxToUse}");
                break;
        }
    }


    // 玩家专属Hitbox禁用
    public void DisablePlayerHitboxes()
    {
        // 禁用所有身体部位Hitbox
        if (leftHandCollider != null)
            leftHandCollider.enabled = false;
        if (rightHandCollider != null)
            rightHandCollider.enabled = false;
        if (leftFootCollider != null)
            leftFootCollider.enabled = false;
        if (rightFootCollider != null)
            rightFootCollider.enabled = false;

        // 禁用武器Hitbox
        var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;

        Debug.Log("禁用所有玩家Hitbox");
    }

    public IEnumerator ExecutePlayerAttack(ICombatSystem target, int comboCount)
    {
        // 1. 准备攻击
        PreparePlayerAttack(target);

       
        InAction = true;
        currTarget = target;
        Attackstate = AttackStates.Windup;

        // 2. 获取攻击数据
        var attack = SelectPlayerAttack(target,Attacks, LongRangeAttacks, comboCount, LongRangeAttackThreshold);
        Vector3 attackDir = CalculatePlayerAttackDirection(target);
        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculatePlayerAttackPosition(target, attack, attackDir, startPos);

        // 3. 播放动画
        animator.CrossFade(attack.AttackName, 0.2f);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);

        // 4. 攻击执行循环
        float timer = 0f;
        while (timer <= animstate.length)
        {
            if (IsTakingHit) break;

            timer += Time.deltaTime;
            float normalizedTime = timer / animstate.length;

            // 移动逻辑
            if (target != null && attack.MoveToTarget)
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

            // 转向控制
            if (attackDir != Vector3.zero)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(attackDir),
                    500f * Time.deltaTime);
            }

            // 5. 状态管理
            UpdatePlayerAttackState(normalizedTime, attack);

            // 6. 连击检查
            if (CheckPlayerComboCondition())
            {
                docombo = false;
                int newComboCount = (comboCount + 1) % Attacks.Count;
                StartCoroutine(ExecutePlayerAttack(target, newComboCount));
                yield break;
            }

            yield return null;
        }

        // 7. 攻击结束
        ResetPlayerAttackState();
        FinishPlayerAttack();

        currTarget = null;
    }




    #region ICombatSystem接口方法实现接口方法实现

    public bool HasUsableWeapon() => PlayerHasUsableWeapon();//HasUsebleWeapon接口实现
    public bool CanAttack() => PlayerCanAttack();//CanAttack接口实现
    public void TryToAttack(ICombatSystem target = null) => PlayerTryToAttack(target);//TryAttack接口实现
    public Vector3 CalculateAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
      => CalculatePlayerAttackPosition(target, attack, attackDir, startPos);//CalculateAttackPosition接口实现
    public Vector3 CalculateAttackDirection(ICombatSystem target) => CalculatePlayerAttackDirection(target);//CalculateAttackDirection接口实现
    public void PrepareAttack(ICombatSystem target) => PreparePlayerAttack(target);// PrepareAttack接口实现
    public void FinishAttack() => FinishPlayerAttack();
    public void UpdateAttackState(float normalizedTime, AttackData attack) => UpdatePlayerAttackState(normalizedTime, attack);
    public void ResetAttackState() => ResetPlayerAttackState();
    public bool CheckComboCondition() => CheckPlayerComboCondition();//CheckComboCondition接口实现
    public void EnableHitbox(AttackData attack) => EnablePlayerHitbox(attack);
    public void DisableHitboxes() => DisablePlayerHitboxes();
    public AttackData SelectAttack(ICombatSystem target, int comboCount)
    {
        return SelectPlayerAttack(target, Attacks, LongRangeAttacks, comboCount, LongRangeAttackThreshold);
    }
    public IEnumerator ExecuteAttack(ICombatSystem target, int comboCount)
    {
        yield return ExecutePlayerAttack(target, comboCount);
    }
    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;
    
    #endregion
}

