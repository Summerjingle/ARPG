using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消耗品数据：可组合多个效果（如同时回HP+回能量）
/// </summary>
[CreateAssetMenu(menuName = "Items/Consumable", fileName = "New Consumable")]
public class ConsumableSO : ItemSO
{
    public override ItemType itemType => ItemType.Consumable;

    [Header("使用效果（Curr类属性，直接加当前值）")]
    public List<Property> effects;

    public override bool IsStackable() => maxStackSize > 1;

    public override void Use(PlayerController user)
    {
        if (effects == null) return;

        PlayerProperty pp = user?.GetComponent<PlayerProperty>();
        if (pp == null) return;

        foreach (Property p in effects)
            pp.AddProperty(p.statType, p.value);
    }
}
