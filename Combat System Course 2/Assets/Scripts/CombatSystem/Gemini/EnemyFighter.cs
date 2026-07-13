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

    [SerializeField] protected AudioClip hitSound;         // 命中音效
    [SerializeField] protected GameObject hitFxPrefab;     // 飙血特效预制体（旧，保留兼容）
    [SerializeField] protected GameObject[] bloodSplashPrefabs;  // BloodEffectsPack Splash
    [SerializeField] protected GameObject[] bloodDecalPrefabs;   // BloodEffectsPack DecalProjector
    [SerializeField] private float knockbackDistance = 3f; // 特殊击退距离

    [Header("Rebound")]
    [SerializeField] [Range(0f, 0.5f)] private float reboundFreezeDuration = 0.02f;
    [SerializeField] [Range(-5f, -0.1f)] private float reboundSpeed = -1f;
    [SerializeField] private GameObject reboundVfxPrefab;
    [SerializeField] private AudioClip reboundSfx;
    public bool IsRebounding { get; private set; } = false;
    private Coroutine reboundCoroutine;
    private Vector3 lastReboundHitPoint;

    private HashSet<int> hitTargets = new HashSet<int>();

    // 健康系统
    public HealthSystem HealthSystem { get; private set; }

    // 战斗状态
    public GameObject blockObject;
    public bool InAction { get; set; } = false;
    public bool IsInPassiveAction
    {
        get => IsTakingHit || IsRebounding;  
    }
    public float CritRate => 0f;  // 敌人暴击率=0（静态，玩家可背板）
    public float CritDamage=>1;// 敌人暴击效果=x1 相当于无暴击效果，纯实现一个接口
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;

    // 特殊受击动画（攻击时从 AttackData 设置，空字符串 = 使用默认）
    public string CurrentSpecialHitReaction { get; set; }

    // 当前正在执行的 AttackData（伤害 + 受击动画均从此读取）
    private AttackData currentAttackData;
    public bool IsCurrentAttackKnockdown => currentAttackData != null && currentAttackData.IsKnockdown;

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

    /// <summary>Boss 用：动态替换攻击列表，运行时选择不同攻击</summary>
    public void OverrideAttacks(List<AttackData> newAttacks) { attacks = newAttacks; }

    [SerializeField] protected int attackAnimLayer = 0; // Boss 动画在 Action Layer(1)，普通敌人在 Base Layer(0)
    public int AttackAnimLayer => attackAnimLayer;

    // 组件引用
    public Animator animator { get; private set; }
    public BoxCollider WeaponCollider { get; protected set; }


    [field: SerializeField] public Collider leftHandCollider { get; private set; }
    [field: SerializeField] public Collider rightHandCollider { get; private set; }
    [field: SerializeField] public Collider leftFootCollider { get; private set; }
    [field: SerializeField] public Collider rightFootCollider { get; private set; }
    [field: SerializeField] public Collider bodyCollider { get; private set; }

    // 事件
    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;
    public event System.Action<GameObject> OnDamageDealt;
    public void NotifyDamageDealt(GameObject target) => OnDamageDealt?.Invoke(target);

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
    // 碰撞器初始化：序列化字段优先（手动拖拽），为空时自动从骨骼查找
    protected virtual void InitializeEnemyBodyColliders()
    {
        if (leftHandCollider  == null) leftHandCollider  = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<Collider>();
        if (rightHandCollider == null) rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<Collider>();
        if (leftFootCollider  == null) leftFootCollider  = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<Collider>();
        if (rightFootCollider == null) rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<Collider>();

        if (bodyCollider == null)
            bodyCollider = animator.GetBoneTransform(HumanBodyBones.Spine)?.GetComponent<Collider>();
        if (bodyCollider == null)
            bodyCollider = animator.GetBoneTransform(HumanBodyBones.Hips)?.GetComponent<Collider>();

        // 全部默认关闭
        if (leftHandCollider)  leftHandCollider.enabled  = false;
        if (rightHandCollider) rightHandCollider.enabled = false;
        if (leftFootCollider)  leftFootCollider.enabled  = false;
        if (rightFootCollider) rightFootCollider.enabled = false;
        if (bodyCollider)      bodyCollider.enabled      = false;

        // 敌人使用自己的武器（默认关闭碰撞体，仅在 Impact 阶段由 EnableEnemyHitbox 打开）
        if (enemyWeapon != null)
        {
            WeaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
            if (WeaponCollider != null)
                WeaponCollider.enabled = false;
        }
    }

    [SerializeField] private float unarmedDamage = 25f;

    public float GetWeaponDamage()
    {
        return currentAttackData?.Damage ?? enemyWeapon?.GetDamage() ?? unarmedDamage;
    }

    public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = 0;

        HealthSystem.TakeDamage(damage, currentArmor, isCrit);
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

        // 无武器也可徒手攻击（Boss等），伤害取 unarmedDamage
        return true;
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
    public IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null, bool isKnockdown = false)
    {
        InAction = true;
        IsTakingHit = true;

        var dispVec = attacker.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);

        string hitAnim = string.IsNullOrEmpty(specialHitReaction) ? "SwordImpact" : specialHitReaction;
        bool isSpecial = !string.IsNullOrEmpty(specialHitReaction);

        // 特殊击退：脚本移动替代 root motion（不依赖 FBX Bake Into Pose 设置）
        Vector3 knockbackDir = Vector3.zero;
        Vector3 knockbackStart = Vector3.zero;
        Vector3 knockbackTarget = Vector3.zero;
        if (isSpecial)
        {
            knockbackDir = (transform.position - attacker.transform.position).normalized;
            knockbackDir.y = 0f;
            knockbackStart = transform.position;
            knockbackTarget = knockbackStart + knockbackDir * knockbackDistance;
            if (navAgent != null) navAgent.isStopped = true;
        }

        bool isHumanoid = true;

        if (isHumanoid)
        {
            animator.CrossFade(hitAnim, 0.2f, 1);
            yield return null;
            var animstate = animator.GetNextAnimatorStateInfo(1);
            float waitTime = isSpecial ? animstate.length : animstate.length * 0.8f;

            if (isSpecial)
            {
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / waitTime);
                    float eased = 1f - (1f - t) * (1f - t); // ease-out
                    transform.position = Vector3.Lerp(knockbackStart, knockbackTarget, eased);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }
        }
        else
        {
            animator.CrossFade(hitAnim, 0.2f, 0);
            yield return null;
            var animstate = animator.GetCurrentAnimatorStateInfo(0);
            float waitTime = isSpecial ? animstate.length : animstate.length * 0.8f;

            if (isSpecial)
            {
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / waitTime);
                    float eased = 1f - (1f - t) * (1f - t);
                    transform.position = Vector3.Lerp(knockbackStart, knockbackTarget, eased);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }
        }

        if (isSpecial)
        {
            transform.position = knockbackTarget;
            if (navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.Warp(transform.position);
                navAgent.isStopped = false;
            }
        }

        OnHitComplete?.Invoke();
        InAction = false;
        IsTakingHit = false;
    }
    public void PlayDeathAnimation(ICombatSystem attacker)
    {
        animator.CrossFade("Death", 0.2f);
    }
    protected void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;

        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();

            // 防止同一刀命中同一目标多次
            if (!attacker.RegisterHit(this.gameObject)) 
                return;
            
            // 格挡中不受伤害（重武器无视格挡）
            if (blockObject != null && blockObject.activeSelf && !attacker.IsUsingHeavyWeapon()) return;
            bool isCrit = Random.value < (attacker.CritRate / 100f);
            float finalDamage=isCrit?attackerDamage*attacker.CritDamage:attackerDamage;
            TakeDamage(finalDamage, isCrit);

            // 通知攻击方：成功造成伤害（用于命中转向等）
            attacker.NotifyDamageDealt(this.gameObject);

            // 顿帧（攻击者 + 自己）
            Animator attackerAnimator = (attacker as MonoBehaviour)?.GetComponent<Animator>();
            Animator selfAnimator = GetComponent<Animator>();
            Debug.Log($"attacker={attacker}, animator={attackerAnimator}");
            if (attackerAnimator != null)
                HitDelay.Instance.Stop(0.07f, attackerAnimator);

            if (selfAnimator != null)
                HitDelay.Instance.Stop(0.04f, selfAnimator);

            // 命中音效 + 血液特效（在 OnHitReaction 之前，Boss 也能享受）
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, hitPoint, 0.8f);
            BloodEffectManager.SpawnBlood(hitPoint, bloodSplashPrefabs, bloodDecalPrefabs);

            string specialReaction = attacker.CurrentSpecialHitReaction;
            attacker.CurrentSpecialHitReaction = null;
            OnHitReaction(attacker, specialReaction);
        }
    }



    /// <summary>受击反应：默认播放受击动画。Boss 子类重写跳过。</summary>
    protected virtual void OnHitReaction(ICombatSystem attacker, string specialReaction)
    {
        if (!HealthSystem.IsDead)
        {
            StartCoroutine(PlayHitReaction(attacker, specialReaction));
        }
        else
        {
            PlayDeathAnimation(attacker);
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
        hitTargets.Clear();

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
            case AttackHitbox.BothHands:
                if (leftHandCollider != null) leftHandCollider.enabled = true;
                if (rightHandCollider != null) rightHandCollider.enabled = true;
                Debug.Log($"启用敌人({gameObject.name})双手Hitbox");
                break;
            case AttackHitbox.BothFeet:
                if (leftFootCollider != null) leftFootCollider.enabled = true;
                if (rightFootCollider != null) rightFootCollider.enabled = true;
                Debug.Log($"启用敌人({gameObject.name})双脚Hitbox");
                break;
            case AttackHitbox.Body:
                if (bodyCollider != null)
                {
                    bodyCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})身体Hitbox");
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
        if (bodyCollider != null)
            bodyCollider.enabled = false;

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

    // ==========================================
    // 动画事件可调用的单部位开关（复杂多段打击用）
    // ==========================================
    public void AnimEvent_ClearHitTargets() => hitTargets.Clear();

    public void AnimEvent_EnableLeftHand()  { if (leftHandCollider  != null) leftHandCollider.enabled  = true; }
    public void AnimEvent_EnableRightHand() { if (rightHandCollider != null) rightHandCollider.enabled = true; }
    public void AnimEvent_EnableLeftFoot()  { if (leftFootCollider  != null) leftFootCollider.enabled  = true; }
    public void AnimEvent_EnableRightFoot() { if (rightFootCollider != null) rightFootCollider.enabled = true; }
    public void AnimEvent_EnableBody()      { if (bodyCollider      != null) bodyCollider.enabled      = true; }
    public void AnimEvent_EnableBothHands() { AnimEvent_EnableLeftHand();  AnimEvent_EnableRightHand(); }
    public void AnimEvent_EnableBothFeet()  { AnimEvent_EnableLeftFoot();  AnimEvent_EnableRightFoot(); }

    public bool IsUsingHeavyWeapon()
    {
        return enemyWeapon?.isHeavy ?? false;
    }

    public bool RegisterHit(GameObject target)
    {
        int id = target.GetInstanceID();
        if (hitTargets.Contains(id)) return false;
        hitTargets.Add(id);
        return true;
    }

    // ==========================================
    // 反弹系统 (由 Weapon.OnTriggerEnter 调用)
    // ==========================================

    /// <summary>只播反弹 VFX + 音效，不倒放动画（Boss 用）</summary>
    public void PlayReboundVfx(Vector3 hitPoint)
    {
        if (reboundVfxPrefab != null)
            Instantiate(reboundVfxPrefab, hitPoint, Quaternion.identity);
        if (reboundSfx != null)
            AudioSource.PlayClipAtPoint(reboundSfx, hitPoint);
    }

    public void OnWeaponRebound(Vector3 hitPoint)
    {
        if (!InAction) return;
        if (IsRebounding) return;
        if (HealthSystem.IsDead) return;

        lastReboundHitPoint = hitPoint;
        reboundCoroutine = StartCoroutine(DoRebound());
    }

    private IEnumerator DoRebound()
    {
        IsRebounding = true;

        // ① 关闭武器碰撞体
        var weaponCollider = GetComponentInChildren<Weapon>()?.GetComponentInChildren<BoxCollider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;

        // ② 定格 + VFX/音效
        animator.SetFloat("AttackSpeed", 0f);

        if (reboundVfxPrefab != null)
            Instantiate(reboundVfxPrefab, lastReboundHitPoint, Quaternion.identity);

        if (reboundSfx != null)
            AudioSource.PlayClipAtPoint(reboundSfx, lastReboundHitPoint);

        yield return new WaitForSeconds(reboundFreezeDuration);

        if (IsTakingHit || HealthSystem.IsDead)
            goto Abort;

        // ③ 倒放攻击动画
        var state = animator.GetCurrentAnimatorStateInfo(0);
        float currentNormTime = state.normalizedTime;
        int stateHash = state.fullPathHash;

        var clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        float clipLength = 1f;
        if (clipInfos.Length > 0)
            clipLength = clipInfos[0].clip.length;
        else if (!float.IsInfinity(state.length) && state.length > 0.01f)
            clipLength = state.length;

        float reverseDuration = currentNormTime * clipLength / Mathf.Abs(reboundSpeed);

        animator.SetFloat("AttackSpeed", reboundSpeed);

        float elapsed = 0f;
        while (elapsed < reverseDuration)
        {
            if (IsTakingHit || HealthSystem.IsDead)
                goto Abort;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ④ 切回待机，和玩家 Play("Combat Blend Tree") 同理
        animator.SetFloat("AttackSpeed", 1f);
        animator.Play("CombatMovement", 0, 0);
        DisableEnemyHitboxes();

        if (navAgent != null)
            navAgent.isStopped = false;

        Attackstate = AttackStates.Idle;
        comboCount = 0;
        docombo = false;
        currTarget = null;
        InAction = false;
        IsRebounding = false;

        yield break;

    Abort:
        animator.SetFloat("AttackSpeed", 1f);
        DisableEnemyHitboxes();

        if (navAgent != null)
            navAgent.isStopped = false;

        Attackstate = AttackStates.Idle;
        comboCount = 0;
        docombo = false;
        currTarget = null;
        InAction = false;
        IsRebounding = false;
    }

    // 供外部强制重置攻击状态
    public void ForceResetAttackState()
    {
        Attackstate = AttackStates.Idle;
        comboCount = 0;
        docombo = false;
        InAction = false;
    }

public IEnumerator ExecuteEnemyAttack(ICombatSystem target = null, int comboCount = 0)
{
    Debug.Log($"[EnemyAttack] 开始执行敌人攻击，目标: {(target != null ? target.gameObject.name : "null")}");
    InAction = true;
    currTarget = target;
    Debug.Log($"[EnemyAttack] 敌人攻击目标设置为: {currTarget?.gameObject?.name}");
    Attackstate = AttackStates.Windup;

    var attack = attacks[comboCount];
    currentAttackData = attack;
    CurrentSpecialHitReaction = attack.SpecialHitReaction;

    bool vfxSpawned = false;
    bool shakeTriggered = false;
    bool sfxPlayed = false;

    var attackDir = transform.forward;
    Vector3 startPos = transform.position;
    Vector3 targetPos = Vector3.zero;
    bool hasReachedTarget = false; // 标记是否已到达目标位置

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
            targetPos.y = transform.position.y; // 保持Y轴
        }
    }

    animator.CrossFade(attack.AttackName, 0.2f, attackAnimLayer);
    yield return null;
    var animstate = animator.GetNextAnimatorStateInfo(attackAnimLayer);

    float timer = 0f;
    while (timer <= animstate.length)
    {
        if (IsTakingHit) break;
        if (IsRebounding) break;
        timer += Time.deltaTime;
        float normalizedTime = timer / animstate.length;

        // ===== 移动逻辑 =====
        if (target != null && attack.MoveToTarget && !hasReachedTarget)
        {
            if (normalizedTime >= attack.MoveStartTime && normalizedTime <= attack.MoveEndTime)
            {
                Vector3 vecToTarget = target.transform.position - transform.position;
                vecToTarget.y = 0;
                Vector3 currentDir = vecToTarget.normalized;
                float currentDistance = vecToTarget.magnitude;

                // 使用速度移动
                Vector3 moveDelta = currentDir * attack.MoveSpeed * Time.deltaTime;

                // 不超出目标位置
                float stopDistance = attack.DistanceFromTarget;
                if (currentDistance - moveDelta.magnitude <= stopDistance)
                {
                    Vector3 finalPos = target.transform.position - currentDir * stopDistance;
                    finalPos.y = transform.position.y;
                    transform.position = finalPos;
                    hasReachedTarget = true;
                }
                else
                {
                    transform.position += moveDelta;
                }

                attackDir = currentDir;
            }
        }

        // ===== 旋转逻辑 =====
        if (!attack.IsSpinAttack && attackDir != Vector3.zero)
        {
            // 只在 Impact 阶段旋转，且移动结束后不再旋转（或极慢旋转）
            if (Attackstate == AttackStates.Impact && !hasReachedTarget)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    Quaternion.LookRotation(attackDir), 
                    500f * Time.deltaTime
                );
            }
            else if (Attackstate == AttackStates.Windup)
            {
                // 蓄力阶段缓慢转向，避免抽搐
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    Quaternion.LookRotation(attackDir), 
                    150f * Time.deltaTime
                );
            }
            // Cooldown 阶段不旋转，保持当前朝向
        }

        // ===== 攻击状态机 =====
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
                int newComboCount = (comboCount + 1) % attacks.Count;
                StartCoroutine(ExecuteEnemyAttack(target, newComboCount));
                yield break;
            }
        }

        // ===== 摄像机震动 =====
        if (!shakeTriggered && attack.EnableCameraShake && normalizedTime >= attack.CameraShakeTime)
        {
            shakeTriggered = true;
            var camCtrl = FindObjectOfType<PlayerCameraController>();
            if (camCtrl != null)
            {
                camCtrl.ShakeCamera(attack.CameraShakeIntensity, attack.CameraShakeDuration, attack.CameraShakeFrequency);
            }
        }

        // ===== 攻击音效 =====
        if (!sfxPlayed && attack.AttackSFX != null && normalizedTime >= attack.SFXSpawnTime)
        {
            sfxPlayed = true;
            AudioSource.PlayClipAtPoint(attack.AttackSFX, transform.position);
        }

        // ===== 攻击特效生成 =====
        if (!vfxSpawned && attack.AttackVFXPrefab != null && normalizedTime >= attack.VFXSpawnTime)
        {
            vfxSpawned = true;
            Vector3 spawnPos = transform.position + attack.VFXSpawnOffset;
            GameObject vfx = Instantiate(attack.AttackVFXPrefab, spawnPos, Quaternion.identity);
            if (attack.VFXFollowAttacker)
                vfx.transform.SetParent(transform);
        }

        yield return null;
    }

    // ===== 收尾清理 =====
    if (!IsRebounding)
    {
        Attackstate = AttackStates.Idle;
        comboCount = 0;
        currTarget = null;
        currentAttackData = null;
    }
    if (!IsTakingHit && !IsRebounding)
    {
        InAction = false;
    }
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

    // 供 Enemy/Boss 状态机直接调用的公开方法（已从 ICombatSystem 移除）
    public void TryToAttack(ICombatSystem target = null) => EnemyTryToAttack(target);
    public void DisableHitboxes() => DisableEnemyHitboxes();

    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;
}