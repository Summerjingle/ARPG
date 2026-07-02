using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ItemUsageHandler : MonoBehaviour
{
    public static ItemUsageHandler Instance { get; private set; }
    public ArmorEquipmentManager armorManager;
    public PlayerProperty playerProperty;
    [SerializeField] public Transform weapon1Socket; // Weapon1的节点位置
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
        if (itemSO is EquipmentSO equipment)
        {
            // 检查装备条件
            if (!equipment.CanEquip(playerProperty))
            {
                string failReasons = "";
                if (equipment.equipConditions != null)
                {
                    foreach (var cond in equipment.equipConditions)
                    {
                        int currentVal = playerProperty.GetStatValue(cond.statType);
                        if (currentVal < cond.requiredValue)
                            failReasons += $"{cond.statType}不足(需要{cond.requiredValue},当前{currentVal}) ";
                    }
                }
                MessageUI.Instance?.Show($"无法装备: {failReasons}");
                return;
            }

            if (itemSO is WeaponSO)
                weaponManager.EquipWeapon(itemSO);
            else if (itemSO is ArmorSO)
                armorManager.EquipArmor(itemSO);
        }
        else if (itemSO is ConsumableSO)
        {
            playerProperty.UseDrag(itemSO);
            Debug.Log($"使用消耗品: {itemSO.nameOfItem}");
        }
        else if (itemSO.itemType == ItemType.QuestRelated)
        {
            Debug.Log("任务道具无法直接使用");
        }
        else
        {
            Debug.LogWarning($"未知物品类型: {itemSO.itemType}");
        }
    }
}
