using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickItemBar : MonoBehaviour
{
    [System.Serializable]
    public class QuickSlot
    {
        public ItemSO item;
        public int count;
    }

    [System.Serializable]
    public class SlotView
    {
        public GameObject root;
        public Image icon;
        public TMP_Text countText;
        [HideInInspector] public CanvasGroup canvasGroup;
    }

    [Header("数据")]
    [SerializeField] private QuickSlot[] slots = new QuickSlot[4];

    [Header("UI 引用")]
    [SerializeField] private SlotView leftSlot;
    [SerializeField] private SlotView centerSlot;
    [SerializeField] private SlotView rightSlot;
    [SerializeField] private GameObject background;
    [SerializeField] private Animator indicatorAnimator;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private GameObject itemNameParent;
    private CanvasGroup backgroundCG;

    [Header("缩放")]
    [SerializeField] private float expandedCenterScale = 1.2f;

    [Header("透明度")]
    [SerializeField] private float dimAlpha = 0.3f;
    [SerializeField] private float brightAlpha = 0.8f;
    [SerializeField] private float sideCollapsedAlpha = 0.5f;
    [SerializeField] private float sideExpandedAlpha = 1f;

    private int selectedIndex = 0;
    private bool isExpanded;

    private void Start()
    {
        if (background != null)
            background.TryGetComponent(out backgroundCG);

        leftSlot.canvasGroup = leftSlot.root?.GetComponent<CanvasGroup>();
        centerSlot.canvasGroup = centerSlot.root?.GetComponent<CanvasGroup>();
        rightSlot.canvasGroup = rightSlot.root?.GetComponent<CanvasGroup>();

        if (itemNameParent != null)
            itemNameParent.SetActive(false);

        InputManager.Instance.OnQuickItemModifierChanged += OnModifierChanged;
        InputManager.Instance.OnQuickItemNavigate += OnNavigate;

        SetExpanded(false);
        RefreshView();
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnQuickItemModifierChanged -= OnModifierChanged;
            InputManager.Instance.OnQuickItemNavigate -= OnNavigate;
        }
    }

    private void OnModifierChanged(bool held)
    {
        SetExpanded(held);

        if (indicatorAnimator != null)
            indicatorAnimator.SetTrigger(held ? "Enter" : "Exit");

        if (itemNameParent != null)
            itemNameParent.SetActive(held);
    }

    private void OnNavigate(int direction)
    {
        if (!isExpanded) return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, slots.Length - 1);
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            RefreshView();
        }
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (backgroundCG != null)
            backgroundCG.alpha = expanded ? brightAlpha : dimAlpha;

        RefreshView();
    }

    private void RefreshView()
    {
        // 中间始终显示
        float centerS = isExpanded ? expandedCenterScale : 1f;
        ApplySlot(centerSlot, selectedIndex, centerS);

        // 道具名
        if (itemNameText != null)
        {
            var centerData = slots[selectedIndex];
            itemNameText.text = centerData.item != null ? centerData.item.nameOfItem : "";
        }

        // 左边：始终显示，alpha 控制显眼程度
        bool hasLeft = selectedIndex > 0;
        ApplySlot(leftSlot, hasLeft ? selectedIndex - 1 : selectedIndex, 1f);
        if (hasLeft)
            SetSlotAlpha(leftSlot, isExpanded ? sideExpandedAlpha : sideCollapsedAlpha);
        else
            SetSlotAlpha(leftSlot, 0f);

        // 右边：始终显示，alpha 控制显眼程度
        bool hasRight = selectedIndex < slots.Length - 1;
        ApplySlot(rightSlot, hasRight ? selectedIndex + 1 : selectedIndex, 1f);
        if (hasRight)
            SetSlotAlpha(rightSlot, isExpanded ? sideExpandedAlpha : sideCollapsedAlpha);
        else
            SetSlotAlpha(rightSlot, 0f);
    }

    private void ApplySlot(SlotView view, int dataIndex, float scale)
    {
        QuickSlot data = slots[dataIndex];
        if (data.item != null)
        {
            view.icon.sprite = data.item.icon;
            view.icon.enabled = true;
            view.countText.text = data.count > 1 ? data.count.ToString() : "";
        }
        else
        {
            view.icon.sprite = null;
            view.icon.enabled = false;
            view.countText.text = "";
        }

        view.root.transform.localScale = Vector3.one * scale;
    }

    private void SetSlotAlpha(SlotView view, float alpha)
    {
        if (view.canvasGroup != null)
            view.canvasGroup.alpha = alpha;
    }

    // === 外部接口 ===

    /// <summary> 给指定槽位设置道具 </summary>
    public void SetSlot(int index, ItemSO item, int count)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index].item = item;
        slots[index].count = count;
        RefreshView();
    }

    /// <summary> 当前选中的 ItemSO（使用道具时读这个） </summary>
    public ItemSO CurrentItem => slots[selectedIndex].item;

    /// <summary> 当前选中槽位索引 </summary>
    public int SelectedIndex => selectedIndex;

    /// <summary> 是否处于展开态 </summary>
    public bool IsExpanded => isExpanded;
}
