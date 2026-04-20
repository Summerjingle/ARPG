using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ItemUsageHandler : MonoBehaviour
{
    public static ItemUsageHandler Instance { get; private set; }
    public ArmorEquipmentManager armorManager; 
    public PlayerProperty playerProperty;
    [SerializeField] public Transform weapon1Socket; // Weapon1�ڵ������
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
                Debug.Log($"ʹ������Ʒ: {itemSO.nameOfItem}");
                break;
            case ItemType.Armor:
                armorManager.EquipArmor(itemSO);
                break;
            case ItemType.QuestRelated:
                Debug.Log("��������޷�ֱ��ʹ��");
                
                break;
            default:
                Debug.LogWarning($"δ֪����Ʒ����: {itemSO.itemType}");
                break;
        }
    }
}