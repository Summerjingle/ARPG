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
    public Image currentWeaponIcon;
    public Image currentHelmetIcon;
    public Image currentChestplateIcon;
    public Image currentGauntletsIcon;
    public Image currentLeggingsIcon;
    public Image currentBootsIcon;

    public ItemUI currentSelectedItem;
    private ItemUI lastSelectedItem;   //记录上一次选中的物品，用于取消高亮

    [Header("攻击提示")]
    public GameObject attackHintUI;
    public KeyCode attackKey = KeyCode.Mouse1;

    [SerializeField] private GameObject inventoryPanel;
    private PlayerInputActions inputActions;

    public static bool IsInventoryOpen { get; private set; }

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

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        IsInventoryOpen = false;

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
            InputManager.Instance.SwitchToInventory();
            SelectFirstItem();
        }
        else
        {
            // 关闭背包时，取消当前高亮并清空记录
            if (currentSelectedItem != null)
            {
                currentSelectedItem.SetHighlight(false);
                currentSelectedItem = null;
                lastSelectedItem = null;
            }
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

            if (currentSelectedItem != null)
            {
                EventSystem.current.SetSelectedGameObject(currentSelectedItem.gameObject);
            }
            return;
        }

        ToggleInventory();
    }

    private void HandleSubmit()
    {
        if (itemDetail.gameObject.activeSelf)
        {
            itemDetail.OnUseButtonClick();
        }
        else
        {
            OnItemClick(currentSelectedItem.itemSO, currentSelectedItem);
        }
    }

    private void CheckAndShowAttackHint()
    {
        if (hasShownAttackHint) return;

        if (WeaponEquipmentManager.Instance?.GetCurrentWeapon() != null)
        {
            if (attackHintUI != null)
            {
                attackHintUI.SetActive(true);
                Debug.Log("显示攻击提示：玩家已经持有武器");
            }
        }
    }

    public void ResetAttackHint()
    {
        hasShownAttackHint = false;
        if (attackHintUI != null)
            attackHintUI.SetActive(false);
    }

    public void OnInventoryButtonClick()
    {
        ToggleInventory();
    }

    public void AddItem(ItemSO itemSO)
    {
        if (itemSO.IsStackable())
        {
            ItemUI existingUI = FindItemUI(itemSO);
            if (existingUI != null)
            {
                existingUI.UpdateAmountDisplay();
                return;
            }
        }

        GameObject itemGo = Instantiate(itemPrefab);
        itemGo.transform.SetParent(content.transform);
        ItemUI itemUI = itemGo.GetComponent<ItemUI>();
        itemUI.InitItem(itemSO);
    }

    private ItemUI FindItemUI(ItemSO targetItem)
    {
        if (targetItem == null) return null;

        foreach (Transform child in content.transform)
        {
            if (child == null) continue;

            ItemUI itemUI = child.GetComponent<ItemUI>();
            if (itemUI != null && itemUI.itemSO != null)
            {
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

        // 切换高亮
        SwitchHighlight(itemUI);
    }

    public void OnItemUse(ItemSO itemSO, ItemUI itemUI)
    {
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            itemSO.amount -= 1;
            itemUI.UpdateAmountDisplay();
            ItemUsageHandler.Instance.UseItem(itemSO);
            currentSelectedItem = null;

            // 数量减少但物品仍存在，重新高亮当前物品
            SwitchHighlight(itemUI);
            EventSystem.current.SetSelectedGameObject(itemUI.gameObject);
            Debug.Log("使用后重新选中当前物品（数量减少了但还在）");
        }
        else
        {
            GameObject destroyedItem = itemUI.gameObject;

            destroyedItem.transform.SetParent(null);
            InventoryManager.Instance.itemList.Remove(itemSO);

            // 如果被销毁的物品正好是当前高亮的，需要清除高亮记录
            if (currentSelectedItem == itemUI || lastSelectedItem == itemUI)
            {
                if (currentSelectedItem != null) currentSelectedItem.SetHighlight(false);
                currentSelectedItem = null;
                lastSelectedItem = null;
            }

            SelectFirstItem();

            ItemUsageHandler.Instance.UseItem(itemSO);
            Destroy(destroyedItem);
            Debug.Log("重新选中第一个物品（因为当前物品被销毁了）");
        }
    }

    // 新增：统一的高亮切换方法
    private void SwitchHighlight(ItemUI newSelected)
    {
        if (newSelected == null) return;

        // 如果选中的是同一个物品，不需要重复操作
        if (currentSelectedItem == newSelected)
            return;

        // 取消上一个物品的高亮
        if (lastSelectedItem != null)
            lastSelectedItem.SetHighlight(false);

        // 设置新高亮
        newSelected.SetHighlight(true);

        // 更新记录
        currentSelectedItem = newSelected;
        lastSelectedItem = newSelected;
    }

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

    private void UpdateIcon(ref Image iconSlot, Sprite icon)
    {
        if (iconSlot != null)
        {
            iconSlot.sprite = icon;
            iconSlot.enabled = icon != null;
        }
    }

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
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemSO item in InventoryManager.Instance.itemList)
        {
            if (item == null) continue;

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }

    public void SelectFirstItem()
    {
        if (content == null || content.transform.childCount == 0)
        {
            Debug.LogWarning("[Inventory] content 中没有物品，无法选中");
            return;
        }

        Selectable firstSelectable = content.transform.GetChild(0).GetComponent<Selectable>();

        if (firstSelectable != null && firstSelectable.interactable)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);

            ItemUI firstItemUI = firstSelectable.GetComponent<ItemUI>();
            SwitchHighlight(firstItemUI);   // 使用统一的高亮切换

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
                SwitchHighlight(itemUI);   // 键盘/手柄导航时切换高亮

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