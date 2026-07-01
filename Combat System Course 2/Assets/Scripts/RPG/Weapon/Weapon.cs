using System;
using UnityEngine;

// ========== 挂点枚举 ==========
// 新增步骤：1.枚举加一项  2.去 WeaponEquipmentManager 的 lookup 方法加一行
public enum SheathLocation { Waist, Back }
public enum HandSocket { Primary, Secondary }

public abstract class Weapon : MonoBehaviour
{
    public ItemSO itemSO;

    [Header("动画触发器")]
    public String drawWeaponTriggerName;
    public String sheathWeaponTriggerName;
    public string combatBlendTreeName = "Combat Blend Tree";

    [Header("挂点选择")]
    public SheathLocation sheathLocation = SheathLocation.Waist;
    public HandSocket handSocket = HandSocket.Primary;

    [Header("武器类型")]
    public bool isHeavy = false; // 重型武器（大剑等），拔出时玩家减速
    public abstract float GetDamage();
    public virtual void Initialize(ItemSO weaponItem)
    {
        itemSO = weaponItem;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle"))
            return;

        // 重武器无视战斗单位的碰撞（格挡盾牌等），直接穿透
        if (isHeavy && other.GetComponentInParent<ICombatSystem>() != null)
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        var playerFighter = GetComponentInParent<PlayerFighterNew>();
        if (playerFighter != null)
        {
            playerFighter.OnWeaponRebound(hitPoint);
            return;
        }

        var enemyFighter = GetComponentInParent<EnemyFighter>();
        if (enemyFighter != null)
        {
            enemyFighter.OnWeaponRebound(hitPoint);
        }
    }
}