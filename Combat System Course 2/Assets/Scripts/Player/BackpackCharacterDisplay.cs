using UnityEngine;

public class BackpackCharacterDisplay : MonoBehaviour
{
    public static BackpackCharacterDisplay Instance { get; private set; }

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("防具挂点")]
    [SerializeField] private Transform helmetSocket;
    [SerializeField] private Transform chestplateSocket;
    [SerializeField] private Transform gauntletsSocket_Left;
    [SerializeField] private Transform gauntletsSocket_Right;
    [SerializeField] private Transform leggingsSocket_Left;
    [SerializeField] private Transform leggingsSocket_Right;
    [SerializeField] private Transform bootsSocket_Left;
    [SerializeField] private Transform bootsSocket_Right;

    [Header("武器挂点")]
    [SerializeField] private Transform weaponSocket;       // 腰部
    [SerializeField] private Transform weaponSocket_Back;  // 背部

    // Animator State 名称 —— 填到动画机里
    private const string STATE_HELMET      = "ShowHelmet";
    private const string STATE_CHESTPLATE  = "ShowChestplate";
    private const string STATE_GAUNTLETS   = "ShowGauntlets";
    private const string STATE_LEGGINGS    = "ShowLeggings";
    private const string STATE_BOOTS       = "ShowBoots";

    // 当前实例化引用
    private GameObject currentHelmet;
    private GameObject currentChestplate;
    private GameObject currentGauntlets_L;
    private GameObject currentGauntlets_R;
    private GameObject currentLeggings_L;
    private GameObject currentLeggings_R;
    private GameObject currentBoots_L;
    private GameObject currentBoots_R;
    private GameObject currentWeapon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ==================== 防具 ====================

    public void EquipArmor(ArmorSO armorSO)
    {
        if (armorSO == null || armorSO.equipmentPrefab == null) return;

        switch (armorSO.armorType)
        {
            case ArmorType.Helmet:
                EquipSingle(armorSO, helmetSocket, ref currentHelmet, STATE_HELMET);
                break;
            case ArmorType.Chestplate:
                EquipSingle(armorSO, chestplateSocket, ref currentChestplate, STATE_CHESTPLATE);
                break;
            case ArmorType.Gauntlets:
                EquipSymmetric(armorSO, gauntletsSocket_Left, gauntletsSocket_Right,
                    ref currentGauntlets_L, ref currentGauntlets_R, STATE_GAUNTLETS);
                break;
            case ArmorType.Leggings:
                EquipSymmetric(armorSO, leggingsSocket_Left, leggingsSocket_Right,
                    ref currentLeggings_L, ref currentLeggings_R, STATE_LEGGINGS);
                break;
            case ArmorType.Boots:
                EquipSymmetric(armorSO, bootsSocket_Left, bootsSocket_Right,
                    ref currentBoots_L, ref currentBoots_R, STATE_BOOTS);
                break;
        }
    }

    public void UnequipArmor(ArmorType armorType)
    {
        switch (armorType)
        {
            case ArmorType.Helmet:
                DestroyAndClear(ref currentHelmet);
                break;
            case ArmorType.Chestplate:
                DestroyAndClear(ref currentChestplate);
                break;
            case ArmorType.Gauntlets:
                DestroyAndClear(ref currentGauntlets_L);
                DestroyAndClear(ref currentGauntlets_R);
                break;
            case ArmorType.Leggings:
                DestroyAndClear(ref currentLeggings_L);
                DestroyAndClear(ref currentLeggings_R);
                break;
            case ArmorType.Boots:
                DestroyAndClear(ref currentBoots_L);
                DestroyAndClear(ref currentBoots_R);
                break;
        }
    }

    // ==================== 武器（无动画） ====================

    public void EquipWeapon(WeaponSO weaponSO)
    {
        if (weaponSO == null || weaponSO.equipmentPrefab == null) return;

        UnequipWeapon();

        // 根据武器配置的鞘位选择挂点
        Weapon prefabWeapon = weaponSO.equipmentPrefab.GetComponent<Weapon>();
        Transform targetSocket = (prefabWeapon != null && prefabWeapon.sheathLocation == SheathLocation.Back)
            ? weaponSocket_Back
            : weaponSocket;

        if (targetSocket == null) return;

        currentWeapon = InstantiateEquipment(weaponSO.equipmentPrefab, targetSocket);
    }

    public void UnequipWeapon()
    {
        DestroyAndClear(ref currentWeapon);
    }

    // ==================== 选中高亮 ====================

    /// <summary>高亮指定类型已装备的防具</summary>
    public void HighlightEquippedArmor(ArmorType armorType)
    {
        switch (armorType)
        {
            case ArmorType.Helmet:
                SetOutlineActive(currentHelmet, true);
                break;
            case ArmorType.Chestplate:
                SetOutlineActive(currentChestplate, true);
                break;
            case ArmorType.Gauntlets:
                SetOutlineActive(currentGauntlets_L, true);
                SetOutlineActive(currentGauntlets_R, true);
                break;
            case ArmorType.Leggings:
                SetOutlineActive(currentLeggings_L, true);
                SetOutlineActive(currentLeggings_R, true);
                break;
            case ArmorType.Boots:
                SetOutlineActive(currentBoots_L, true);
                SetOutlineActive(currentBoots_R, true);
                break;
        }
    }

    /// <summary>取消指定类型防具的高亮</summary>
    public void ClearArmorHighlight(ArmorType armorType)
    {
        switch (armorType)
        {
            case ArmorType.Helmet:
                SetOutlineActive(currentHelmet, false);
                break;
            case ArmorType.Chestplate:
                SetOutlineActive(currentChestplate, false);
                break;
            case ArmorType.Gauntlets:
                SetOutlineActive(currentGauntlets_L, false);
                SetOutlineActive(currentGauntlets_R, false);
                break;
            case ArmorType.Leggings:
                SetOutlineActive(currentLeggings_L, false);
                SetOutlineActive(currentLeggings_R, false);
                break;
            case ArmorType.Boots:
                SetOutlineActive(currentBoots_L, false);
                SetOutlineActive(currentBoots_R, false);
                break;
        }
    }

    /// <summary>高亮当前武器</summary>
    public void HighlightWeapon()
    {
        SetOutlineActive(currentWeapon, true);
    }

    /// <summary>取消武器高亮</summary>
    public void ClearWeaponHighlight()
    {
        SetOutlineActive(currentWeapon, false);
    }

    // ==================== 内部方法 ====================

    private void SetOutlineActive(GameObject equipment, bool active)
    {
        if (equipment == null)
        {
            Debug.LogWarning($"[BackpackCharacterDisplay] SetOutlineActive({active}): equipment 为 null，该槽位没有装备实例。");
            return;
        }

        var outlines = equipment.GetComponentsInChildren<AddOutlineToRenderer>();
        if (outlines.Length == 0)
        {
            Debug.LogWarning($"[BackpackCharacterDisplay] SetOutlineActive({active}): {equipment.name} 及其子物体上没有找到 AddOutlineToRenderer 组件。", equipment);
            return;
        }

        foreach (var outline in outlines)
        {
            Debug.Log($"[BackpackCharacterDisplay] SetOutlineActive({active}): {equipment.name} → outline.CreateOutline()/DestroyOutline()", equipment);

            if (active)
                outline.CreateOutline();
            else
                outline.DestroyOutline();
        }
    }

    private void EquipSingle(ArmorSO armorSO, Transform socket, ref GameObject current, string stateName)
    {
        if (socket == null) return;

        DestroyAndClear(ref current);
        current = InstantiateEquipment(armorSO.equipmentPrefab, socket);

        if (animator != null)
            animator.Play(stateName);
    }

    private void EquipSymmetric(ArmorSO armorSO, Transform leftSock, Transform rightSock,
        ref GameObject left, ref GameObject right, string stateName)
    {
        DestroyAndClear(ref left);
        DestroyAndClear(ref right);

        if (leftSock != null)
            left = InstantiateEquipment(armorSO.equipmentPrefab, leftSock);
        if (rightSock != null)
            right = InstantiateEquipment(armorSO.equipmentPrefab, rightSock);

        if (animator != null)
            animator.Play(stateName);
    }

    private GameObject InstantiateEquipment(GameObject prefab, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        SetLayerRecursive(obj, LayerMask.NameToLayer("UI_Model"));
        return obj;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private void DestroyAndClear(ref GameObject obj)
    {
        if (obj != null)
        {
            Destroy(obj);
            obj = null;
        }
    }
}
