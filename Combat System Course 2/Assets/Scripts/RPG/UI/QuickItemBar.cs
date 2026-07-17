using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private GameObject collapsedBackground;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private GameObject itemNameParent;
    [SerializeField] private TMP_Text centerCountText;
    [SerializeField] private GameObject useInform;   // 中间槽为空时隐藏的使用提示


    [Header("滑动动效")]
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int selectedIndex = 0;
    private bool isExpanded;
    private readonly List<int> visibleIndices = new List<int>(); // 展开时可见的非空槽位索引

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
        leftSlot.canvasGroup = leftSlot.root?.GetComponent<CanvasGroup>();
        centerSlot.canvasGroup = centerSlot.root?.GetComponent<CanvasGroup>();
        rightSlot.canvasGroup = rightSlot.root?.GetComponent<CanvasGroup>();


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

    }

    private void OnNavigate(int direction)
    {
        if (!isExpanded || isSliding || visibleIndices.Count == 0) return;

        int vi = visibleIndices.IndexOf(selectedIndex);
        if (vi < 0) return;

        int nextVi = Mathf.Clamp(vi + direction, 0, visibleIndices.Count - 1);
        int newIndex = visibleIndices[nextVi];
        if (newIndex != selectedIndex)
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideCoroutine(direction, newIndex));
        }
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        // 背景：只在展开时显示
        if (background != null)
            background.SetActive(expanded);

        // 收起时的背景：只在收起时显示
        if (collapsedBackground != null)
            collapsedBackground.SetActive(!expanded);

        if (isExpanded)
        {
            RebuildVisibleIndices();
            // 展开时如果当前选中槽位为空，跳到第一个有东西的槽位
            if (visibleIndices.Count > 0 && visibleIndices.IndexOf(selectedIndex) < 0)
                selectedIndex = visibleIndices[0];
        }

        // 左右槽位：只在展开时显示
        if (leftSlot.root != null)
            leftSlot.root.SetActive(expanded);
        if (rightSlot.root != null)
            rightSlot.root.SetActive(expanded);

        RefreshView();
    }

    /// <summary> 重建展开时可见的槽位列表：只留有东西的；全空则默认只留 1 号槽 </summary>
    private void RebuildVisibleIndices()
    {
        visibleIndices.Clear();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null)
                visibleIndices.Add(i);
        }
        if (visibleIndices.Count == 0)
            visibleIndices.Add(0);
    }

    public void RefreshView()
    {
        // 中间
        ApplySlot(centerSlot, selectedIndex);

        // midslot 没东西时隐藏使用提示
        if (useInform != null)
            useInform.SetActive(slots[selectedIndex].item != null);

        // 中间数量
        if (centerCountText != null)
        {
            var centerData = slots[selectedIndex];
            if (centerData.item != null)
            {
                int displayCount = centerData.item.IsStackable() ? centerData.item.amount : centerData.count;
                centerCountText.text = displayCount > 0 ? displayCount.ToString() : "";
            }
            else
            {
                centerCountText.text = "";
            }
        }

        // 道具名
        if (itemNameText != null)
        {
            var centerData = slots[selectedIndex];
            itemNameText.text = centerData.item != null ? centerData.item.nameOfItem : "";
        }

        // 左右邻居从 visibleIndices 里取，跳过空槽
        int vi = visibleIndices.IndexOf(selectedIndex);

        // 左边
        if (leftSlot.root != null && leftSlot.root.activeSelf)
        {
            bool hasLeft = vi > 0;
            ApplySlot(leftSlot, hasLeft ? visibleIndices[vi - 1] : selectedIndex);
            SetSlotAlpha(leftSlot, hasLeft ? 1f : 0f);
        }

        // 右边
        if (rightSlot.root != null && rightSlot.root.activeSelf)
        {
            bool hasRight = vi >= 0 && vi < visibleIndices.Count - 1;
            ApplySlot(rightSlot, hasRight ? visibleIndices[vi + 1] : selectedIndex);
            SetSlotAlpha(rightSlot, hasRight ? 1f : 0f);
        }
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

    private void ApplySlot(SlotView view, int dataIndex)
    {
        QuickSlot data = slots[dataIndex];
        if (data.item != null)
        {
            view.icon.sprite = data.item.icon;
            view.icon.enabled = true;
        }
        else
        {
            view.icon.sprite = null;
            view.icon.enabled = false;
        }
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
        if (isExpanded)
            RebuildVisibleIndices();
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

    /// <summary> 清除指定物品所在的快捷槽位（物品用光时自动跳到下一个有东西的槽）</summary>
    public void ClearSlotByItem(ItemSO item)
    {
        if (item == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
            {
                slots[i].item = null;
                slots[i].count = 0;
                RebuildVisibleIndices();
                if (selectedIndex == i)
                    SnapToNextValid(i);
                RefreshView();
                return;
            }
        }
    }

    /// <summary> 跳到 fromIndex 之后第一个有东西的槽；后面没有就取第一个可见槽（全空时回到 1 号槽）</summary>
    private void SnapToNextValid(int fromIndex)
    {
        foreach (int idx in visibleIndices)
        {
            if (idx > fromIndex && slots[idx].item != null)
            {
                selectedIndex = idx;
                return;
            }
        }
        selectedIndex = visibleIndices[0];
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