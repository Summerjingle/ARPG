using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有物品的抽象基类（纯数据容器）
/// </summary>
public abstract class ItemSO : ScriptableObject
{
    [Header("基础信息")]
    public string nameOfItem;
    public string description;
    public Sprite icon;
    public GameObject interactablePrefab; // 场景中的拾取预制体

    [Header("堆叠")]
    public int maxStackSize = 1;
    public int amount = 1;

    [Header("稀有度")]
    public Rarity rarity;

    /// <summary> 物品分类（子类强制实现）</summary>
    public abstract ItemType itemType { get; }

    public virtual bool IsStackable()
    {
        return itemType == ItemType.Consumable && maxStackSize > 1;
    }

    public virtual bool CanStackWith(ItemSO otherItem)
    {
        return this.nameOfItem == otherItem.nameOfItem &&
               this.itemType == otherItem.itemType &&
               this.IsStackable() && otherItem.IsStackable();
    }

    /// <summary> 使用物品（子类重写）</summary>
    public virtual void Use(PlayerController user) { }
}

// ==================== 枚举 ====================

public enum ItemType
{
    Weapon,
    Consumable,
    Armor,
    QuestRelated
}

public enum ArmorType
{
    Helmet,
    Chestplate,
    Gauntlets,
    Leggings,
    Boots,
    NotArmor
}

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

// ==================== Property ====================

[Serializable]
public class Property
{
    public StatType statType;
    public int value;

    public Property() { }

    public Property(StatType statType, int value)
    {
        this.statType = statType;
        this.value = value;
    }
}
