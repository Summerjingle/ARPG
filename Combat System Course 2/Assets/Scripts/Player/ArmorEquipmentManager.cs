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
        ArmorSO armorSO = armorItem as ArmorSO;
        if (armorSO == null) return;

        EquipmentSocket targetSocket = GetSocketByType(armorSO.armorType);
        if (targetSocket == null) return;

        UnequipArmor(targetSocket, false);

        // ����װ������ѡ������߼�
        if (targetSocket.isSymmetric)
        {
            EquipSymmetricArmor(armorSO, targetSocket);
        }
        else
        {
            EquipSingleArmor(armorSO, targetSocket);
        }
        OnEquipmentChanged?.Invoke();
        InventoryUI.Instance.UpdateEquipmentIcon(armorSO);
        BackpackCharacterDisplay.Instance?.EquipArmor(armorSO);
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

        // 同步背包模型
        BackpackCharacterDisplay.Instance?.UnequipArmor(socket.armorType);

        // ���UIͼ�꣨�����Ƿ���װ����Ҫִ�У�
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.ClearEquipmentIcon(ItemType.Armor, socket.armorType);
        }

        if (sendNotification)
        {
            OnEquipmentChanged?.Invoke();
        }

    }
    public void UnequipArmor(ArmorType armorType)//重载卸载武器，入参为武器类型
    {
        EquipmentSocket socket = GetSocketByType(armorType);
        if (socket == null) return;

        UnequipArmor(socket);
    }

    // װ������λ���ף�ͷ�����ؼ׵ȣ�
    private void EquipSingleArmor(ArmorSO armorSO, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.socketTransform == null)
        {
            Debug.LogError($"����λװ�� {armorSO.armorType} �Ĺ��ص�δ���ã�");
            return;
        }

        socket.currentEquipment = InstantiateArmorModel(armorSO, socket.socketTransform);
        ApplyArmorProperties(armorSO);

    }

    // װ���ԳƲ�λ���ף����ۡ����ȡ�ѥ�ӵȣ�
    private void EquipSymmetricArmor(ArmorSO armorSO, EquipmentSocket socket)
    {
        UnequipArmor(socket);

        if (socket.leftSocket == null || socket.rightSocket == null)
        {
            Debug.LogError($"�Գ�װ�� {armorSO.armorType} �����ҹ��ص�δ����������");
            return;
        }


        socket.leftEquipment = InstantiateArmorModel(armorSO, socket.leftSocket);
        socket.rightEquipment = InstantiateArmorModel(armorSO, socket.rightSocket);

        ApplyArmorProperties(armorSO);

    }


    // ʵ��������ģ��
    private GameObject InstantiateArmorModel(ArmorSO armorSO, Transform socketTransform)
    {
        if (armorSO.equipmentPrefab == null)
        {
            Debug.LogError($"���� {armorSO.name} ��Prefabδ���ã�");
            return null;
        }

        GameObject equipment = Instantiate(
            armorSO.equipmentPrefab,
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
            ArmorSO armorSO = po.itemSO as ArmorSO;
            if (armorSO != null)
                RemoveArmorProperties(armorSO);
        }
    }

    // Ӧ�û�������
    private void ApplyArmorProperties(ArmorSO armorSO)
    {
        if (playerProperty == null || armorSO.propertyList == null) return;
        foreach (Property p in armorSO.propertyList)
            playerProperty.AddProperty(p.statType, p.value);
    }

    // �Ƴ���������
    private void RemoveArmorProperties(ArmorSO armorSO)
    {
        if (playerProperty == null || armorSO.propertyList == null) return;
        foreach (Property p in armorSO.propertyList)
            playerProperty.RemoveProperty(p.statType, p.value);
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
