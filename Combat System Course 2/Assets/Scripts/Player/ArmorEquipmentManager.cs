using UnityEngine;
using System.Collections.Generic;

public class ArmorEquipmentManager : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSocket
    {
        public ArmorType armorType; // 护甲类型

        [Header("单部位装备配置")]
        public Transform socketTransform; // 单部位装备使用

        [Header("对称部位装备配置")]
        public bool isSymmetric = false;  // 是否为对称装备
        public Transform leftSocket;      // 左侧挂载点（isSymmetric为true时使用）
        public Transform rightSocket;     // 右侧挂载点（isSymmetric为true时使用）

        [HideInInspector] public GameObject currentEquipment; // 单部位装备实例
        [HideInInspector] public GameObject leftEquipment;    // 对称部位左装备实例
        [HideInInspector] public GameObject rightEquipment;   // 对称部位右装备实例
    }

    [SerializeField] private EquipmentSocket[] equipmentSockets;
    private PlayerProperty playerProperty;

    private void Awake() => playerProperty = GetComponent<PlayerProperty>();

    // 装备护甲（暴露给外部调用的唯一接口）
    public void EquipArmor(ItemSO armorItem)
    {
        EquipmentSocket targetSocket = GetSocketByType(armorItem.armorType);
        if (targetSocket == null) return;

        // 根据装备类型选择挂载逻辑
        if (targetSocket.isSymmetric)
        {
            EquipSymmetricArmor(armorItem, targetSocket);
        }
        else
        {
            EquipSingleArmor(armorItem, targetSocket);
        }

        InventoryUI.Instance.UpdateEquipmentIcon(armorItem);
    }

    // 根据护甲类型找到对应的挂载点
    public EquipmentSocket GetSocketByType(ArmorType armorType)
    {
        foreach (var socket in equipmentSockets)
        {
            if (socket.armorType == armorType) return socket;
        }
        Debug.LogError($"未配置 {armorType} 类型的挂载点！");
        return null;
    }

    // 卸下指定类型的护甲
    public  void UnequipArmor(EquipmentSocket socket)
    {
        // 先获取当前装备的ItemSO（可能为null）
        ItemSO equippedItem = GetEquippedItem(socket.armorType);

        // 根据装备类型选择卸载逻辑
        if (socket.isSymmetric)
        {
            UnequipSymmetricArmor(socket);
        }
        else
        {
            UnequipSingleArmor(socket);
        }

        // 清除UI图标（无论是否有装备都要执行）
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ClearEquipmentIcon(ItemType.Armor, socket.armorType);
        }

        // 只有确实有装备时才添加回背包
        if (equippedItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(equippedItem);
        }
    }

    // 装备单部位护甲（头盔、胸甲等）
    private void EquipSingleArmor(ItemSO armorItem, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.socketTransform == null)
        {
            Debug.LogError($"单部位装备 {armorItem.armorType} 的挂载点未设置！");
            return;
        }

        socket.currentEquipment = InstantiateArmorModel(armorItem, socket.socketTransform);
        ApplyArmorProperties(armorItem);
    }

    // 装备对称部位护甲（护臂、护腿、靴子等）
    private void EquipSymmetricArmor(ItemSO armorItem, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.leftSocket == null || socket.rightSocket == null)
        {
            Debug.LogError($"对称装备 {armorItem.armorType} 的左右挂载点未设置完整！");
            return;
        }

        
        socket.leftEquipment = InstantiateArmorModel(armorItem, socket.leftSocket);
        socket.rightEquipment = InstantiateArmorModel(armorItem, socket.rightSocket);

        ApplyArmorProperties(armorItem);
    }


    // 实例化护甲模型
    private GameObject InstantiateArmorModel(ItemSO armorItem, Transform socketTransform)
    {
        if (armorItem.weaponPrefab == null)
        {
            Debug.LogError($"护甲 {armorItem.name} 的Prefab未设置！");
            return null;
        }

        GameObject equipment = Instantiate(
            armorItem.weaponPrefab,
            socketTransform.position,
            socketTransform.rotation,
            socketTransform
        );
        equipment.transform.localPosition = Vector3.zero;
        equipment.transform.localRotation = Quaternion.identity;

        return equipment;
    }

   

    // 卸下单部位装备
    private void UnequipSingleArmor(EquipmentSocket socket)
    {
        if (socket.currentEquipment != null)
        {
            RemoveArmorPropertiesFromInstance(socket.currentEquipment);
            Destroy(socket.currentEquipment);
            socket.currentEquipment = null;
        }
    }

    // 卸下对称部位装备
    private void UnequipSymmetricArmor(EquipmentSocket socket)
    {
        // 只需要移除一次属性（左右装备共享属性）
        bool propertiesRemoved = false;

        if (socket.leftEquipment != null)
        {
            if (!propertiesRemoved)
            {
                RemoveArmorPropertiesFromInstance(socket.leftEquipment);
                propertiesRemoved = true;
            }
            Destroy(socket.leftEquipment);
            socket.leftEquipment = null;
        }

        if (socket.rightEquipment != null)
        {
            // 右侧装备不需要再次移除属性
            
            Destroy(socket.rightEquipment);
            socket.rightEquipment = null;
        }
    }

    // 从装备实例获取ItemSO并移除属性
    private void RemoveArmorPropertiesFromInstance(GameObject equipmentInstance)
    {
        PickableObject po = equipmentInstance.GetComponent<PickableObject>();
        if (po != null && playerProperty != null)
        {
            RemoveArmorProperties(po.itemSO);
        }
    }

    // 应用护甲属性
    private void ApplyArmorProperties(ItemSO armorItem)
    {
        if (playerProperty == null) return;

        foreach (Property p in armorItem.propertyList)
        {
            switch (p.propertyType)
            {
                case PropertyType.DefenseValue:
                    playerProperty.AddArmorValue(p.value);
                    break;
                case PropertyType.HPValue:
                case PropertyType.EnergyValue:
                    playerProperty.AddProperty(p.propertyType, p.value);
                    break;
            }
        }
    }

    // 移除护甲属性
    private void RemoveArmorProperties(ItemSO armorItem)
    {
        if (playerProperty == null) return;

        foreach (Property p in armorItem.propertyList)
        {
            switch (p.propertyType)
            {
                case PropertyType.DefenseValue:
                    playerProperty.RemoveArmorValue(p.value);
                    break;
                case PropertyType.HPValue:
                case PropertyType.EnergyValue:
                    playerProperty.RemoveProperty(p.propertyType, p.value);
                    break;
            }
        }
    }

    // 获取当前装备的ItemSO（用于存档等）
    public ItemSO GetEquippedItem(ArmorType armorType)
    {
        EquipmentSocket socket = GetSocketByType(armorType);
        if (socket == null) return null;

        GameObject equipmentInstance = socket.isSymmetric ?
            socket.leftEquipment : socket.currentEquipment;

        if (equipmentInstance != null)
        {
            PickableObject po = equipmentInstance.GetComponent<PickableObject>();
            return po?.itemSO;
        }

        return null;
    }

    // 卸下所有装备（用于角色死亡、重置等）
    public void UnequipAll()
    {
        foreach (EquipmentSocket socket in equipmentSockets)
        {
            UnequipArmor(socket);
        }
    }
}