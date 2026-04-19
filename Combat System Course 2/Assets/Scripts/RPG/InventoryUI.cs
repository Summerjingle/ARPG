using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    public GameObject content;
    public GameObject itemPrefab;
    public ItemDetailUI itemDetail;
    [Header("装备槽位")]
    public Image currentWeaponIcon;   // 武器
    public Image currentHelmetIcon;   // 头盔
    public Image currentChestplateIcon; // 护甲
    public Image currentGauntletsIcon; // 手套
    public Image currentLeggingsIcon;  // 腿甲
    public Image currentBootsIcon;     // 鞋子

    private ItemUI currentSelectedItem;

    [Header("攻击提示")]
    public GameObject attackHintUI; // 攻击提示UI
    public KeyCode attackKey = KeyCode.Mouse1; // 攻击按键，默认为右键

    [SerializeField] private GameObject inventoryPanel;
    private PlayerInputActions inputActions;

    public static bool IsInventoryOpen { get; private set; }


    // 是否已经显示过攻击提示
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

    private void OnEnable()
    {
        
       
        InputManager.Instance.OnToggleInventory += ToggleInventory;
        InputManager.Instance.OnUISubmit += HandleSubmit;
        InputManager.Instance.OnUICancel += HandleCancel;
    }

    private void OnDisable()
    {
        // 注销事件
        InputManager.Instance.OnToggleInventory -= ToggleInventory;
        InputManager.Instance.OnUISubmit -= HandleSubmit;
        InputManager.Instance.OnUICancel -= HandleCancel;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
    private void OnBagPerformed(InputAction.CallbackContext ctx)
    {
        ToggleInventory();
    }


   public void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(IsInventoryOpen);

        UIStateManager.SetUIActive(IsInventoryOpen);

        Debug.Log($"背包 {(IsInventoryOpen ? "打开" : "关闭")}");

        if (IsInventoryOpen)
        {
            InputManager.Instance.SwitchToInventory();   // 你已经加了的切换方法
            SelectFirstItem();                           // 新增：自动选中第一个物品
        }
        else
        {
            InputManager.Instance.SwitchToPlayerFromInventory();
            CheckAndShowAttackHint();
        }
    }
    private void HandleCancel()
    {
        Debug.Log("Cancel pressed");

       if (itemDetail.gameObject.activeSelf)
        {
            itemDetail.gameObject.SetActive(false);

            // ✅ 把焦点还给当前选中的物品
            if (currentSelectedItem != null)
            {
                EventSystem.current.SetSelectedGameObject(currentSelectedItem.gameObject);
            }

            return;
        }

        // ✅ 如果详情没开，才关闭背包
        ToggleInventory();
    }
    private void HandleSubmit()
{
    if (itemDetail.gameObject.activeSelf)
    {
        itemDetail.OnUseButtonClick(); // 👉 直接调用
    }
    else
    {
        OnItemClick(currentSelectedItem.itemSO, currentSelectedItem);
    }
}



    // 检查并显示攻击提示
    private void CheckAndShowAttackHint()
    {
        // 如果已经显示过提示，则不再显示
        if (hasShownAttackHint) return;

        // 直接检查是否已经有武器并且已经进入战斗状态
        if (WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null)
        {
            // 如果有武器且还未显示过提示，则显示攻击提示
            if (attackHintUI != null)
            {
                attackHintUI.SetActive(true);
                Debug.Log("显示攻击提示：玩家已经持有武器");
            }
        }
    }

    // 手动重置攻击提示状态（比如游戏初始化时）
    public void ResetAttackHint()
    {
        hasShownAttackHint = false;
        if (attackHintUI != null)
            attackHintUI.SetActive(false);
    }

    // 物品按钮点击，或者UI按钮打开背包的
    public void OnInventoryButtonClick()
    {
        ToggleInventory();
    }

    public void AddItem(ItemSO itemSO)
    {
        // 检查是否已存在该物品UI（可堆叠物品）
        if (itemSO.IsStackable())
        {
            ItemUI existingUI = FindItemUI(itemSO);
            if (existingUI != null)
            {
                existingUI.UpdateAmountDisplay();
                return;
            }
        }

        // 创建新的或非堆叠物品的UI
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
                // 通过详细匹配（名称）
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
            // 如果找不到UI说明需要添加新的
            AddItem(targetItem);
        }
    }
    void Update()
    {   
        if (IsInventoryOpen)
        {
            DebugNavigation();
        }
    }
    public void OnItemClick(ItemSO itemSO, ItemUI itemUI)
    {
        itemDetail.UpdateDetailUI(itemSO, itemUI);
         EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(itemDetail.useButton.gameObject);
    }

    public void OnItemUse(ItemSO itemSO, ItemUI itemUI)
    {
        // 先处理数量逻辑，再调用效果
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

        // 直接为每个物品创建UI（不管是否堆叠）
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
                Debug.LogError("物品预制体缺少ItemUI脚本");
            }
        }

        // 强制刷新布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }
    private void SelectFirstItem()
{
    if (content == null || content.transform.childCount == 0)
    {
        Debug.LogWarning("[Inventory] content 中没有物品，无法选中");
        return;
    }

    // 只从 content 里找第一个有效的 Selectable（ItemUI）
    Selectable firstSelectable = content.transform.GetChild(0).GetComponent<Selectable>();

    if (firstSelectable != null && firstSelectable.interactable)
    {
        // 强制清空当前选中，再选中 content 里的第一个物品
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);

        Debug.Log($"[Inventory] 成功选中 content 里的第一个物品 → {firstSelectable.name}");
    }
    else
    {
        Debug.LogWarning("[Inventory] content 第一个子物体上缺少 Selectable 组件（Button）");
    }
}
    private void DebugNavigation()
{
    if (EventSystem.current == null)
    {
        Debug.LogWarning("EventSystem.current 为 null");
        return;
    }

    GameObject selectedGO = EventSystem.current.currentSelectedGameObject;
    
    if (selectedGO != null)
    {
        ItemUI itemUI = selectedGO.GetComponent<ItemUI>();

        if (itemUI != null && currentSelectedItem != itemUI)
        {
            currentSelectedItem = itemUI;

            string itemName = itemUI.itemSO != null ? itemUI.itemSO.nameOfItem : "未知物品";
            Debug.Log($"[Navigation Debug] 当前选中物品: {itemName} | GameObject: {selectedGO.name}");
        }
    }
    else
    {
        Debug.Log("[Navigation Debug] 当前没有选中任何物品");
    }
}
}