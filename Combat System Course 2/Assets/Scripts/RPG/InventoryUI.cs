using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class InventoryUI : MonoBehaviour
{
    private enum InventoryFocus
    {
        Items,
        Equipment
    }
    [Header("区域隔离 (CanvasGroup)")]
    public CanvasGroup itemsCanvasGroup;     // 拖入物品栏的父级 CanvasGroup
    public CanvasGroup equipmentCanvasGroup; // 拖入装备栏的父级 CanvasGroup
    

    public static InventoryUI Instance { get; private set; }
    private InventoryFocus currentFocus = InventoryFocus.Items;
    public GameObject content;
    public GameObject itemPrefab;
    public ItemDetailUI itemDetail;
    
    [Header("装备槽位")]
    [SerializeField] private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
    public ItemUI currentSelectedItem;
    private ItemUI lastSelectedItem;   //记录上一次选中的物品，用于取消高亮
    private GameObject lastSelectedGO;  

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
            ? equipmentSlots[0].GetComponent<Selectable>()
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
                slot.armorType == item.armorType)
            {
                return slot;
            }
        }

        return null;
    }
    private void HandleSubmit()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null) return;

        // 👉 如果当前打开的是详情UI
        if (itemDetail.gameObject.activeSelf)
        {
            itemDetail.OnUseButtonClick();
            return;
        }

        // 👉 1. 物品
        ItemUI itemUI = selected.GetComponent<ItemUI>();
        if (itemUI != null)
        {
            OnItemClick(itemUI.itemSO, itemUI);
            return;
        }

        // 👉 2. 装备槽（你后面要实现）
        EquipmentSlotUI slot = selected.GetComponent<EquipmentSlotUI>();
        if (slot != null)
        {
            slot.OnSubmit();
            return;
        }

        Debug.Log("当前选中对象没有可处理的Submit逻辑: " + selected.name);
    }
    private void SwitchToEquipment()
    {
        if (!IsInventoryOpen) return;

        currentFocus = InventoryFocus.Equipment;

        // 【新增】打开装备区交互，关闭物品区交互（阻断 D-pad 乱串）
        if (equipmentCanvasGroup != null) equipmentCanvasGroup.interactable = true;
        if (itemsCanvasGroup != null) itemsCanvasGroup.interactable = false;

        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetHighlight(false);
            currentSelectedItem = null;
            lastSelectedItem = null;
        }

        var firstSlot = GetFirstEquipmentSlot();
        if (firstSlot != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSlot.gameObject);
            Debug.Log("切换到装备槽: " + firstSlot.name);
        }
    }
   private void SwitchToItems()
    {
        if (!IsInventoryOpen) return;

        currentFocus = InventoryFocus.Items;

        // 【新增】打开物品区交互，关闭装备区交互
        if (itemsCanvasGroup != null) itemsCanvasGroup.interactable = true;
        if (equipmentCanvasGroup != null) equipmentCanvasGroup.interactable = false;

        SelectFirstItem();
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
            }
        }
        else
        {
            Debug.Log("[Navigation Debug] 当前没有选中任何物品");
        }
    }
}
}