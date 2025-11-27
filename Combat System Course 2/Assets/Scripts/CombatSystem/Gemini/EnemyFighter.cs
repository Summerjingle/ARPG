using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyFighter : MonoBehaviour, ICombatSystem
{

    protected Weapon enemyWeapon;
    private NavMeshAgent navAgent;
    private EnemyController enemyController;
    

    private float decisionCooldown;
    private Vector3 lastKnownPlayerPosition;

    [SerializeField] private AudioClip hitSound;         // 命中音效
    [SerializeField] private GameObject hitFxPrefab;     // 飙血特效预制体


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
    public BoxCollider WeaponCollider { get; protected set; }
    public SphereCollider leftHandCollider { get; private set; }
    public SphereCollider rightHandCollider { get; private set; }
    public SphereCollider leftFootCollider { get; private set; }
    public SphereCollider rightFootCollider { get; private set; }

    // 事件
    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;

    protected virtual void Awake()
    {
        enemyWeapon = GetComponentInChildren<Weapon>();
        navAgent = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();
        
        HealthSystem = GetComponent<HealthSystem>();
        if (HealthSystem == null)
            HealthSystem = gameObject.AddComponent<HealthSystem>();

        animator = GetComponent<Animator>();

        // 初始化碰撞器
        InitializeEnemyBodyColliders();
    }
    // 敌人专属的碰撞器初始化
    protected virtual void InitializeEnemyBodyColliders()
    {
        // 从 MeleeFighter 迁移过来的敌人碰撞器初始化逻辑
        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<SphereCollider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<SphereCollider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<SphereCollider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<SphereCollider>();

        // 敌人使用自己的武器
        if (enemyWeapon != null)
        {
            WeaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
        }
    }

    public float GetWeaponDamage()
    {
        return enemyWeapon?.GetDamage() ?? 1f;
    }

    public virtual void TakeDamage(float damage)
    {
        if (HealthSystem.IsDead) return;

        // 敌人护甲逻辑（可以根据需要扩展）
        int currentArmor = 0;

        // 如果有需要，可以在这里添加敌人的护甲计算
        // 例如：var enemyProperty = GetComponent<EnemyProperty>();
        // currentArmor = enemyProperty?.armorValue ?? 0;

        HealthSystem.TakeDamage(damage, currentArmor);
        OnGotHit?.Invoke(this);  // this 就是 ICombatSystem

        Debug.Log($"敌人({gameObject.name})受到伤害: {damage}, 护甲减免: {currentArmor}, 剩余生命: {HealthSystem.Health}");
    }
    public bool EnemyHasUsableWeapon()
    {
        // 优化敌人武器检查逻辑
        var weapon = GetComponentInChildren<Weapon>();
        if (weapon != null)
        {
            Debug.Log($"敌人武器: {weapon.name}, 伤害: {weapon.GetDamage()}");
            return true;
        }

        Debug.LogWarning($"{gameObject.name} 没有找到可用武器！");
        return false;
    }
    public bool EnemyCanAttack()
    {
        
        return !InAction && EnemyHasUsableWeapon();
    }

    public void EnemyTryToAttack(ICombatSystem target = null)
    {
        if (EnemyCanAttack())
        {
            StartCoroutine(ExecuteEnemyAttack(target, comboCount));
        }
        else if (Attackstate == AttackStates.Impact ||
                 Attackstate == AttackStates.Cooldown)
        {
            docombo = true;
        }

    }

    public Vector3 CalculateEnemyAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
    {
        // 敌人通常不需要复杂的移动计算，NavAgent会处理
        // 返回当前位置，让NavAgent控制移动
        return startPos;
    }

    // 敌人攻击方向计算 - 面向目标即可
    public Vector3 CalculateEnemyAttackDirection(ICombatSystem target)
    {
        if (target != null)
        {
            var vecToTarget = target.transform.position - transform.position;
            vecToTarget.y = 0;
            return vecToTarget.normalized;
        }
        return transform.forward;
    }

    // 敌人攻击数据选择 
    public AttackData SelectEnemyAttack(ICombatSystem target, List<AttackData> attacks, List<AttackData> longRangeAttacks, int comboCount)
    {
        // 敌人可能基于距离、状态等选择攻击
        // 目前保持简单逻辑
        return attacks[comboCount % attacks.Count];
    }

    // 敌人特定的攻击准备逻辑
    public void PrepareEnemyAttack(ICombatSystem target)
    {

        var enemyController = GetComponent<EnemyController>();

        if (enemyController != null)
        {
            // 敌人可能需要在攻击前停止导航
            enemyController.NavAgent.isStopped = true;
        }

        // 设置攻击目标
        currTarget = target;
    }

    // 敌人攻击结束逻辑
    public void FinishEnemyAttack()
    {
        var enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            // 恢复导航
            enemyController.NavAgent.isStopped = false;
        }
    }
    public IEnumerator PlayHitReaction(ICombatSystem attacker)
    {
        InAction = true;
        IsTakingHit = true;

        var dispVec = attacker.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);

        // 检查敌人类型
        bool isHumanoid = GetComponent<WolfController>() == null;

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
            // 狼：使用图层0
            animator.CrossFade("SwordImpact", 0.2f, 0);
            yield return null;
            var animstate = animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(animstate.length * 0.8f);
        }

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

        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();
            TakeDamage(attackerDamage);
            HitEffect.Instance.PlaySound(hitSound, transform.position);
            HitEffect.Instance.PlayFX(hitFxPrefab,
                other.ClosestPointOnBounds(transform.position),
                Quaternion.LookRotation(attacker.transform.forward)
            );

            // 顿帧（攻击者 + 自己）
            Animator attackerAnimator = (attacker as MonoBehaviour)?.GetComponent<Animator>();
            Animator selfAnimator = GetComponent<Animator>();
            Debug.Log($"attacker={attacker}, animator={attackerAnimator}");
            if (attackerAnimator != null)
                HitDelay.Instance.Stop(0.07f, attackerAnimator);

            if (selfAnimator != null)
                HitDelay.Instance.Stop(0.04f, selfAnimator);

            // 受击动画
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



    // 敌人专属状态管理
    public void UpdateEnemyAttackState(float normalizedTime, AttackData attack)
    {

        if (Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                Attackstate = AttackStates.Impact;
                Debug.Log($"敌人({gameObject.name})攻击进入Impact状态");
                EnableEnemyHitbox(attack);
            }
        }
        else if (Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                Attackstate = AttackStates.Cooldown;
                Debug.Log($"敌人({gameObject.name})攻击进入Cooldown状态");
                DisableEnemyHitboxes();
            }
        }
    }

    // 敌人状态重置
    public void ResetEnemyAttackState()
    {
        Attackstate = AttackStates.Idle;
        InAction = false;
       

        var enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.NavAgent.isStopped = false;
        }
        Debug.Log($"敌人({gameObject.name})攻击状态重置");
    }

    // 敌人连击状态检查
    public bool CheckEnemyComboCondition()
    {
        return docombo &&
               (Attackstate == AttackStates.Impact ||
                Attackstate == AttackStates.Cooldown);
    }

    // 敌人专属Hitbox启用
    public void EnableEnemyHitbox(AttackData attack)
    {

        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (leftHandCollider != null)
                {
                    leftHandCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})左手Hitbox");
                }
                break;
            case AttackHitbox.RightHand:
                if (rightHandCollider != null)
                {
                    rightHandCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})右手Hitbox");
                }
                break;
            case AttackHitbox.LeftFoot:
                if (leftFootCollider != null)
                {
                    leftFootCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})左脚Hitbox");
                }
                break;
            case AttackHitbox.RightFoot:
                if (rightFootCollider != null)
                {
                    rightFootCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})右脚Hitbox");
                }
                break;
            case AttackHitbox.Sword:
                // 敌人使用自己的武器组件
                var enemyWeapon = GetComponentInChildren<Weapon>();
                if (enemyWeapon != null)
                {
                    var weaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
                    if (weaponCollider != null)
                    {
                        weaponCollider.enabled = true;
                        Debug.Log($"启用敌人({gameObject.name})武器Hitbox");
                    }
                }
                else
                {
                    Debug.LogWarning($"敌人({gameObject.name})武器为null，无法启用Hitbox");
                }
                break;
            default:
                Debug.Log($"敌人使用未知Hitbox类型: {attack.HitboxToUse}");
                break;
        }
    }

    // 敌人专属Hitbox禁用
    public void DisableEnemyHitboxes()
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

        // 禁用敌人武器Hitbox
        var enemyWeapon = GetComponentInChildren<Weapon>();
        if (enemyWeapon != null)
        {
            var weaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
            if (weaponCollider != null)
                weaponCollider.enabled = false;
        }

        Debug.Log($"禁用所有敌人({gameObject.name})Hitbox");
    }
    public IEnumerator ExecuteEnemyAttack(ICombatSystem target = null, int comboCount = 0)
    {
        Debug.Log($"[EnemyAttack] 开始执行敌人攻击，目标: {(target != null ? target.gameObject.name : "null")}");
        float damage = GetWeaponDamage();
        InAction = true;
        currTarget = target;
        Debug.Log($"[EnemyAttack] 敌人攻击目标设置为: {currTarget?.gameObject?.name}");
        Attackstate = AttackStates.Windup;

        var attack = attacks[comboCount];

        var attackDir = transform.forward;
        Vector3 startPos = transform.position;
        Vector3 targetPos = Vector3.zero;

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

        animator.CrossFade(attack.AttackName, 0.2f);
        yield return null;
        var animstate = animator.GetNextAnimatorStateInfo(1);

        float timer = 0f;
        while (timer <= animstate.length)
        {
            if (IsTakingHit) break;
            timer += Time.deltaTime;
            float normalizedTime = timer / animstate.length;

            
            if (target != null && attack.MoveToTarget)
            {
                float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);
                Vector3 desiredPosition = Vector3.Lerp(startPos, targetPos, percTime);

               
                float currentDistance = Vector3.Distance(transform.position, target.transform.position);
                if (currentDistance > 1.0f) 
                {
                    transform.position = desiredPosition;
                }
            }

           
            if (attackDir != null)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(attackDir), 500f * Time.deltaTime);
            }
            if (Attackstate == AttackStates.Windup)
            {
                if (InCounter) break;
                if (normalizedTime >= attack.ImpactStartTime)
                {
                    Attackstate = AttackStates.Impact;
                    EnableEnemyHitbox(attack);
                }
            }
            else if (Attackstate == AttackStates.Impact)
            {
                if (normalizedTime >= attack.ImpactEndTime)
                {
                    Attackstate = AttackStates.Cooldown;
                    DisableEnemyHitboxes();
                }
            }
            else if (Attackstate == AttackStates.Cooldown)
            {
                if (docombo)
                {
                    docombo = false;
                    int newComboCount = (comboCount + 1) % attacks.Count;  // 计算新的连击数
                    StartCoroutine(ExecuteEnemyAttack(target, newComboCount));  // 
                    yield break;
                }
            }
            yield return null;
        }
        //�ȴ��������
        Attackstate = AttackStates.Idle;
        comboCount = 0;
        InAction = false;
        currTarget = null;
    }
    public void UpdateEnemyAttackStateWithCombo(float normalizedTime, AttackData attack)
    {
        if (Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                Attackstate = AttackStates.Impact;
                Debug.Log($"敌人({gameObject.name})攻击进入Impact状态");
                EnableEnemyHitbox(attack);
            }
        }
        else if (Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                Attackstate = AttackStates.Cooldown;
                Debug.Log($"敌人({gameObject.name})攻击进入Cooldown状态");
                DisableEnemyHitboxes();
            }
        }
      
    }

    #region ICombatSystem接口方法实现


    public bool CanAttack() => EnemyCanAttack();//能否进行攻击
    public void TryToAttack(ICombatSystem target = null) => EnemyTryToAttack(target);//尝试攻击
    
    public bool HasUsableWeapon() => EnemyHasUsableWeapon();//检查是否有武器
    public AttackData SelectAttack(ICombatSystem target, int comboCount)
        => SelectEnemyAttack(target, Attacks,  LongRangeAttacks, comboCount);//选择攻击数据
    public Vector3 CalculateAttackDirection(ICombatSystem target) => CalculateEnemyAttackDirection(target);//计算攻击时的朝向
    public Vector3 CalculateAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos)
        => CalculateEnemyAttackPosition(target, attack, attackDir, startPos);//计算攻击时移动到的位置
    public void UpdateAttackState(float normalizedTime, AttackData attack) => UpdateEnemyAttackState(normalizedTime, attack);//更新攻击数据
    public void ResetAttackState() => ResetEnemyAttackState();//重置攻击数据
    public void EnableHitbox(AttackData attack) => EnableEnemyHitbox(attack);//启用碰撞体
    public void DisableHitboxes() => DisableEnemyHitboxes();//禁用碰撞体
    public void PrepareAttack(ICombatSystem target) => PrepareEnemyAttack(target);//攻击
    public void FinishAttack() => FinishEnemyAttack();//攻击完成
    public bool CheckComboCondition() => CheckEnemyComboCondition();//查看连招状态

    public IEnumerator ExecuteAttack(ICombatSystem target, int comboCount)
    {
        yield return ExecuteEnemyAttack(target, comboCount);
    }

    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;
    #endregion
}