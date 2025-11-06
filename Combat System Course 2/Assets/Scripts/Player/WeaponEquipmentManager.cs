using UnityEngine;

public class WeaponEquipmentManager : MonoBehaviour
{
    public static WeaponEquipmentManager Instance { get; private set; }

    [Header("武器挂载点")]
    public Transform weaponSocket; // 在Inspector中分配weapon1Socket

    private Weapon currentWeapon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    // 装备武器
    public void EquipWeapon(ItemSO weaponItem)
    {
        UnequipWeapon();

        if (weaponItem?.weaponPrefab == null)
        {
            Debug.LogError("武器物品或预制体为空");
            return;
        }

        // 实例化武器模型
        GameObject weaponObj = Instantiate(
            weaponItem.weaponPrefab,
            weaponSocket.position,
            weaponSocket.rotation,
            weaponSocket
        );

        currentWeapon = weaponObj.GetComponent<Weapon>();
        if (currentWeapon != null)
        {
            currentWeapon.Initialize(weaponItem);
        }

        // 更新UI
        InventoryUI.Instance?.UpdateEquipmentIcon(weaponItem);
    }

    // 卸下武器
    public void UnequipWeapon()
    {
        if (currentWeapon != null)
        {
            // 将武器添加回背包
            if (InventoryManager.Instance != null && currentWeapon.itemSO != null)
            {
                InventoryManager.Instance.ReAddItem(currentWeapon.itemSO);
            }

            Destroy(currentWeapon.gameObject);
            currentWeapon = null;
        }

        // 清除UI
        InventoryUI.Instance?.ClearEquipmentIcon(ItemType.Weapon, ArmorType.NotArmor);
    }

    // 获取当前武器
    public Weapon GetCurrentWeapon() => currentWeapon;

    // 获取武器伤害
    public float GetWeaponDamage()
    {
        return currentWeapon?.GetDamage() ?? 5f;
    }
}