using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class ItemSO :ScriptableObject
{
    public string nameOfItem;

    public ItemType itemType;
    public ArmorType armorType;
    public string description;
    public List<Property> propertyList;
    public Sprite icon;
    public GameObject interactablePrefab; // 用于掉落和拾取的预制体（所有物品）
    public GameObject weaponPrefab;
    public bool canDrop = true;

}
public enum ItemType
{
    Weapon,
    Consumable,
    Armor
}
public enum ArmorType
{
    Helmet,     // 头盔
    Chestplate, // 胸甲
    Gauntlets,  // 护手
    Leggings,   // 护腿
    Boots,// 靴子
    NotArmor
}

[Serializable]
public class Property
{
    public PropertyType propertyType;
    public int value;  
    public Property()
    {

    }
    public Property(PropertyType propertyType, int value)
    {
        this.propertyType = propertyType;
        this.value = value;
    }
}
public enum PropertyType
{
    HPValue,
    EnergyValue,
    AttackValue,
    DefenseValue
}
