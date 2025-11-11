using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeleeFighter))]
public class EnemyFighter : MonoBehaviour, ICombatSystem
{
    private MeleeFighter baseFighter;
    private Weapon enemyWeapon;
    private NavMeshAgent navAgent;
    private EnemyController enemyController;
    private WolfController wolfController;

    private float decisionCooldown;
    private MeleeFighter currentTarget;
    private Vector3 lastKnownPlayerPosition;

    private void Awake()
    {
        baseFighter = GetComponent<MeleeFighter>();
        enemyWeapon = GetComponentInChildren<Weapon>();
        navAgent = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();
        wolfController = GetComponent<WolfController>();
    }
  
  
    
  

    public float GetWeaponDamage()
    {
        return enemyWeapon?.GetDamage() ?? 1f;
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
        var meleeFighter = GetComponent<MeleeFighter>();
        return !meleeFighter.InAction && EnemyHasUsableWeapon();
    }

    public void EnemyTryToAttack(MeleeFighter target = null)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        if (EnemyCanAttack())
        {
            StartCoroutine(ExecuteEnemyAttack(target, meleeFighter.comboCount));
        }
        else if (meleeFighter.Attackstate == AttackStates.Impact ||
                 meleeFighter.Attackstate == AttackStates.Cooldown)
        {
            Debug.Log($"敌人({gameObject.name})进入连击状态，由AttackState控制");
        }
    }

    public Vector3 CalculateEnemyAttackPosition(MeleeFighter target, AttackData attack, Vector3 attackDir, Vector3 startPos)
    {
        // 敌人通常不需要复杂的移动计算，NavAgent会处理
        // 返回当前位置，让NavAgent控制移动
        return startPos;
    }

    // 敌人攻击方向计算 - 面向目标即可
    public Vector3 CalculateEnemyAttackDirection(MeleeFighter target)
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
    public AttackData SelectEnemyAttack(MeleeFighter target, List<AttackData> attacks, List<AttackData> longRangeAttacks, int comboCount)
    {
        // 敌人可能基于距离、状态等选择攻击
        // 目前保持简单逻辑
        return attacks[comboCount % attacks.Count];
    }

    // 敌人特定的攻击准备逻辑
    public void PrepareEnemyAttack(MeleeFighter target)
    {
        var meleeFighter = GetComponent<MeleeFighter>();
        var enemyController = GetComponent<EnemyController>();

        if (enemyController != null)
        {
            // 敌人可能需要在攻击前停止导航
            enemyController.NavAgent.isStopped = true;
        }

        // 设置攻击目标
        meleeFighter.currTarget = target;
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

    // 敌人专属状态管理
    public void UpdateEnemyAttackState(float normalizedTime, AttackData attack)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        if (meleeFighter.Attackstate == AttackStates.Windup)
        {
            if (normalizedTime >= attack.ImpactStartTime)
            {
                meleeFighter.Attackstate = AttackStates.Impact;
                Debug.Log($"敌人({gameObject.name})攻击进入Impact状态");
                EnableEnemyHitbox(attack);
            }
        }
        else if (meleeFighter.Attackstate == AttackStates.Impact)
        {
            if (normalizedTime >= attack.ImpactEndTime)
            {
                meleeFighter.Attackstate = AttackStates.Cooldown;
                Debug.Log($"敌人({gameObject.name})攻击进入Cooldown状态");
                DisableEnemyHitboxes();
            }
        }
    }

    // 敌人状态重置
    public void ResetEnemyAttackState()
    {
        var meleeFighter = GetComponent<MeleeFighter>();
        meleeFighter.Attackstate = AttackStates.Idle;
        meleeFighter.InAction = false;

        // 敌人可能不需要重置comboCount，因为由AI控制
        var enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.NavAgent.isStopped = false;
        }
        Debug.Log($"敌人({gameObject.name})攻击状态重置");
    }

    // 敌人连击状态检查（由AttackState控制，这里只是接口）
    public bool CheckEnemyComboCondition()
    {
        // 敌人的连击由AttackState状态机控制
        return false; // 返回false，让AttackState处理
    }

    // 敌人专属Hitbox启用
    public void EnableEnemyHitbox(AttackData attack)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        switch (attack.HitboxToUse)
        {
            case AttackHitbox.LeftHand:
                if (meleeFighter.leftHandCollider != null)
                {
                    meleeFighter.leftHandCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})左手Hitbox");
                }
                break;
            case AttackHitbox.RightHand:
                if (meleeFighter.rightHandCollider != null)
                {
                    meleeFighter.rightHandCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})右手Hitbox");
                }
                break;
            case AttackHitbox.LeftFoot:
                if (meleeFighter.leftFootCollider != null)
                {
                    meleeFighter.leftFootCollider.enabled = true;
                    Debug.Log($"启用敌人({gameObject.name})左脚Hitbox");
                }
                break;
            case AttackHitbox.RightFoot:
                if (meleeFighter.rightFootCollider != null)
                {
                    meleeFighter.rightFootCollider.enabled = true;
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
    public IEnumerator ExecuteEnemyAttack(MeleeFighter target, int comboCount)
    {
        // 1. 准备攻击
        PrepareEnemyAttack(target);

        var baseFighter = GetComponent<MeleeFighter>();
        baseFighter.InAction = true;
        baseFighter.currTarget = target;
        baseFighter.Attackstate = AttackStates.Windup;

        // 2. 获取攻击数据
        var attack = SelectEnemyAttack(target, baseFighter.Attacks, baseFighter.longRangeAttacks, comboCount);
        Vector3 attackDir = CalculateEnemyAttackDirection(target);
        Vector3 startPos = transform.position;
        Vector3 targetPos = CalculateEnemyAttackPosition(target, attack, attackDir, startPos);

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

            // 敌人通常不需要手动移动，由NavAgent处理
            // 但保留转向控制
            if (attackDir != Vector3.zero)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(attackDir),
                    500f * Time.deltaTime);
            }

            // 5. 状态管理
            UpdateEnemyAttackState(normalizedTime, attack);

            // 6. 敌人连击由AI控制，这里不处理连击
            // 原有的连击逻辑被移除，因为敌人连击在AttackState中处理

            yield return null;
        }

        // 7. 攻击结束
        ResetEnemyAttackState();
        FinishEnemyAttack();

        baseFighter.currTarget = null;
    }

    #region ICombatSystem接口方法实现
    public bool CanAttack() => EnemyCanAttack();//能否进行攻击
    public void TryToAttack(MeleeFighter target = null) => EnemyTryToAttack(target);//尝试攻击
    
    public bool HasUsableWeapon() => EnemyHasUsableWeapon();//检查是否有武器
    public AttackData SelectAttack(MeleeFighter target, int comboCount)
        => SelectEnemyAttack(target, baseFighter.Attacks, baseFighter.longRangeAttacks, comboCount);//选择攻击数据
    public Vector3 CalculateAttackDirection(MeleeFighter target) => CalculateEnemyAttackDirection(target);//计算攻击时的朝向
    public Vector3 CalculateAttackPosition(MeleeFighter target, AttackData attack, Vector3 attackDir, Vector3 startPos)
        => CalculateEnemyAttackPosition(target, attack, attackDir, startPos);//计算攻击时移动到的位置
    public void UpdateAttackState(float normalizedTime, AttackData attack) => UpdateEnemyAttackState(normalizedTime, attack);//更新攻击数据
    public void ResetAttackState() => ResetEnemyAttackState();//重置攻击数据
    public void EnableHitbox(AttackData attack) => EnableEnemyHitbox(attack);//启用碰撞体
    public void DisableHitboxes() => DisableEnemyHitboxes();//禁用碰撞体
    public void PrepareAttack(MeleeFighter target) => PrepareEnemyAttack(target);//攻击
    public void FinishAttack() => FinishEnemyAttack();//攻击完成
    public bool CheckComboCondition() => CheckEnemyComboCondition();//查看连招状态

    public IEnumerator ExecuteAttack(MeleeFighter target, int comboCount)
    {
        yield return ExecuteEnemyAttack(target, comboCount);
    }
    #endregion
}