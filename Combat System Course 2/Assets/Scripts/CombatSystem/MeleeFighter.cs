using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum AttackStates { Idle, Windup, Impact, Cooldown }//枚举武器的状态
public class MeleeFighter : MonoBehaviour
{


    [SerializeField] List<AttackData> attacks;
    [SerializeField] List<AttackData> longRangeAttacks;
    [SerializeField] float LongRangeAttackThreshold = 1.5f;
    public Weapon currentWeapon;


    [field: SerializeField] public float MaxHealth { get;  set; } = 25f;
    [field: SerializeField] public float Health { get;  set; } = 25f;

    BoxCollider WeaponCollider;
    SphereCollider leftHandCollider, rightHandCollider, leftFootCollider, rightFootCollider;
    public Animator animator;




    public AttackStates Attackstate { get; private set; }
    public event Action<MeleeFighter> OnGotHit;//收到打击事件
    public event Action OnHitComplete;//收到打击完成事件
    public event Action OnHealthChanged;//血量变动事件


    public event Action<MeleeFighter> OnDeath;
    public event Action OnDeathComplete;//死亡完成事件
    private PlayerProperty playerProperty;
    public bool isPlayer;



    public bool InAction { get; private set; } = false;
    public bool IsDead { get; private set; } = false;
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    private bool docombo;

    private int comboCount = 0;

    public void Awake()
    {
        animator = GetComponent<Animator>();
        currentWeapon = GetComponentInChildren<Weapon>();
        Health = MaxHealth; // 初始化满血
        playerProperty = GetComponent<PlayerProperty>();
        isPlayer = playerProperty != null;
    }

