using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeleeFighter))]
public class EnemyFighter : MonoBehaviour
{
    private MeleeFighter baseFighter;
    private Weapon enemyWeapon;

    private void Awake()
    {
        baseFighter = GetComponent<MeleeFighter>();
        enemyWeapon = GetComponentInChildren<Weapon>();
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
            meleeFighter.StartCoroutine(meleeFighter.Attack(target));
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

    // 敌人攻击数据选择 - 可能基于AI策略
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
}