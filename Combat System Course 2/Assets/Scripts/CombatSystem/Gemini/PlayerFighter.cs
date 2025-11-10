using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeleeFighter))]
public class PlayerFighter : MonoBehaviour
{
    private MeleeFighter baseFighter;
    private WeaponEquipmentManager weaponManager;

    private void Awake()
    {
        baseFighter = GetComponent<MeleeFighter>();
        weaponManager = WeaponEquipmentManager.Instance;

        // 确保标记为玩家
        if (baseFighter != null)
        {
            // 暂时保持兼容性
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
        var meleeFighter = GetComponent<MeleeFighter>();
        return !meleeFighter.InAction && PlayerHasUsableWeapon();
    }

    public void PlayerTryToAttack(MeleeFighter target = null)
    {
        var meleeFighter = GetComponent<MeleeFighter>();

        if (PlayerCanAttack())
        {
            meleeFighter.StartCoroutine(meleeFighter.Attack(target));
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
}

