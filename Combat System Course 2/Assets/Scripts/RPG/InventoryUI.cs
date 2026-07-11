using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Video;


public class InventoryUI : MonoBehaviour
{
    private enum InventoryFocus
    {
        Items,
        Equipment
    }
    [Header("区域隔离 (CanvasGroup)")]
    public CanvasGroup itemsCanvasGroup;     // 物品栏的父级 CanvasGroup
    public CanvasGroup equipmentCanvasGroup; // 装备栏的父级 CanvasGroup
    public Transform itemsSwitchButton;
    public Transform equipmentSwitchButton;
    private Vector3 originV3=new Vector3(1,1,1);
    private Vector3 selectV3=new Vector3(1.2f,1.2f,1.2f);



    public static InventoryUI Instance { get; private set; }
    private InventoryFocus currentFocus = InventoryFocus.Items;
    public GameObject content;
    public GameObject itemPrefab;
    public ItemDetailUI itemDetail;

    // ItemSO → ItemUI 快速查找，避免 foreach 轮询
    private Dictionary<ItemSO, ItemUI> itemUIMap = new Dictionary<ItemSO, ItemUI>();

    [Header("装备槽位")]
    [SerializeField] private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
    public ItemUI currentSelectedItem;
    private ItemUI lastSelectedItem;   //记录上一次选中的物品，用于取消高亮
    private GameObject lastSelectedGO;
    private EquipmentSlotUI lastHighlightedSlot; // 上一次高亮模型的装备槽

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
        equipmentSwitchButton.localScale=originV3;
        itemsSwitchButton.localScale=selectV3;
    }



    private void OnEnable()
    {
        InputManager.Instance.OnToggleInventory += ToggleInventory;
        InputManager.Instance.OnUISubmit += HandleSubmit;
        InputManager.Instance.OnUICancel += HandleCancel;
        InputManager.Instance.OnUISwitchLeft += SwitchToEquipment;
        InputManager.Instance.OnUISwitchRight += SwitchToItems;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnToggleInventory -= ToggleInventory;
        InputManager.Instance.OnUISubmit -= HandleSubmit;
        InputManager.Instance.OnUICancel -= HandleCancel;
        InputManager.Instance.OnUISwitchLeft -= SwitchToEquipment;
        InputManager.Instance.OnUISwitchRight -= SwitchToItems;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }

    private void OnBagPerformed(InputAction.CallbackContext ctx)
    {
        ToggleInventory();
    }
    public Selectable GetFirstEquipmentSlot()
    {
        return equipmentSlots.Count > 0
            ? equipmentSlots[0].GetComponentInChildren<Selectable>()
            : null;
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
        
            //每次打开背包，重置为物品栏状态
            currentFocus = InventoryFocus.Items;
            if (itemsCanvasGroup != null) itemsCanvasGroup.interactable = true;
            if (equipmentCanvasGroup != null) equipmentCanvasGroup.interactable = false;

        SelectFirstItem();
        Debug.Log($"[DEBUG] 选中物体: {EventSystem.current.currentSelectedGameObject?.name ?? "NULL"}");
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
            ClearEquipmentModelHighlight();
            InputManager.Instance.SwitchToPlayer();
            CheckAndShowAttackHint();
        }
    }

    private void HandleCancel()
    {
        // detail 面板由 UI_ItemDetail map 独立处理 Cancel，这里只管背包
        ToggleInventory();
    }
    private EquipmentSlotUI GetSlot(ItemSO item)
    {
        if (item == null) return null;

        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slot = equipmentSlots[i];

            if (item.itemType == ItemType.Weapon &&
                slot.itemType == ItemType.Weapon)
            {
                return slot;
            }

            if (item.itemType == ItemType.Armor &&
                slot.itemType == ItemType.Armor &&
                item is ArmorSO armor &&
                slot.armorType == armor.armorType)
            {
                return slot;
            }
        }

        return null;
    }
   private void HandleSubmit()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        Debug.Log($"[DEBUG HandleSubmit] 当前选中: {selected?.name ?? "NULL"}");
        if (selected == null) return;

        // 1. 物品栏逻辑
        ItemUI itemUI = selected.GetComponent<ItemUI>();
        if (itemUI != null)
        {
            OnItemClick(itemUI.itemSO, itemUI);
            return;
        }

        // 3. 装备槽逻辑
        EquipmentSlotUI slot = selected.GetComponentInParent<EquipmentSlotUI>();
        if (slot != null)
        {
            slot.OnSubmit();
            // 卸装后维持选中装备槽，防止跳回物品栏
            EventSystem.current.SetSelectedGameObject(slot.gameObject);
            return;
        }

        // 4. 普通按钮
        Button button = selected.GetComponent<Button>();
        if (button != null) button.onClick.Invoke();
    }    
   private void SwitchToEquipment()
    {
        if (!IsInventoryOpen) return;

        currentFocus = InventoryFocus.Equipment;
        itemsSwitchButton.localScale = originV3;
        equipmentSwitchButton.localScale = selectV3;

        // 区域交互隔离
        if (equipmentCanvasGroup != null) equipmentCanvasGroup.interactable = true;
        if (itemsCanvasGroup != null) itemsCanvasGroup.interactable = false;

        // 清除物品栏高亮
        ClearAllHighlights();

        // 装备槽Selectable已设Vertical模式，Unity自动处理上下导航
        // 左右为null，切换区域只能用肩键
        var sorted = GetSortedSlots();

        Selectable target = GetPrioritySlot(sorted);
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
   private void SwitchToItems()
    {
        if (!IsInventoryOpen) return;

        currentFocus = InventoryFocus.Items;
        equipmentSwitchButton.localScale=originV3;
        itemsSwitchButton.localScale=selectV3;
        
        // 【新增】打开物品区交互，关闭装备区交互
        if (itemsCanvasGroup != null) itemsCanvasGroup.interactable = true;
        if (equipmentCanvasGroup != null) equipmentCanvasGroup.interactable = false;

        SelectFirstItem();
        ClearEquipmentModelHighlight();
    }
private void ClearAllHighlights()
    {
        if (currentSelectedItem != null)
            currentSelectedItem.SetHighlight(false);
        
        currentSelectedItem = null;
        lastSelectedItem = null;
        
        // 如果装备槽也需要逻辑清除，可以在这里遍历清除
    }

    private void UpdateEquipmentModelHighlight(EquipmentSlotUI slot)
    {
        if (slot == null) return;

        // 先清除上一个槽位的模型高亮
        ClearEquipmentModelHighlight();

        // 设置新的模型高亮
        var display = BackpackCharacterDisplay.Instance;
        if (display == null) return;

        if (slot.itemType == ItemType.Weapon)
            display.HighlightWeapon();
        else if (slot.itemType == ItemType.Armor)
            display.HighlightEquippedArmor(slot.armorType);

        lastHighlightedSlot = slot;
    }

    private void ClearEquipmentModelHighlight()
    {
        if (lastHighlightedSlot == null) return;

        var display = BackpackCharacterDisplay.Instance;
        if (display == null) return;

        if (lastHighlightedSlot.itemType == ItemType.Weapon)
            display.ClearWeaponHighlight();
        else if (lastHighlightedSlot.itemType == ItemType.Armor)
            display.ClearArmorHighlight(lastHighlightedSlot.armorType);

        lastHighlightedSlot = null;
    }

private List<EquipmentSlotUI> GetSortedSlots()
    {
        var list = new List<EquipmentSlotUI>(equipmentSlots);
        list.Sort((a, b) => {
            int GetOrder(EquipmentSlotUI s) {
                if (s.itemType == ItemType.Weapon) return 3;
                return s.armorType switch {
                    ArmorType.Helmet => 0, ArmorType.Chestplate => 1,
                    ArmorType.Gauntlets => 2, ArmorType.Leggings => 4,
                    ArmorType.Boots => 5, _ => 99
                };
            }
            return GetOrder(a).CompareTo(GetOrder(b));
        });
        return list;
    }
    private Selectable GetPrioritySlot(List<EquipmentSlotUI> sorted)
    {
        foreach (var slot in sorted)
            if (slot.iconImage != null && slot.iconImage.enabled) 
                return slot.GetComponentInChildren<Selectable>();
        return sorted[0].GetComponentInChildren<Selectable>();
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
        itemUIMap[itemSO] = itemUI;
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
        // detail 打开后由 ItemDetailUI.OnEnable 切换到 UI_ItemDetail map 并选中 useButton

        // 切换高亮
        SwitchHighlight(itemUI);
    }

    public void OnItemUse(ItemSO itemSO, ItemUI itemUI)
    {
        if (itemSO.IsStackable() && itemSO.amount > 1)
        {
            if (!ItemUsageHandler.Instance.UseItem(itemSO))
                return;

            itemSO.amount -= 1;
            itemUI.UpdateAmountDisplay();
            currentSelectedItem = null;

            // 数量减少但物品仍存在，重新高亮当前物品
            SwitchHighlight(itemUI);
            EventSystem.current.SetSelectedGameObject(itemUI.gameObject);
            Debug.Log("使用后重新选中当前物品（数量减少了但还在）");
        }
        else
        {
            if (!ItemUsageHandler.Instance.UseItem(itemSO))
                return;

            GameObject destroyedItem = itemUI.gameObject;

            destroyedItem.transform.SetParent(null);
            itemUIMap.Remove(itemSO);
            InventoryManager.Instance.RemoveItem(itemSO, 1, updateUI: false);

            // 如果被销毁的物品正好是当前高亮的，需要清除高亮记录
            if (currentSelectedItem == itemUI || lastSelectedItem == itemUI)
            {
                if (currentSelectedItem != null) currentSelectedItem.SetHighlight(false);
                currentSelectedItem = null;
                lastSelectedItem = null;
            }

            SelectFirstItem();

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
        var slot = GetSlot(item);
        if (slot == null)
        {
            Debug.LogWarning($"未找到对应装备槽: {item.nameOfItem}");
            return;
        }

        slot.UpdateIcon(item.icon);
    }

    public void ClearEquipmentIcon(ItemType itemType, ArmorType armorType = ArmorType.NotArmor)
    {
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slot = equipmentSlots[i];

            if (slot.itemType != itemType) continue;

            if (itemType == ItemType.Armor && slot.armorType != armorType)
                continue;

            slot.UpdateIcon(null);
            return;
        }
    }

   

    public void UpdateInventoryUI()
    {
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }
        itemUIMap.Clear();

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
                itemUIMap[item] = itemUI;
            }
            else
            {
                Debug.LogError("物品预制体缺少ItemUI脚本");
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }

    /// <summary> 刷新指定物品的 QuickLight 指示器 (O(1) 字典查找) </summary>
    public void RefreshQuickLightForItem(ItemSO item)
    {
        if (item == null) return;
        if (itemUIMap.TryGetValue(item, out ItemUI itemUI))
            itemUI.UpdateQuickLight();
    }

    /// <summary> 刷新全部物品的 QuickLight（读档后调用） </summary>
    public void RefreshAllQuickLights()
    {
        foreach (var kv in itemUIMap)
            kv.Value.UpdateQuickLight();
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
    if (EventSystem.current == null) return;

    GameObject selectedGO = EventSystem.current.currentSelectedGameObject;

    // 只在选中对象变化时打印
    if (selectedGO != lastSelectedGO)
    {
        lastSelectedGO = selectedGO;

        if (selectedGO != null)
        {
            if (currentFocus == InventoryFocus.Items)
            {
                ItemUI itemUI = selectedGO.GetComponent<ItemUI>();
                if (itemUI != null && currentSelectedItem != itemUI)
                {
                    SwitchHighlight(itemUI);
                    string itemName = itemUI.itemSO != null ? itemUI.itemSO.nameOfItem : "未知物品";
                    Debug.Log($"[Navigation Debug] 当前选中物品: {itemName}");
                }
            }
            else if (currentFocus == InventoryFocus.Equipment)
            {
                Debug.Log($"[Navigation Debug] 当前选中装备槽: {selectedGO.name}");

                EquipmentSlotUI slot = selectedGO.GetComponentInParent<EquipmentSlotUI>();
                if (slot != null)
                    UpdateEquipmentModelHighlight(slot);
            }
        }
        else
        {
            Debug.Log("[Navigation Debug] 当前没有选中任何物品");
        }
    }
}
}