using UnityEngine;
using System.Collections.Generic;
using System;

public class ArmorEquipmentManager : MonoBehaviour
{
    [System.Serializable]
    
    public class EquipmentSocket
    {

        public ArmorType armorType; // ��������

        [Header("����λװ������")]
        public Transform socketTransform; // ����λװ��ʹ��

        [Header("�ԳƲ�λװ������")]
        public bool isSymmetric = false;  // �Ƿ�Ϊ�Գ�װ��
        public Transform leftSocket;      // �����ص㣨isSymmetricΪtrueʱʹ�ã�
        public Transform rightSocket;     // �Ҳ���ص㣨isSymmetricΪtrueʱʹ�ã�

        [HideInInspector] public GameObject currentEquipment; // ����λװ��ʵ��
        [HideInInspector] public GameObject leftEquipment;    // �ԳƲ�λ��װ��ʵ��
        [HideInInspector] public GameObject rightEquipment;   // �ԳƲ�λ��װ��ʵ��
    }

    [SerializeField] private EquipmentSocket[] equipmentSockets;
    public static ArmorEquipmentManager Instance { get; private set; }
    public event Action OnEquipmentChanged;//装备事件
    private PlayerProperty playerProperty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        playerProperty = GetComponent<PlayerProperty>();
    }

    // װ�����ף���¶���ⲿ���õ�Ψһ�ӿڣ�
    public void EquipArmor(ItemSO armorItem)
    {
        EquipmentSocket targetSocket = GetSocketByType(armorItem.armorType);
        if (targetSocket == null) return;

        UnequipArmor(targetSocket, false);

        // ����װ������ѡ������߼�
        if (targetSocket.isSymmetric)
        {
            EquipSymmetricArmor(armorItem, targetSocket);
        }
        else
        {
            EquipSingleArmor(armorItem, targetSocket);
        }
        OnEquipmentChanged?.Invoke();
        InventoryUI.Instance.UpdateEquipmentIcon(armorItem);
        
    }

    // ���ݻ��������ҵ���Ӧ�Ĺ��ص�
    public EquipmentSocket GetSocketByType(ArmorType armorType)
    {
        foreach (var socket in equipmentSockets)
        {
            if (socket.armorType == armorType) return socket;
        }
        Debug.LogError($"δ���� {armorType} ���͵Ĺ��ص㣡");
        return null;
    }

    // ж��ָ�����͵Ļ���
    public  void UnequipArmor(EquipmentSocket socket, bool sendNotification = true)
    {
        // �Ȼ�ȡ��ǰװ����ItemSO������Ϊnull��
        ItemSO equippedItem = GetEquippedItem(socket.armorType);

        // ����װ������ѡ��ж���߼�
        if (socket.isSymmetric)
        {
            UnequipSymmetricArmor(socket);
        }
        else
        {
            UnequipSingleArmor(socket);
        }

        // ���UIͼ�꣨�����Ƿ���װ����Ҫִ�У�
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ClearEquipmentIcon(ItemType.Armor, socket.armorType);
        }

        // ֻ��ȷʵ��װ��ʱ�����ӻر���
        if (equippedItem != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReAddItem(equippedItem);
        }
        if (sendNotification)
        {
            OnEquipmentChanged?.Invoke();
        }
        
    }

    // װ������λ���ף�ͷ�����ؼ׵ȣ�
    private void EquipSingleArmor(ItemSO armorItem, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.socketTransform == null)
        {
            Debug.LogError($"����λװ�� {armorItem.armorType} �Ĺ��ص�δ���ã�");
            return;
        }

        socket.currentEquipment = InstantiateArmorModel(armorItem, socket.socketTransform);
        ApplyArmorProperties(armorItem);
        
    }

    // װ���ԳƲ�λ���ף����ۡ����ȡ�ѥ�ӵȣ�
    private void EquipSymmetricArmor(ItemSO armorItem, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.leftSocket == null || socket.rightSocket == null)
        {
            Debug.LogError($"�Գ�װ�� {armorItem.armorType} �����ҹ��ص�δ����������");
            return;
        }

        
        socket.leftEquipment = InstantiateArmorModel(armorItem, socket.leftSocket);
        socket.rightEquipment = InstantiateArmorModel(armorItem, socket.rightSocket);

        ApplyArmorProperties(armorItem);
        
    }


    // ʵ��������ģ��
    private GameObject InstantiateArmorModel(ItemSO armorItem, Transform socketTransform)
    {
        if (armorItem.weaponPrefab == null)
        {
            Debug.LogError($"���� {armorItem.name} ��Prefabδ���ã�");
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

   

    // ж�µ���λװ��
    private void UnequipSingleArmor(EquipmentSocket socket)
    {
        if (socket.currentEquipment != null)
        {
            RemoveArmorPropertiesFromInstance(socket.currentEquipment);
            Destroy(socket.currentEquipment);
            socket.currentEquipment = null;
            
        }
    }

    // ж�¶ԳƲ�λװ��
    private void UnequipSymmetricArmor(EquipmentSocket socket)
    {
        // ֻ��Ҫ�Ƴ�һ�����ԣ�����װ���������ԣ�
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
            // �Ҳ�װ������Ҫ�ٴ��Ƴ�����
            
            Destroy(socket.rightEquipment);
            socket.rightEquipment = null;
        }
        
    }

    // ��װ��ʵ����ȡItemSO���Ƴ�����
    private void RemoveArmorPropertiesFromInstance(GameObject equipmentInstance)
    {
        PickableObject po = equipmentInstance.GetComponent<PickableObject>();
        if (po != null && playerProperty != null)
        {
            RemoveArmorProperties(po.itemSO);
        }
    }

    // Ӧ�û�������
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

    // �Ƴ���������
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

    // ��ȡ��ǰװ����ItemSO�����ڴ浵�ȣ�
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

    // ж������װ�������ڽ�ɫ���������õȣ�
    public void UnequipAll()
    {
        foreach (EquipmentSocket socket in equipmentSockets)
        {
            UnequipArmor(socket);
        }
    }
}