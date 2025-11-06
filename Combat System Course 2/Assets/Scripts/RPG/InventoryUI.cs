using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public GameObject PausePanel;
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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
        //回到主菜单（快捷键esc键）
        if (Input.GetKeyDown(KeyCode.H))
        {
            // 保存当前游戏（如果有）
            if (IsInGameScene() && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }

            // 关键：清空所有单例数据
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearInventory();
                Debug.Log("返回主菜单：InventoryManager 已清空");
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ResetAllQuests();
                Debug.Log("返回主菜单：QuestManager 已重置");
            }
            if (WeaponEquipmentManager.Instance != null)
            {
                WeaponEquipmentManager.Instance.UnequipWeapon();
                Debug.Log("返回主菜单：武器装备已重置");
            }
            if (ArmorEquipmentManager.Instance != null)
            {

                ArmorEquipmentManager.Instance.UnequipAll();
                Debug.Log("返回主菜单：护甲装备已重置");
            }

            SaveManager.Instance.currentSaveData.level = 1;
            SaveManager.Instance.currentSaveData.currEXP = 0;
            SaveManager.Instance.currentSaveData.hpValue = 100;
            SaveManager.Instance.currentSaveData.maxHealth = 100;
            SaveManager.Instance.currentSaveData.energyValue = 100;
            SaveManager.Instance.currentSaveData.armorValue = 0;

            // 重置装备信息
            SaveManager.Instance.currentSaveData.equippedWeapon = "";
            SaveManager.Instance.currentSaveData.equippedHelmet = "";
            SaveManager.Instance.currentSaveData.equippedChestplate = "";
            SaveManager.Instance.currentSaveData.equippedGauntlets = "";
            SaveManager.Instance.currentSaveData.equippedLeggings = "";
            SaveManager.Instance.currentSaveData.equippedBoots = "";

            // 清空场景物品拾取状态
            SaveManager.Instance.currentSaveData.scenePickedItems.Clear();

            // 清空机关激活状态
            SaveManager.Instance.currentSaveData.sceneMechanismStates.Clear();


            PlayerPrefs.SetString("TargetScene", "000Scene_Menu");
            PlayerPrefs.Save();
            SaveManager.shouldLoadFromSave = false;
            SceneManager.LoadScene("LoadingScene");


        }
        // 检测攻击按键来关闭攻击提示
        if (attackHintUI != null && attackHintUI.activeSelf && Input.GetKeyDown(attackKey))
        {
            attackHintUI.SetActive(false);
            hasShownAttackHint = true;
        }
    }
    private bool IsInGameScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene != "000Scene_Menu" && currentScene != "LoadingScene";
    }


    private void TogglePausePanel()
    {
        isMapOpen = !isMapOpen;
        PausePanel.SetActive(isMapOpen);
        Time.timeScale = isMapOpen ? 0f : 1f;

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
        if (playerFighter != null && WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null)
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
        // 检查是否已存在该物品的UI（堆叠物品）
        if (itemSO.IsStackable())
        {
            ItemUI existingUI = FindItemUI(itemSO);
            if (existingUI != null)
            {
                existingUI.UpdateAmountDisplay();
                return;
            }
        }

        // 不存在或非堆叠物品，创建新UI
        GameObject itemGo = GameObject.Instantiate(itemPrefab);
        itemGo.transform.SetParent(content.transform);
        ItemUI itemUI = itemGo.GetComponent<ItemUI>();
        itemUI.InitItem(itemSO);
    }

    // 查找物品的UI元素
    private ItemUI FindItemUI(ItemSO targetItem)
    {
        if (targetItem == null) return null;

        foreach (Transform child in content.transform)
        {
            if (child == null) continue;

            ItemUI itemUI = child.GetComponent<ItemUI>();
            if (itemUI != null && itemUI.itemSO != null)
            {
                // 更严格的匹配条件
                if (itemUI.itemSO.nameOfItem == targetItem.nameOfItem &&
                    itemUI.itemSO.itemType == targetItem.itemType)
                {
                    return itemUI;
                }
            }
        }
        return null;
    }
    public void UpdateItemAmountDisplay(ItemSO targetItem)
    {
        ItemUI existingUI = FindItemUI(targetItem);
        if (existingUI != null)
        {
            existingUI.UpdateAmountDisplay();
        }
        else
        {
            // 如果找不到UI，说明需要创建新的
            AddItem(targetItem);
        }
    }

    public void OnItemClick(ItemSO itemSO, ItemUI itemUI)
    {
        itemDetail.UpdateDetailUI(itemSO, itemUI);
    }

    public void OnItemUse(ItemSO itemSO, ItemUI itemUI)
    {
        // 先处理背包逻辑（减少数量）
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            // 直接减少数量
            itemSO.amount -= 1;
            itemUI.UpdateAmountDisplay(); // 更新UI显示

            // 再使用物品效果
            ItemUsageHandler.Instance.UseItem(itemSO);
        }
        else
        {
            // 最后一个物品或非堆叠物品
            Destroy(itemUI.gameObject);
            InventoryManager.Instance.itemList.Remove(itemSO);

            // 再使用物品效果
            ItemUsageHandler.Instance.UseItem(itemSO);
        }
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

        // 直接为每个物品创建UI，不进行堆叠检查
        foreach (ItemSO item in InventoryManager.Instance.itemList)
        {
            if (item == null) continue;

            // 直接实例化UI预制体
            GameObject itemGo = Instantiate(itemPrefab);
            itemGo.transform.SetParent(content.transform);
            itemGo.transform.localScale = Vector3.one;
            itemGo.SetActive(true);

            ItemUI itemUI = itemGo.GetComponent<ItemUI>();
            if (itemUI != null)
            {
                itemUI.InitItem(item);
            }
            else
            {
                Debug.LogError("物品预制体缺少ItemUI组件");
            }
        }

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
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