using System.Collections;
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

    public static QuickItemBar Instance { get; private set; }

    [Header("数据")]
    [SerializeField] private QuickSlot[] slots = new QuickSlot[7];

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

    [Header("滑动动效")]
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine slideCoroutine;
    private float slotSpacing;
    private Vector2 leftOrigPos, centerOrigPos, rightOrigPos;
    private bool isSliding;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

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

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemRemoved += OnItemRemovedFromInventory;

        SetExpanded(false);
        RefreshView();

        // 缓存三个槽的原始 anchoredPosition，用于滑动动效
        CacheOriginalPositions();
    }

    private void CacheOriginalPositions()
    {
        if (leftSlot.root != null)
            leftOrigPos = ((RectTransform)leftSlot.root.transform).anchoredPosition;
        if (centerSlot.root != null)
            centerOrigPos = ((RectTransform)centerSlot.root.transform).anchoredPosition;
        if (rightSlot.root != null)
            rightOrigPos = ((RectTransform)rightSlot.root.transform).anchoredPosition;

        slotSpacing = centerOrigPos.x - leftOrigPos.x;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnQuickItemModifierChanged -= OnModifierChanged;
            InputManager.Instance.OnQuickItemNavigate -= OnNavigate;
        }
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnItemRemoved -= OnItemRemovedFromInventory;
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
        if (!isExpanded || isSliding) return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, slots.Length - 1);
        if (newIndex != selectedIndex)
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideCoroutine(direction, newIndex));
        }
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (backgroundCG != null)
            backgroundCG.alpha = expanded ? brightAlpha : dimAlpha;

        RefreshView();
    }

    public void RefreshView()
    {
        // 中间始终显示
        float centerS = isExpanded ? expandedCenterScale : 1.1f;//这个是常态
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

    private IEnumerator SlideCoroutine(int direction, int newIndex)
    {
        isSliding = true;

        RectTransform leftRT = leftSlot.root?.GetComponent<RectTransform>();
        RectTransform centerRT = centerSlot.root?.GetComponent<RectTransform>();
        RectTransform rightRT = rightSlot.root?.GetComponent<RectTransform>();

        // direction>0 滚轮向下 → 内容左移；direction<0 滚轮向上 → 内容右移
        float slideDistance = -direction * slotSpacing;
        Vector2 offset = new Vector2(slideDistance, 0f);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            Vector2 current = Vector2.LerpUnclamped(Vector2.zero, offset, t);

            if (leftRT != null) leftRT.anchoredPosition = leftOrigPos + current;
            if (centerRT != null) centerRT.anchoredPosition = centerOrigPos + current;
            if (rightRT != null) rightRT.anchoredPosition = rightOrigPos + current;

            yield return null;
        }

        // 复位
        if (leftRT != null) leftRT.anchoredPosition = leftOrigPos;
        if (centerRT != null) centerRT.anchoredPosition = centerOrigPos;
        if (rightRT != null) rightRT.anchoredPosition = rightOrigPos;

        selectedIndex = newIndex;
        RefreshView();

        isSliding = false;
        slideCoroutine = null;
    }

    private void ApplySlot(SlotView view, int dataIndex, float scale)
    {
        QuickSlot data = slots[dataIndex];
        if (data.item != null)
        {
            view.icon.sprite = data.item.icon;
            view.icon.enabled = true;
            int displayCount = data.item.IsStackable() ? data.item.amount : data.count;
            view.countText.text = displayCount > 1 ? displayCount.ToString() : "";
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

    /// <summary> 读取槽位数据 </summary>
    public QuickSlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Length)
            return new QuickSlot();
        return slots[index];
    }

    /// <summary> 给指定槽位设置道具 </summary>
    public void SetSlot(int index, ItemSO item, int count)
    {
        if (index < 0 || index >= slots.Length) return;
        slots[index].item = item;
        slots[index].count = count;
        RefreshView();
    }

    /// <summary> 检查物品是否已在任意快捷槽位中 </summary>
    public bool HasItem(ItemSO item)
    {
        if (item == null) return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
                return true;
        }
        return false;
    }

    /// <summary> 清除指定物品所在的快捷槽位 </summary>
    public void ClearSlotByItem(ItemSO item)
    {
        if (item == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
            {
                slots[i].item = null;
                slots[i].count = 0;
                RefreshView();
                return;
            }
        }
    }

    private void OnItemRemovedFromInventory(ItemSO item)
    {
        ClearSlotByItem(item);
    }

    /// <summary> 当前选中的 ItemSO（使用道具时读这个） </summary>
    public ItemSO CurrentItem => slots[selectedIndex].item;

    /// <summary> 当前选中槽位索引 </summary>
    public int SelectedIndex => selectedIndex;

    /// <summary> 是否处于展开态 </summary>
    public bool IsExpanded => isExpanded;
}
