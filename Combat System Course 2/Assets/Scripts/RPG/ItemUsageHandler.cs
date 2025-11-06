using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUsageHandler : MonoBehaviour
{
    public static ItemUsageHandler Instance { get; private set; }
    public ArmorEquipmentManager armorManager; 
    public PlayerProperty playerProperty;
    [SerializeField] public Transform weapon1Socket; // Weapon1节点的引用
    public WeaponEquipmentManager weaponManager;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        weaponManager = GetComponent<WeaponEquipmentManager>();
        if (weaponManager == null)
        {
            weaponManager = gameObject.AddComponent<WeaponEquipmentManager>();
        }
    }
    private void Start()
    {
        playerProperty=GetComponent<PlayerProperty>();
    }
    

   

    public void UseItem(ItemSO itemSO)
    {
        switch (itemSO.itemType)
        {
            case ItemType.Weapon:
                weaponManager.EquipWeapon(itemSO);
                break;

            case ItemType.Consumable:
                playerProperty.UseDrag(itemSO);
                Debug.Log($"使用消耗品: {itemSO.nameOfItem}");
                break;
            case ItemType.Armor:
                armorManager.EquipArmor(itemSO);
                break;
            case ItemType.QuestRelated:
                Debug.Log("任务道具无法直接使用");
                
                break;
            default:
                Debug.LogWarning($"未知的物品类型: {itemSO.itemType}");
                break;
        }
    }
}