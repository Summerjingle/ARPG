using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备中间层：武器和护甲的公共基类
/// </summary>
public abstract class EquipmentSO : ItemSO
{
    [Header("装备模型")]
    public GameObject equipmentPrefab; // 装备到角色身上时实例化的模型

    [Header("装备属性（Bonus类，加的是Max值）")]
    public List<Property> propertyList;

    [Header("装备条件（全部AND，全部>=才可装备）")]
    public List<EquipCondition> equipConditions;

    /// <summary> 检查角色是否满足装备条件 </summary>
    public bool CanEquip(PlayerProperty pp)
    {
        if (equipConditions == null || equipConditions.Count == 0)
            return true;

        foreach (var cond in equipConditions)
        {
            if (pp.GetStatValue(cond.statType) < cond.requiredValue)
                return false;
        }
        return true;
    }
}
