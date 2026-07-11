using UnityEngine;

/// <summary>
/// 武器数据
/// </summary>
[CreateAssetMenu(menuName = "Items/Weapon", fileName = "New Weapon")]
public class WeaponSO : EquipmentSO
{
    public override ItemType itemType => ItemType.Weapon;

    [Header("动画触发")]
    public string drawWeaponTriggerName;
    public string sheathWeaponTriggerName;
    public string combatBlendTreeName = "Combat Blend Tree";

    [Header("挂点选择")]
    public SheathLocation sheathLocation = SheathLocation.Waist;
    public HandSocket handSocket = HandSocket.Primary;

    [Header("武器类型")]
    public bool isHeavy = false;
    public WeaponCategory weaponCategory = WeaponCategory.Sword;

    [Header("翻滚")]
    public string rollAnim = "Esc_Roll_Front_Root";

    [Header("武器伤害")]
    public float baseDamage = 10f;
    
}

// 这些枚举供 Weapon MonoBehaviour 和 WeaponEquipmentManager 使用
public enum SheathLocation { Waist, Back }
public enum HandSocket { Primary, Secondary }
public enum WeaponCategory { Sword, GreatSword }
