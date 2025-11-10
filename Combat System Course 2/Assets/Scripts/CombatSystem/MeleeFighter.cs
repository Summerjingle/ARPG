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
    [SerializeField] private HealthSystem healthSystem;
    public HealthSystem HealthSystem => healthSystem;
    private BoxCollider WeaponCollider;
    private SphereCollider leftHandCollider, rightHandCollider, leftFootCollider, rightFootCollider;
    public Animator animator;
    private Weapon enemyWeapon;




    public AttackStates Attackstate { get; private set; }
    public event Action<MeleeFighter> OnGotHit;//收到打击事件
    public event Action OnHitComplete;//收到打击完成事件
   



    private PlayerProperty playerProperty;
    public bool isPlayer;



    public bool InAction { get; set; } = false;
   
    public bool IsTakingHit { get; private set; } = false;
    public bool InCounter { get; set; } = false;
    private bool docombo;

    private int comboCount = 0;

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
        IEnumerator Attack(MeleeFighter target = null)
        {
        float damage = WeaponEquipmentManager.Instance?.GetWeaponDamage() ?? 5f;
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
                var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
                if (weaponCollider != null) weaponCollider.enabled = true;
                else Debug.LogWarning("武器碰撞器为null，无法启用");
                break;
            default:
                break;
        }
    }
    private bool HasUsableWeapon()
    {
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


    public List<AttackData> Attacks => attacks;
    public bool IsCounterable => Attackstate == AttackStates.Windup && comboCount == 0;
   
}