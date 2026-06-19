using UnityEngine;
using UnityEngine.EventSystems;

public class SetQuickUseUI : MonoBehaviour
{
    [Header("槽位")]
    [SerializeField] private QuickUseSlotUI[] slots = new QuickUseSlotUI[7];

    [Header("导航隔离")]
    [SerializeField] private ItemDetailUI itemDetailUI;

    private ItemSO pendingItemSO;

    private void Start()
    {
        this.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        InputManager.Instance.OnQuickUseConfirm += OnConfirm;
        InputManager.Instance.OnQuickUseCancel += OnCancel;
        InputManager.Instance.SwitchToQuickUseBar();

        // 阻止 EventSystem 导航到背后的面板
        SetBackgroundInteractable(false);

        // 鼠标点击支持
        foreach (var slot in slots)
        {
            if (slot != null)
                slot.onClick += OnSlotClicked;
        }
    }

    private void OnDisable()
    {
        InputManager.Instance.OnQuickUseConfirm -= OnConfirm;
        InputManager.Instance.OnQuickUseCancel -= OnCancel;

        foreach (var slot in slots)
        {
            if (slot != null)
                slot.onClick -= OnSlotClicked;
        }

        // 恢复背后面板的交互
        SetBackgroundInteractable(true);
    }

    private void SetBackgroundInteractable(bool interactable)
    {
        // 从 itemDetailUI 的 GameObject 上获取 CanvasGroup
        if (itemDetailUI != null)
        {
            var cg = itemDetailUI.GetComponent<CanvasGroup>();
            if (cg != null) cg.interactable = interactable;
        }

        if (InventoryUI.Instance != null)
        {
            if (InventoryUI.Instance.itemsCanvasGroup != null)
                InventoryUI.Instance.itemsCanvasGroup.interactable = interactable;
            if (InventoryUI.Instance.equipmentCanvasGroup != null)
                InventoryUI.Instance.equipmentCanvasGroup.interactable = interactable;
        }
    }

    /// <summary> 从 ItemDetailUI 调用，传入要设置的物品 </summary>
    public void Open(ItemSO itemSO)
    {
        pendingItemSO = itemSO;
        SyncFromQuickItemBar();
        this.gameObject.SetActive(true);  // 先激活，否则子槽位无法接收选中
        SelectDefault();
    }

    private void SyncFromQuickItemBar()
    {
        var bar = QuickItemBar.Instance;
        if (bar == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var data = bar.GetSlot(i);
            slots[i].SetIcon(data.item?.icon);
        }
    }

    private void SelectDefault()
    {
        var bar = QuickItemBar.Instance;
        // 优先空槽，否则第一个
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (bar != null && bar.GetSlot(i).item == null)
            {
                EventSystem.current.SetSelectedGameObject(slots[i].gameObject);
                return;
            }
        }
        if (slots.Length > 0 && slots[0] != null)
            EventSystem.current.SetSelectedGameObject(slots[0].gameObject);
    }

    private void OnConfirm()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;

        QuickUseSlotUI slotUI = selected.GetComponent<QuickUseSlotUI>();
        if (slotUI == null) return;

        QuickItemBar.Instance?.SetSlot(slotUI.slotIndex, pendingItemSO, 1);

        Close();
    }

    private void OnSlotClicked(QuickUseSlotUI slotUI)
    {
        if (slotUI == null) return;
        EventSystem.current.SetSelectedGameObject(slotUI.gameObject);
        QuickItemBar.Instance?.SetSlot(slotUI.slotIndex, pendingItemSO, 1);
        Close();
    }

    private void OnCancel()
    {
        Close();
    }

    private void Close()
    {
        EventSystem.current.SetSelectedGameObject(null);
        // 切回 detail 的输入 map
        InputManager.Instance.SwitchToItemDetail();
        // 刷新 detail 的快捷按钮状态
        itemDetailUI?.RefreshQuickSlotState();
        gameObject.SetActive(false);
    }
}
