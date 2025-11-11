using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeleeFighter))]
public class PlayerFighter : MonoBehaviour, ICombatSystem
{
    private MeleeFighter baseFighter;
    private WeaponEquipmentManager weaponManager;
    private PlayerProperty playerProperty;

    private bool attackInput;
    private float lastAttackTime;
    private const float INPUT_BUFFER_TIME = 0.2f;
    private MeleeFighter currentTarget;

    private void Awake()
    {
        baseFighter = GetComponent<MeleeFighter>();
        weaponManager = WeaponEquipmentManager.Instance;
        playerProperty = GetComponent<PlayerProperty>();
    }
    private void Update()
    {
        HandlePlayerInput();
        UpdateTarget();
    }

    private void HandlePlayerInput()
    {
        // 检测攻击输入
        if (Input.GetMouseButtonDown(0) && !IsUIActive())
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
    private MeleeFighter FindNearestTarget()
    {
        var enemies = FindObjectsOfType<EnemyFighter>();
        MeleeFighter nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            var fighter = enemy.GetComponent<MeleeFighter>();
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
    public MeleeFighter GetCurrentTarget()
    {
        return currentTarget;
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
        var meleeFighter = GetComponent<MeleeFighter>();
        return !meleeFighter.InAction && PlayerHasUsableWeapon();
    }


    public void PlayerTryToAttack(MeleeFighter target = null)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        if (PlayerCanAttack())
        {
            StartCoroutine(ExecutePlayerAttack(target, meleeFighter.comboCount));
        }
        else if (meleeFighter.Attackstate == AttackStates.Impact ||
                 meleeFighter.Attackstate == AttackStates.Cooldown)
        {
            meleeFighter.docombo = true;
        }
    }


    public Vector3 CalculatePlayerAttackPosition(MeleeFighter target, AttackData attack, Vector3 attackDir, Vector3 startPos)
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


    public Vector3 CalculatePlayerAttackDirection(MeleeFighter target)
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
    public AttackData SelectPlayerAttack(MeleeFighter target, List<AttackData> attacks, List<AttackData> longRangeAttacks, int comboCount, float longRangeThreshold)
    {
        var meleeFighter = GetComponent<MeleeFighter>();
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
    public void PreparePlayerAttack(MeleeFighter target)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        // 玩家可能需要重置某些状态
        meleeFighter.docombo = false;

        // 设置攻击目标
        meleeFighter.currTarget = target;

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
        var meleeFighter = GetComponent<MeleeFighter>();

        if (meleeFighter.Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                meleeFighter.Attackstate = AttackStates.Impact;
                Debug.Log("玩家攻击进入Impact状态");

                EnablePlayerHitbox(attack);
            }
        }
        else if (meleeFighter.Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                meleeFighter.Attackstate = AttackStates.Cooldown;
                Debug.Log("玩家攻击进入Cooldown状态");
                DisablePlayerHitboxes();
            }
        }
    }


    // 玩家状态重置
    public void ResetPlayerAttackState()
    {
        var meleeFighter = GetComponent<MeleeFighter>();
        meleeFighter.Attackstate = AttackStates.Idle;
        meleeFighter.InAction = false;
        meleeFighter.comboCount = 0;
        meleeFighter.docombo = false;
        Debug.Log("玩家攻击状态重置");
    }


    // 玩家连击状态检查
    public bool CheckPlayerComboCondition()
    {
        var meleeFighter = GetComponent<MeleeFighter>();
        return meleeFighter.docombo &&
               (meleeFighter.Attackstate == AttackStates.Impact ||
                meleeFighter.Attackstate == AttackStates.Cooldown);
    }


    // 玩家专属Hitbox启用
    public void EnablePlayerHitbox(AttackData attack)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (meleeFighter.leftHandCollider != null)
                {
                    meleeFighter.leftHandCollider.enabled = true;
                    Debug.Log("启用玩家左手Hitbox");
                }
                break;
            case AttackHitbox.RightHand:
                if (meleeFighter.rightHandCollider != null)
                {
                    meleeFighter.rightHandCollider.enabled = true;
                    Debug.Log("启用玩家右手Hitbox");
                }
                break;
            case AttackHitbox.LeftFoot:
                if (meleeFighter.leftFootCollider != null)
                {
                    meleeFighter.leftFootCollider.enabled = true;
                    Debug.Log("启用玩家左脚Hitbox");
                }
                break;
            case AttackHitbox.RightFoot:
                if (meleeFighter.rightFootCollider != null)
                {
                    meleeFighter.rightFootCollider.enabled = true;
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
        var meleeFighter = GetComponent<MeleeFighter>();

        // 禁用所有身体部位Hitbox
        if (meleeFighter.leftHandCollider != null)
            meleeFighter.leftHandCollider.enabled = false;
        if (meleeFighter.rightHandCollider != null)
            meleeFighter.rightHandCollider.enabled = false;
        if (meleeFighter.leftFootCollider != null)
            meleeFighter.leftFootCollider.enabled = false;
        if (meleeFighter.rightFootCollider != null)
            meleeFighter.rightFootCollider.enabled = false;

        // 禁用武器Hitbox
        var weaponCollider = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.GetComponentInChildren<BoxCollider>();
        if (weaponCollider != null)
            weaponCollider.enabled = false;

        Debug.Log("禁用所有玩家Hitbox");
    }

    public IEnumerator ExecutePlayerAttack(MeleeFighter target, int comboCount)
    {
        // 1. 准备攻击
        PreparePlayerAttack(target);

        var baseFighter = GetComponent<MeleeFighter>();
        baseFighter.InAction = true;
        baseFighter.currTarget = target;
        baseFighter.Attackstate = AttackStates.Windup;

        // 2. 获取攻击数据
        var attack = SelectPlayerAttack(target, baseFighter.Attacks, baseFighter.longRangeAttacks, comboCount, baseFighter.LongRangeAttackThreshold);
        Vector3 attackDir = CalculatePlayerAttackDirection(target);
        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculatePlayerAttackPosition(target, attack, attackDir, startPos);

        // 3. 播放动画
        baseFighter.animator.CrossFade(attack.AttackName, 0.2f);
        yield return null;
        var animstate = baseFighter.animator.GetNextAnimatorStateInfo(1);

        // 4. 攻击执行循环
        float timer = 0f;
        while (timer <= animstate.length)
        {
            if (baseFighter.IsTakingHit) break;

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
                baseFighter.docombo = false;
                int newComboCount = (comboCount + 1) % baseFighter.Attacks.Count;
                StartCoroutine(ExecutePlayerAttack(target, newComboCount));
                yield break;
            }

            yield return null;
        }

        // 7. 攻击结束
        ResetPlayerAttackState();
        FinishPlayerAttack();

        baseFighter.currTarget = null;
    }




    #region ICombatSystem接口方法实现接口方法实现

    public bool HasUsableWeapon() => PlayerHasUsableWeapon();//HasUsebleWeapon接口实现
    public bool CanAttack() => PlayerCanAttack();//CanAttack接口实现
    public void TryToAttack(MeleeFighter target = null) => PlayerTryToAttack(target);//TryAttack接口实现
    public Vector3 CalculateAttackPosition(MeleeFighter target, AttackData attack, Vector3 attackDir, Vector3 startPos)
      => CalculatePlayerAttackPosition(target, attack, attackDir, startPos);//CalculateAttackPosition接口实现
    public Vector3 CalculateAttackDirection(MeleeFighter target) => CalculatePlayerAttackDirection(target);//CalculateAttackDirection接口实现
    public void PrepareAttack(MeleeFighter target) => PreparePlayerAttack(target);// PrepareAttack接口实现
    public void FinishAttack() => FinishPlayerAttack();
    public void UpdateAttackState(float normalizedTime, AttackData attack) => UpdatePlayerAttackState(normalizedTime, attack);
    public void ResetAttackState() => ResetPlayerAttackState();
    public bool CheckComboCondition() => CheckPlayerComboCondition();//CheckComboCondition接口实现
    public void EnableHitbox(AttackData attack) => EnablePlayerHitbox(attack);
    public void DisableHitboxes() => DisablePlayerHitboxes();
    public AttackData SelectAttack(MeleeFighter target, int comboCount)
    {
        return SelectPlayerAttack(target, baseFighter.Attacks, baseFighter.longRangeAttacks, comboCount, baseFighter.LongRangeAttackThreshold);
    }
    public IEnumerator ExecuteAttack(MeleeFighter target, int comboCount)
    {
        yield return ExecutePlayerAttack(target, comboCount);
    }
    #endregion
}

