using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public GameObject map;
    public GameObject content;
    public GameObject itemPrefab;
    public ItemDetailUI itemDetail;
    [Header("装备槽位")]
    public Image currentWeaponIcon;   // 武器
    public Image currentHelmetIcon;   // 头盔
    public Image currentChestplateIcon; // 胸甲
    public Image currentGauntletsIcon; // 护手
    public Image currentLeggingsIcon;  // 护腿
    public Image currentBootsIcon;     // 靴子

    [Header("攻击提示")]
    public GameObject attackHintUI; // 攻击提示UI
    public KeyCode attackKey = KeyCode.Mouse1; // 攻击按键，默认为鼠标左键

    [SerializeField] private GameObject inventoryPanel;
    public CameraController cameraController;

    public static bool IsInventoryOpen { get; private set; }
    private bool isMapOpen=false;

    // 标记是否已经显示过攻击提示
    private static bool hasShownAttackHint = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        Instance = this;
        // 初始化时关闭背包
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        IsInventoryOpen = false;

        // 初始化时关闭攻击提示
        if (attackHintUI != null)
            attackHintUI.SetActive(false);
    }

    private void Update()
    {
        // 切换背包打开/关闭（快捷键I键）
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        //切换地图打开/关闭（快捷键M键）
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
        // 检测攻击按键来关闭攻击提示
        if (attackHintUI != null && attackHintUI.activeSelf && Input.GetKeyDown(attackKey))
        {
            attackHintUI.SetActive(false);
            hasShownAttackHint = true;
        }
    }
    private void ToggleMap()
    {
        isMapOpen = !isMapOpen;
        map.SetActive(isMapOpen);
    }

    public void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(IsInventoryOpen);

        // 控制相机和UI交互
        if (cameraController != null)
        {
            cameraController.SetUIActive(IsInventoryOpen);
        }
        else
        {
            Debug.LogWarning("CameraController未分配！");
        }

        Debug.Log($"背包 {(IsInventoryOpen ? "打开" : "关闭")}");

        // 关闭背包时检查是否需要显示攻击提示
        if (!IsInventoryOpen)
        {
            CheckAndShowAttackHint();
        }
    }

    // 检查并显示攻击提示
    private void CheckAndShowAttackHint()
    {
        // 如果已经显示过提示，不再显示
        if (hasShownAttackHint) return;

        // 找到玩家角色的MeleeFighter
        var playerFighter = FindPlayerMeleeFighter();
        if (playerFighter != null && playerFighter.currentWeapon != null)
        {
            // 玩家有武器且未显示过提示，显示攻击提示
            if (attackHintUI != null)
            {
                attackHintUI.SetActive(true);
                Debug.Log("显示攻击提示：按鼠标左键进行攻击");
            }
        }
    }

    // 找到玩家的MeleeFighter组件
    private MeleeFighter FindPlayerMeleeFighter()
    {
        // 方法1：通过标签查找
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.GetComponent<MeleeFighter>();
        }

        // 方法2：通过PlayerProperty组件查找
        PlayerProperty playerProperty = FindObjectOfType<PlayerProperty>();
        if (playerProperty != null)
        {
            return playerProperty.GetComponent<MeleeFighter>();
        }

        // 方法3：通过isPlayer字段查找
        MeleeFighter[] allFighters = FindObjectsOfType<MeleeFighter>();
        foreach (MeleeFighter fighter in allFighters)
        {
            if (fighter.isPlayer)
                return fighter;
        }

        return null;
    }

    // 手动重置攻击提示状态（用于新游戏等场景）
    public void ResetAttackHint()
    {
        hasShownAttackHint = false;
        if (attackHintUI != null)
            attackHintUI.SetActive(false);
    }

    // 物品栏按钮点击（方便UI按钮打开背包）
    public void OnInventoryButtonClick()
    {
        ToggleInventory();
    }

    public void AddItem(ItemSO itemSO)
    {
        GameObject itemGo = GameObject.Instantiate(itemPrefab);
        itemGo.transform.SetParent(content.transform);
        ItemUI itemUI = itemGo.GetComponent<ItemUI>();
        itemUI.InitItem(itemSO);
    }

    public void OnItemClick(ItemSO itemSO, ItemUI itemUI)
    {
        itemDetail.UpdateDetailUI(itemSO, itemUI);
    }

    public void OnItemUse(ItemSO itemSO, ItemUI itemUI)
    {
        Destroy(itemUI.gameObject);
        InventoryManager.Instance.RemoveItem(itemSO);
        // 使用物品
        ItemUsageHandler.Instance.UseItem(itemSO);
    }

    // 更新装备图标的方法
    public void UpdateEquipmentIcon(ItemSO item)
    {
        if (item == null) return;

        switch (item.itemType)
        {
            case ItemType.Weapon:
                UpdateIcon(ref currentWeaponIcon, item.icon);
                break;
            case ItemType.Armor:
                UpdateArmorIcon(item);
                break;
        }
    }

    // 更新护甲图标（根据ArmorType分类）
    private void UpdateArmorIcon(ItemSO armorItem)
    {
        switch (armorItem.armorType)
        {
            case ArmorType.Helmet:
                UpdateIcon(ref currentHelmetIcon, armorItem.icon);
                break;
            case ArmorType.Chestplate:
                UpdateIcon(ref currentChestplateIcon, armorItem.icon);
                break;
            case ArmorType.Gauntlets:
                UpdateIcon(ref currentGauntletsIcon, armorItem.icon);
                break;
            case ArmorType.Leggings:
                UpdateIcon(ref currentLeggingsIcon, armorItem.icon);
                break;
            case ArmorType.Boots:
                UpdateIcon(ref currentBootsIcon, armorItem.icon);
                break;
        }
    }

    // 更新图标显示
    private void UpdateIcon(ref Image iconSlot, Sprite icon)
    {
        if (iconSlot != null)
        {
            iconSlot.sprite = icon;
            iconSlot.enabled = icon != null;
        }
    }

    // 清除指定类型的装备图标
    public void ClearEquipmentIcon(ItemType itemType, ArmorType armorType = ArmorType.NotArmor)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                UpdateIcon(ref currentWeaponIcon, null);
                break;
            case ItemType.Armor:
                ClearArmorIcon(armorType);
                break;
        }
    }

    private void ClearArmorIcon(ArmorType armorType)
    {
        switch (armorType)
        {
            case ArmorType.Helmet:
                UpdateIcon(ref currentHelmetIcon, null);
                break;
            case ArmorType.Chestplate:
                UpdateIcon(ref currentChestplateIcon, null);
                break;
            case ArmorType.Gauntlets:
                UpdateIcon(ref currentGauntletsIcon, null);
                break;
            case ArmorType.Leggings:
                UpdateIcon(ref currentLeggingsIcon, null);
                break;
            case ArmorType.Boots:
                UpdateIcon(ref currentBootsIcon, null);
                break;
            default:
                Debug.LogWarning($"未知的护甲类型: {armorType}");
                break;
        }
    }

    public void UpdateInventoryUI()
    {
        // 清空当前UI
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        // 重新生成所有物品
        foreach (ItemSO item in InventoryManager.Instance.itemList)
        {
            AddItem(item);
        }
    }

    public void RegisterCameraController(CameraController controller)
    {
        cameraController = controller;
        Debug.Log("CameraController 注册到 InventoryUI");
    }

    public void UnregisterCameraController()
    {
        cameraController = null;
        Debug.Log("CameraController 从 InventoryUI 解除注册");
    }
}