using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemUsageHandler : MonoBehaviour
{
    public static ItemUsageHandler Instance { get; private set; }
    public ArmorEquipmentManager armorManager; 
    public PlayerProperty playerProperty;
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
    }
    private void Start()
    {
        playerProperty=GetComponent<PlayerProperty>();
    }
    [SerializeField] public Transform weapon1Socket; // Weapon1节点的引用

   

    public void UseItem(ItemSO itemSO)
    {
        switch (itemSO.itemType)
        {
            case ItemType.Weapon:
                EquipWeapon(itemSO);
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

    private void EquipWeapon(ItemSO weaponItem)
    {
        // 先移除当前已有的武器
        UnequipWeapon();

        if (weaponItem.weaponPrefab == null)
        {
            Debug.LogError($"武器 {weaponItem.nameOfItem} 的prefab未设置！");
            return;
        }

        GameObject weaponInstance = Instantiate(
            weaponItem.weaponPrefab,
            weapon1Socket.position,
            weapon1Socket.rotation,
            weapon1Socket
        );

        Weapon newWeapon = weaponInstance.GetComponent<Weapon>();
        if (newWeapon == null)
        {
            newWeapon = weaponInstance.GetComponentInChildren<Weapon>();
        }

        // 设置武器的ItemSO引用
        if (newWeapon != null)
        {
            newWeapon.itemSO = weaponItem;
        }

        MeleeFighter fighter = GetComponent<MeleeFighter>();
        if (fighter != null)
        {
            if (newWeapon != null)
            {
                fighter.SetWeapon(newWeapon);
                Debug.Log($"装备武器完成: {weaponItem.nameOfItem}");
            }
            else
            {
                Debug.LogError($"武器预制体 {weaponItem.nameOfItem} 中没有找到Weapon组件");
            }
        }
        else
        {
            Debug.LogError("未找到MeleeFighter组件");
        }
        InventoryUI.Instance.UpdateEquipmentIcon(weaponItem);
    }

    public void UnequipWeapon()
    {
        // 检查当前是否有装备武器
        if (weapon1Socket.childCount > 0)
        {
            // 获取武器物品信息（假设武器子物体上有Weapon组件并保存了ItemSO引用）
            Weapon weapon = weapon1Socket.GetComponentInChildren<Weapon>();
            if (weapon != null && weapon.itemSO != null && InventoryManager.Instance != null)
            {
                // 将武器添加回背包
                InventoryManager.Instance.ReAddItem(weapon.itemSO);
            }
        }

        // 销毁Weapon1节点下的所有武器
        foreach (Transform child in weapon1Socket)
        {
            Destroy(child.gameObject);
        }
        InventoryUI.Instance.ClearEquipmentIcon(ItemType.Weapon);
    }



}