    private void Start()
    {
        // 初始化身体部位的碰撞器（这些与武器无关，应该始终初始化）
        InitializeBodyColliders();

        // 禁用所有碰撞器
        DisableAllHitboxes();


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

        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
            Debug.Log($"初始化武器碰撞器: {(WeaponCollider != null ? WeaponCollider.name : "null")}");
        }
    }
    public void TryToAttack(MeleeFighter target = null)//尝试进行攻击，此方法是 被调用的
    {

        if (!InAction && currentWeapon != null)//如果不在攻击
        {
            StartCoroutine(Attack(target));//调用攻击，进入攻击状态
        }
        else if (Attackstate == AttackStates.Impact || Attackstate == AttackStates.Cooldown)//如果已经在攻击
        {
            docombo = true;//进入连击
        }
    }

    public MeleeFighter currTarget;
        IEnumerator Attack(MeleeFighter target = null)
        {
            float damage = currentWeapon != null ? currentWeapon.GetDamage() : 5f;
            InAction = true;
            currTarget = target;
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
                if (distance > LongRangeAttackThreshold && longRangeAttacks.Count > 0)//如果敌人不在近距离攻击范围内
                {
                    attack = longRangeAttacks[0];//替换为远距离攻击
                }
                if (attack.MoveToTarget)//如果施展的攻击是“朝着目标攻击”的类型
                {
                    if (distance < attack.MaxMoveDistance)//计算敌人距离是否超出最大攻击范围
                        targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;//未超出最大攻击范围
                    else
                        targetPos = startPos + attackDir * attack.MaxMoveDistance;//超出攻击范围，按最大值计算
                }
            }

            animator.CrossFade(attack.AttackName, 0.2f);
            yield return null;//等待一帧
            var animstate = animator.GetNextAnimatorStateInfo(1);

            float timer = 0f;
            while (timer <= animstate.length)
            {
                if (IsTakingHit) break;
                timer += Time.deltaTime;
                float normalizedTime = timer / animstate.length;

            //使攻击者执行攻击时朝目标移动
            if (target != null && attack.MoveToTarget)
            {
                float percTime = (normalizedTime - attack.MoveStartTime) / (attack.MoveEndTime - attack.MoveStartTime);
                Vector3 desiredPosition = Vector3.Lerp(startPos, targetPos, percTime);
                Vector3 moveDelta = desiredPosition - transform.position;

                // 使用CharacterController进行移动（有碰撞检测）
                CharacterController controller = GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.Move(moveDelta);
                }
                else
                {
                    // 备用方案
                    transform.position = desiredPosition;
                }
            }

            //使玩家转向攻击的方向
            if (attackDir != null)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(attackDir), 500f * Time.deltaTime);
                }
                if (Attackstate == AttackStates.Windup)//挥起
                {
                    if (InCounter) break;
                    if (normalizedTime >= attack.ImpactStartTime)
                    {
                        Attackstate = AttackStates.Impact;
                        EnableHitbox(attack);
                    }
                }
                else if (Attackstate == AttackStates.Impact)//砸下
                {
                    if (normalizedTime >= attack.ImpactEndTime)
                    {
                        Attackstate = AttackStates.Cooldown;
                        DisableAllHitboxes();
                    }
                }
                else if (Attackstate == AttackStates.Cooldown)//冷却
                {
                    if (docombo)//在冷却时再次点击攻击，进入连击状态
                    {
                        docombo = false;
                        comboCount = (comboCount + 1) % attacks.Count;
                        StartCoroutine(Attack(target));
                        yield break;
                    }
                }
                yield return null;
            }
            //等待动画完成
            Attackstate = AttackStates.Idle;
            comboCount = 0;
            InAction = false;
            currTarget = null;
        }

    private void OnTriggerEnter(Collider other)
    {
        // 如果已经死亡，不处理任何碰撞
        if (IsDead) return;

        if (other.tag == "Hitbox" && !IsTakingHit && !InCounter)
        {
            var attacker = other.GetComponentInParent<MeleeFighter>();
            Debug.Log($"攻击者: {attacker?.gameObject.name}, 当前目标: {attacker?.currTarget?.gameObject.name}, 攻击状态: {attacker?.Attackstate}");

            // 修复的目标检查逻辑
            if (attacker == null || attacker.currTarget == null)
            {
                Debug.Log("攻击者或目标为空，返回");
                return;
            }

            // 关键修复：比较GameObject而不是组件引用
            if (attacker.currTarget.gameObject != this.gameObject)
            {
                Debug.Log($"攻击目标不匹配: 攻击者目标={attacker.currTarget.gameObject.name}, 自身={this.gameObject.name}，返回");
                return;
            }

            Debug.Log($"即将造成伤害: {attacker.currentWeapon.GetDamage()}");

            TakeDamage(attacker.currentWeapon.GetDamage());
            OnGotHit?.Invoke(attacker);

            if (Health > 0)
            {
                Debug.Log("受伤但未死亡");
                StartCoroutine(PlayHitReaction(attacker));
            }
            else
            {
                PlayDeathAnimation(attacker);
                Invoke("SpawnItem", 1f);
            }
        }
    }
    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        int currentArmor = 0;
        if (isPlayer && playerProperty != null)
        {
            currentArmor = playerProperty.armorValue;
        }

        // 计算护甲减免 (每点护甲减少0.5%伤害)
        float damageReduction = currentArmor * 0.005f; // 0.5% per armor point
        float reducedDamage = damage * (1 - Mathf.Clamp(damageReduction, 0, 0.8f)); // 最多减少80%伤害

        Debug.Log($"原始伤害: {damage}, 护甲值: {currentArmor}, 护甲减免: {damageReduction * 100}%, 实际伤害: {reducedDamage}");

        Health = Math.Clamp(Health - reducedDamage, 0, MaxHealth);


        if (isPlayer && playerProperty != null)
        {
            playerProperty.hpValue = Mathf.RoundToInt(Health);
        }
        CheckDeathState();
        OnHealthChanged?.Invoke();
    }
    private void CheckDeathState()
    {
        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            OnDeath?.Invoke(this);

            // 如果是狼，确保通过WolfController处理死亡
            if (!isPlayer)
            {
                var wolfController = GetComponent<WolfController>();
                if (wolfController != null && !wolfController.IsDead)
                {
                    wolfController.HandleWolfDeath();
                }
                else
                {
                    // 备用方案：直接播放死亡动画
                    var enemyController = GetComponent<EnemyController>();
                    if (enemyController != null)
                    {
                        enemyController.ChangerState(EnemyStates.Dead);
                    }
                }
            }
        }
    }
    public void RestoreHealth(int amount)//供PlayerProperty调用
    {
        Health = Mathf.Clamp(Health + amount, 0, MaxHealth);

        if (isPlayer && playerProperty != null)
        {
            playerProperty.hpValue = Mathf.RoundToInt(Health);
        }
        OnHealthChanged?.Invoke();
    }
    private void SpawnItem()
    {
        int count = 1;
        for (int i = 0; i < count; i++)
        {
            ItemSO item = ItemDBManager.Instance.GetRandomDropAllowedItem();
            if (item != null)
            {

                Vector3 dropPosition = transform.position + Vector3.up * 0.5f;
                GameObject droppedItem = GameObject.Instantiate(item.interactablePrefab, dropPosition, Quaternion.identity);
                PickableObject po = droppedItem.GetComponent<PickableObject>();
                if (po == null)
                {
                    // 如果预制体没有PickableObject，就添加一个
                    po = droppedItem.AddComponent<PickableObject>();
                }

                // 设置itemSO引用
                po.itemSO = item;
            }

        }
    }

    public void PlayDeathAnimation(MeleeFighter attacker)
    {
        animator.CrossFade("Death", 0.2f);
        OnDeathComplete?.Invoke();
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
    private void EnableHitbox(AttackData attack)
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
                if (WeaponCollider != null) WeaponCollider.enabled = true;
                else Debug.LogWarning("武器碰撞器为null，无法启用");
                break;
            default:
                break;
        }
    }

    private void DisableAllHitboxes()//游戏开始时默认禁用所有碰撞器
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
    public void SetWeapon(Weapon weapon, ItemSO weaponItem = null)
    {
        // 先停止所有正在进行的攻击
        StopAllCoroutines();

        // 重置状态
        InAction = false;
        Attackstate = AttackStates.Idle;
        comboCount = 0;

        // 禁用所有碰撞器
        DisableAllHitboxes();

        // 设置新武器
        currentWeapon = weapon;

        if (currentWeapon != null)
        {
            // 初始化武器碰撞器
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
            Debug.Log($"设置武器: {currentWeapon.name}, 碰撞器: {(WeaponCollider != null ? WeaponCollider.name : "null")}");

            // 确保身体碰撞器已初始化（可能在Start之前调用SetWeapon）
            if (leftHandCollider == null)
            {
                InitializeBodyColliders();
            }
        }
        else
        {
            Debug.LogWarning("设置的武器为null");
            WeaponCollider = null;
        }
    }
    public void RefreshWeapon()
    {
        // 如果需要，这里可以保持向后兼容
        currentWeapon = GetComponentInChildren<Weapon>();
        if (currentWeapon != null)
        {
            WeaponCollider = currentWeapon.GetComponentInChildren<BoxCollider>();
        }
        else
        {
            WeaponCollider = null;
        }
    }


    public List<AttackData> Attacks => attacks;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;
   
}