using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFighter : MonoBehaviour, ICombatSystem
{
    public WeaponEquipmentManager weaponManager;
    private PlayerProperty playerProperty;
    private int actionLayerIndex;
    private int hitLayerIndex;
    
    // 核心战斗系统属性
    public HealthSystem HealthSystem { get; private set; }
    private HashSet<int> hitTargets = new HashSet<int>();
    
    // 受击特效
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private GameObject[] bloodSplashPrefabs;
    [SerializeField] private GameObject[] bloodDecalPrefabs;

    // 状态标志
    private bool _inAction = false;
    private string lockOwner = null;
    
    public bool InAction 
    { 
        get => _inAction;
        set 
        {
            if (lockOwner != null && value == false)
            {
                Debug.Log($"[InAction] 被 {lockOwner} 锁定，无法设为 false");
                return;
            }
            _inAction = value;
        }
    }
    public bool IsKnockedDown { get; private set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    //被动InAction
    public bool IsInPassiveAction
    {
        get => IsTakingHit || IsKnockedDown || IsRebounding;  
    }
    // 反弹状态
    [Header("Rebound")]
    [SerializeField] [Range(0f, 0.5f)] private float reboundFreezeDuration = 0.02f;
    [SerializeField] [Range(-5f, -0.1f)] private float reboundSpeed = -1f;
    [SerializeField] private GameObject reboundVfxPrefab;
    [SerializeField] private AudioClip reboundSfx;
    public bool IsRebounding { get; private set; } = false;
    private Coroutine reboundCoroutine;
    private Vector3 lastReboundHitPoint;

    // 特殊受击动画（Animation Event 设置，null/空 = 使用默认受击动画）
    public string CurrentSpecialHitReaction { get; set; }

    // 目标管理
    public ICombatSystem currTarget { get; set; }
    public GameObject blockObject;
    public float CritRate => playerProperty?.TotalCritRate ?? 0f;
    public float CritDamage=>playerProperty?.TotalCritDamage??1.5f;
    public bool IsCurrentAttackKnockdown => false;
    // 碰撞体引用
    public Animator animator { get; private set; }
    public BoxCollider WeaponCollider { get; private set; }
    public Collider leftHandCollider { get; private set; }
    public Collider rightHandCollider { get; private set; }
    public Collider leftFootCollider { get; private set; }
    public Collider rightFootCollider { get; private set; }
    
    // 事件
    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;
    public event System.Action<GameObject> OnDamageDealt;
    public void NotifyDamageDealt(GameObject target) => OnDamageDealt?.Invoke(target);

    void OnEnable()
    {
        InputManager.Instance.OnBlock += DoBlock;
        InputManager.Instance.Actions.Player.GetUp.performed += OnGetUpPerformed;
    }

    void OnDisable()
    {
        InputManager.Instance.OnBlock -= DoBlock;
        InputManager.Instance.Actions.Player.GetUp.performed -= OnGetUpPerformed;
    }

    private void OnGetUpPerformed(InputAction.CallbackContext ctx)
    {
        _getUpPressed = true;
    }
    private void Awake()
    {
        weaponManager = WeaponEquipmentManager.Instance;
        playerProperty = GetComponent<PlayerProperty>();
        HealthSystem = GetComponent<HealthSystem>();
        if (HealthSystem == null)
            HealthSystem = gameObject.AddComponent<HealthSystem>();
        animator = GetComponent<Animator>();
        InitializeBodyColliders();
        actionLayerIndex = animator.GetLayerIndex("ActionLayer");
        hitLayerIndex = animator.GetLayerIndex("HitLayer");
    }

    private void InitializeBodyColliders()
    {
        leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)?.GetComponent<Collider>();
        leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)?.GetComponent<Collider>();
        rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)?.GetComponent<Collider>();
        rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)?.GetComponent<Collider>();

        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
        }
    }

    private void Update()
    {
       //锁定敌人
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

    public void TakeDamage(float damage, bool isCrit = false)
    {
        if (HealthSystem.IsDead) return;

        int currentArmor = GetPlayerArmor();
        HealthSystem.TakeDamage(damage, currentArmor, isCrit);
        OnGotHit?.Invoke(this); 

        
        Debug.Log($"玩家受到伤害: {damage}, 当前护甲: {currentArmor}, 剩余血量: {HealthSystem.Health}");
    }


    public int GetPlayerArmor()
    {
        return playerProperty?.armorValue ?? 0;
    }

    private static int _hitReactionSeq = 0; // debug: 协程序号
    private bool _getUpPressed = false; // 提前记录起身按键，防止协程走到检测点前按键已被消耗

    public IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null, bool isKnockdown = false)
    {
        int seq = ++_hitReactionSeq;
        Debug.Log($"[HR#{seq}] 协程启动, isKnockdown={isKnockdown}, 当前 IsKnockedDown={IsKnockedDown}, IsTakingHit={IsTakingHit}, InAction={InAction}, lockOwner={lockOwner}");

        LockInAction("HitReaction");  //上锁
        InAction = true;
        IsTakingHit = true;
        Debug.Log($"[HR#{seq}] 状态置位: InAction=true, IsTakingHit=true, lockOwner={lockOwner}");

        if (PlayerController.i.IsRolling)
        {
            PlayerController.i.ForceStopRoll();
        }

        string hitAnim = string.IsNullOrEmpty(specialHitReaction) ? "hit_light_B_body" : specialHitReaction;
        int targetLayer = isKnockdown ? actionLayerIndex : hitLayerIndex;
        animator.CrossFade(hitAnim, 0.2f, targetLayer);
        Debug.Log($"[HR#{seq}] CrossFade -> {hitAnim}, layer={targetLayer}");

        if (isKnockdown)//受击倒地
        {
            IsKnockedDown = true;
            if (PlayerController.i != null)
                PlayerController.i.LockRotation = true;
            Debug.Log($"[HR#{seq}] 进入击倒分支, IsKnockedDown=true, LockRotation=true");

            // 等待进入 Loop_DownUp（OnKnockdownLoopEnter 将 IsTakingHit 设为 false）
            Debug.Log($"[HR#{seq}] 等待 OnKnockdownLoopEnter (IsTakingHit -> false)...");
            yield return new WaitUntil(() => !IsTakingHit);
            Debug.Log($"[HR#{seq}] WaitUntil(① !IsTakingHit) 通过, 当前 ActionLayer state={animator.GetCurrentAnimatorStateInfo(actionLayerIndex).shortNameHash}");

            // 等待玩家按下起身键（先检查是否在动画过渡期间已按下）
            var getUpAction = InputManager.Instance.Actions.Player.GetUp;
            Debug.Log($"[HR#{seq}] 等待 GetUp 按键... (_getUpPressed={_getUpPressed})");
            yield return new WaitUntil(() =>
            {
                if (_getUpPressed) return true;
                return getUpAction.WasPressedThisFrame();
            });
            _getUpPressed = false; // 消费标记
            Debug.Log($"[HR#{seq}] WaitUntil(② GetUp按键) 通过, 设置 GetUp trigger");

            animator.SetTrigger("GetUp");

            // 等待起身动画完成（OnGetUpComplete 设置 IsTakingHit = false，并清理 InAction / LockRotation）
            Debug.Log($"[HR#{seq}] 等待 OnGetUpComplete (IsTakingHit -> false)...");
            yield return new WaitUntil(() => !IsTakingHit);
            Debug.Log($"[HR#{seq}] WaitUntil(③ !IsTakingHit) 通过, 协程结束");
        }
        else//受击不倒地
        {
            Debug.Log($"[HR#{seq}] 进入不倒地分支");
            yield return null;
            var animstate = animator.GetNextAnimatorStateInfo(targetLayer);
            yield return new WaitForSeconds(animstate.length * 0.8f);

            Debug.Log($"[HR#{seq}] 不倒地分支结束: 调用 OnHitComplete, UnlockInAction, InAction=false, IsTakingHit=false");
            OnHitComplete?.Invoke();
            UnlockInAction("HitReaction");
            InAction = false;
            IsTakingHit = false;
        }

    }

    /// <summary>进入击倒 Loop 阶段，由 Loop_DownUp 动画首帧 Animation Event 调用</summary>
    public void OnKnockdownLoopEnter()
    {
        Debug.Log($"[OnKnockdownLoopEnter] 触发! IsTakingHit=false, 当前 ActionLayer state={animator.GetCurrentAnimatorStateInfo(actionLayerIndex).shortNameHash}");
        IsTakingHit = false;
    }

    /// <summary>起身完成，由 Exit_GetUp 动画末帧 Animation Event 调用</summary>
    public void OnGetUpComplete()
    {
        Debug.Log($"[OnGetUpComplete] 触发! 清理状态: IsTakingHit=false, InAction=false, IsKnockedDown=false, LockRotation=false");
        UnlockInAction("HitReaction");
        IsTakingHit = false;
        InAction = false;
        IsKnockedDown = false;
        if (PlayerController.i != null)
            PlayerController.i.LockRotation = false;

        OnHitComplete?.Invoke();
    }


    public void PlayDeathAnimation(ICombatSystem attacker)
    {
        animator.CrossFade("Death", 0.2f);
    }

    // ==========================================
    // 反弹系统 (由 Weapon.OnTriggerEnter 调用)
    // ==========================================

    
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
        var playerAttack = GetComponent<PlayerAttack>();

        // ① 关闭武器碰撞体
        var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
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

        // ④ 切回待机
        animator.SetFloat("AttackSpeed", 1f);
        var wp = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        string btName = wp?.combatBlendTreeName ?? "Combat Blend Tree";
        animator.Play(btName, 0, 0);
        animator.applyRootMotion = false;

        if (playerAttack != null)
            playerAttack.ForceResetAttackState();

        InAction = false;
        IsRebounding = false;

        yield return null;
        if (PlayerController.i != null)
            PlayerController.i.LockRotation = false;

        yield break;

    Abort:
        animator.SetFloat("AttackSpeed", 1f);
        animator.applyRootMotion = false;

        if (playerAttack != null)
            playerAttack.ForceResetAttackState();

        IsRebounding = false;

        yield return null;
        if (PlayerController.i != null)
            PlayerController.i.LockRotation = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HealthSystem.IsDead) return;
        
        if (other.CompareTag("Hitbox") && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null || attacker.currTarget == null) return;
            if (attacker.currTarget.gameObject != this.gameObject) return;

            var attackerDamage = attacker.GetWeaponDamage();//拿到武器的伤害值

            // 防止同一刀命中同一目标多次
            if (!attacker.RegisterHit(this.gameObject)) return;

            // 格挡中不受伤害（重武器无视格挡）
            if (blockObject != null && blockObject.activeSelf && !attacker.IsUsingHeavyWeapon()) return;

            //暴击处理
            bool isCrit = Random.value < (attacker.CritRate / 100f);
            float finalDamage=isCrit?attackerDamage*attacker.CritDamage:attackerDamage;
            TakeDamage(finalDamage, isCrit);

            // 通知攻击方：成功造成伤害
            attacker.NotifyDamageDealt(this.gameObject);

            // 命中音效 + 血液特效
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, hitPoint, 0.8f);
            BloodEffectManager.SpawnBlood(hitPoint, bloodSplashPrefabs, bloodDecalPrefabs);

            if (!HealthSystem.IsDead)
            {
                string specialReaction = attacker.CurrentSpecialHitReaction;
                attacker.CurrentSpecialHitReaction = null;
                if (!string.IsNullOrEmpty(specialReaction))
                {
                    string suffix = weaponManager?.GetCurrentWeapon()?.WeaponTypeSuffix ?? "Sword";
                    specialReaction = specialReaction + "_" + suffix;
                }
                bool isKnockdown = attacker.IsCurrentAttackKnockdown;
                Debug.Log($"[OnTriggerEnter] 启动 PlayHitReaction, isKnockdown={isKnockdown}, 当前状态 IsKnockedDown={IsKnockedDown}, IsTakingHit={IsTakingHit}, InAction={InAction}");
                StartCoroutine(PlayHitReaction(attacker, specialReaction, isKnockdown));
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
    public void AE_EnableHitbox(string param)
    {
        // 反弹期间禁止动画事件重新开启碰撞体
        if (IsRebounding) return;

        hitTargets.Clear();

        // 解析参数：格式 "武器的碰撞器类型|特殊受击动画名称（需要在动画机中存在该动画，可以为空）"
        // 例如: "Sword|hit_heavy_B" 或 "RightHand|"
        string[] parts = param.Split('|');
        string hitboxName = parts[0];
        string reactionAnim = parts.Length > 1 ? parts[1] : null;

        // 设置特殊受击动画
        if (string.IsNullOrEmpty(reactionAnim))
            CurrentSpecialHitReaction = null;
        else
            CurrentSpecialHitReaction = reactionAnim;

        // 启用对应的碰撞体
        if (System.Enum.TryParse(hitboxName, out AttackHitbox type))
        {
            EnableHitboxByType(type);
        }
    }


    //动画 Event 中调用此方法关闭碰撞
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
    public bool IsUsingHeavyWeapon()
    {
        return WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.isHeavy ?? false;
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
    private void DoBlock()
    {
        if (weaponManager.GetCurrentWeapon() != null && !InAction)
        {
            Debug.Log("已装备武器，触发DoBlock方法");

            animator.CrossFade("PlayerBlock", 0.2f, 3);
        }
        else
        {
            Debug.Log("未装备武器，触发DoBlock方法失败！");
        }
       
    }
    public void EnableBlock()//给block动画调用
    {
        Debug.Log("动画事件：blockEnable");
        PlayerController.i.isMovementEnabled=false;
        InAction=true;
        blockObject.SetActive(true);
    }
    public void DisableBlock()//给block动画调用
    {
        Debug.Log("动画事件：blockDisable");
        PlayerController.i.isMovementEnabled=true;
        InAction=false;
        blockObject.SetActive(false);
    }

    // 递归锁方法
   public bool LockInAction(string owner)
    {
        // 同一个 owner，直接返回 true（不重复上锁）
        if (lockOwner == owner)
        {
            Debug.Log($"[InAction] {owner} 已持有锁，重复调用忽略");
            return true;
        }
        
        // 已被其他 owner 锁定
        if (lockOwner != null)
        {
            Debug.Log($"[InAction] 已被 {lockOwner} 锁定，{owner} 无法获取锁");
            return false;
        }
        
        // 首次获取
        lockOwner = owner;
        Debug.Log($"[InAction] {owner} 获取锁");
        return true;
    }
    
    public bool UnlockInAction(string owner)
    {
        if (lockOwner != owner)
        {
            Debug.LogWarning($"[InAction] {owner} 不是锁的拥有者 ({lockOwner})");
            return false;
        }
        
        lockOwner = null;
        Debug.Log($"[InAction] {owner} 释放锁");
        return true;
    }
    
    public bool IsActionLocked => lockOwner != null;
    public string LockOwner => lockOwner;
    
    public int ActionLayerIndex => actionLayerIndex;

    Transform ICombatSystem.transform => this.transform;
    GameObject ICombatSystem.gameObject => this.gameObject;
}