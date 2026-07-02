using System;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public WeaponSO itemSO;

    // 以下全部从 WeaponSO 读取，单一数据源
    public string drawWeaponTriggerName => itemSO?.drawWeaponTriggerName;
    public string sheathWeaponTriggerName => itemSO?.sheathWeaponTriggerName;
    public string combatBlendTreeName => itemSO?.combatBlendTreeName ?? "Combat Blend Tree";
    public SheathLocation sheathLocation => itemSO?.sheathLocation ?? SheathLocation.Waist;
    public HandSocket handSocket => itemSO?.handSocket ?? HandSocket.Primary;
    public bool isHeavy => itemSO != null && itemSO.isHeavy;
    public string rollAnim => itemSO?.rollAnim ?? "Roll_Sword";

    public abstract float GetDamage();
    public virtual void Initialize(WeaponSO weaponItem)
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
