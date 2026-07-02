using UnityEngine;

/// <summary>
/// 护甲数据
/// </summary>
[CreateAssetMenu(menuName = "Items/Armor", fileName = "New Armor")]
public class ArmorSO : EquipmentSO
{
    public override ItemType itemType => ItemType.Armor;

    [Header("护甲类型")]
    public ArmorType armorType;
}
