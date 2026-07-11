using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 垂直菜单导航器：管理按钮列表的选中、alpha/位置动画。
/// 选中按钮 alpha=1 居中；相邻按钮 alpha 减半并偏移；超出可见范围的 SetActive(false)。
/// 不关心按钮数量和功能，只管理导航逻辑。
/// </summary>
public class VerticalMenuNavigator : MonoBehaviour
{
    [Header("动画参数")]
    [SerializeField] private float unselectedAlpha = 0.5f;
    [SerializeField] private float positionOffset = 25f;
    [SerializeField] private float animationDuration = 0.15f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("可见范围（选中项上下各可见几个）")]
    [SerializeField] private int visibleRange = 1;

    /// <summary> 选中索引变化时触发 </summary>
    public event Action<int> OnSelectionChanged;

    private List<Button> buttons = new List<Button>();
    private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
    private List<Vector2> originalPositions = new List<Vector2>();
    private List<(Button btn, UnityEngine.Events.UnityAction listener)> selectionListeners = new();
    private Dictionary<Button, Vector2> trueOriginals = new Dictionary<Button, Vector2>();
    private int selectedIndex = 0;
    private Coroutine animCoroutine;
    private bool isAnimating;

    public int SelectedIndex => selectedIndex;
    public int ButtonCount => buttons.Count;

    /// <summary> 当前选中的按钮 </summary>
    public Button CurrentButton
    {
        get
        {
            if (buttons.Count == 0 || selectedIndex < 0 || selectedIndex >= buttons.Count)
                return null;
            return buttons[selectedIndex];
        }
    }

    /// <summary>
    /// 设置按钮列表，会清空旧数据并重新初始化。
    /// 按钮顺序决定导航顺序：列表[0] 在最上面，列表[末尾] 在最下面。
    /// </summary>
    public void SetButtons(List<Button> newButtons)
    {
        bool clearAll = (newButtons == null || newButtons.Count == 0);

        // 先把旧按钮位置复位到真正原始位置（防止跨物品累加偏移），空列表时一并隐藏
        foreach (var (btn, _) in selectionListeners)
        {
            if (btn != null)
            {
                if (clearAll) btn.gameObject.SetActive(false);
                if (trueOriginals.TryGetValue(btn, out var orig))
                {
                    var rt = btn.transform as RectTransform;
                    if (rt != null) rt.anchoredPosition = orig;
                }
            }
        }

        // 清理旧的 selection-sync 监听器
        RemoveSelectionListeners();

        // 停止正在进行的动画
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
        isAnimating = false;

        buttons.Clear();
        canvasGroups.Clear();
        originalPositions.Clear();
        selectionListeners.Clear();

        if (clearAll)
        {
            selectedIndex = 0;
            return;
        }

        foreach (var btn in newButtons)
        {
            if (btn == null) continue;

            int capturedIndex = buttons.Count;
            buttons.Add(btn);

            // 收集或添加 CanvasGroup
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = btn.gameObject.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);

            // 缓存原始位置：初次遇到记录，后续复用缓存值
            var rt = btn.transform as RectTransform;
            Vector2 orig;
            if (!trueOriginals.TryGetValue(btn, out orig))
            {
                orig = rt != null ? rt.anchoredPosition : Vector2.zero;
                trueOriginals[btn] = orig;
            }
            originalPositions.Add(orig);

            // 注入 selection-sync 监听器（不干扰已有的功能性 onClick）
            UnityEngine.Events.UnityAction listener = () => OnButtonClicked(capturedIndex);
            selectionListeners.Add((btn, listener));
            btn.onClick.AddListener(listener);
        }

        // 重置选中为第一个
        selectedIndex = 0;
        RefreshVisuals(animate: false);
        OnSelectionChanged?.Invoke(selectedIndex);
    }

    /// <summary> 移除上次注入的 selection-sync 监听器 </summary>
    private void RemoveSelectionListeners()
    {
        foreach (var (btn, listener) in selectionListeners)
        {
            if (btn != null)
                btn.onClick.RemoveListener(listener);
        }
        selectionListeners.Clear();
    }

    /// <summary>
    /// 上下导航。direction: -1=上, +1=下。
    /// </summary>
    public void Navigate(int direction)
    {
        if (buttons.Count == 0 || isAnimating) return;

        int newIndex = Mathf.Clamp(selectedIndex + direction, 0, buttons.Count - 1);
        if (newIndex != selectedIndex)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateTransition(newIndex));
        }
    }

    /// <summary>
    /// 手动跳到指定索引（用于鼠标点击等外部输入同步）。
    /// </summary>
    public void JumpTo(int index)
    {
        if (buttons.Count == 0 || index < 0 || index >= buttons.Count) return;
        if (index == selectedIndex) return;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        isAnimating = false;

        selectedIndex = index;
        RefreshVisuals(animate: false);
        OnSelectionChanged?.Invoke(selectedIndex);
    }

    /// <summary>
    /// 触发当前选中按钮的 onClick。
    /// </summary>
    public void Submit()
    {
        if (CurrentButton != null)
            CurrentButton.onClick?.Invoke();
    }

    /// <summary>
    /// 鼠标点击按钮时自动同步选中索引（由注入的 selection-sync listener 调用）。
    /// </summary>
    private void OnButtonClicked(int index)
    {
        JumpTo(index);
    }

    /// <summary>
    /// 立即刷新所有按钮的 alpha、位置、active 状态。
    /// </summary>
    private void RefreshVisuals(bool animate)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            int dist = Mathf.Abs(i - selectedIndex);

            // 可视范围外 → 隐藏
            if (dist > visibleRange)
            {
                buttons[i].gameObject.SetActive(false);
                continue;
            }

            buttons[i].gameObject.SetActive(true);

            float targetAlpha = (dist == 0) ? 1f : unselectedAlpha;
            Vector2 targetPos = originalPositions[i];
            if (dist > 0)
            {
                float dir = (i < selectedIndex) ? 1f : -1f; // 上方按钮上移，下方按钮下移
                targetPos += new Vector2(0, dir * positionOffset * dist);
            }

            if (!animate)
            {
                canvasGroups[i].alpha = targetAlpha;
                var rt = buttons[i].transform as RectTransform;
                if (rt != null) rt.anchoredPosition = targetPos;
            }
            else
            {
                // 动画版在 AnimateTransition 协程里处理
                // 这里先设置初始状态
            }
        }
    }

    /// <summary>
    /// 从 oldIndex 过渡到 newIndex，平滑动画 alpha 和位置。
    /// </summary>
    private IEnumerator AnimateTransition(int newIndex)
    {
        isAnimating = true;
        int oldIndex = selectedIndex;

        // 录制起始状态
        int count = buttons.Count;
        var startAlphas = new float[count];
        var startPositions = new Vector2[count];
        var targetAlphas = new float[count];
        var targetPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null) continue;
            startAlphas[i] = canvasGroups[i].alpha;
            var rt = buttons[i].transform as RectTransform;
            startPositions[i] = rt != null ? rt.anchoredPosition : Vector2.zero;

            int oldDist = Mathf.Abs(i - oldIndex);
            int newDist = Mathf.Abs(i - newIndex);

            // 目标：基于 newIndex 计算
            if (newDist > visibleRange)
            {
                targetAlphas[i] = 0f;
                targetPositions[i] = originalPositions[i];
            }
            else
            {
                targetAlphas[i] = (newDist == 0) ? 1f : unselectedAlpha;
                targetPositions[i] = originalPositions[i];
                if (newDist > 0)
                {
                    float dir = (i < newIndex) ? 1f : -1f;
                    targetPositions[i] += new Vector2(0, dir * positionOffset * newDist);
                }
            }
        }

        // 先激活所有新可见的按钮
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null) continue;
            int newDist = Mathf.Abs(i - newIndex);
            if (newDist <= visibleRange)
                buttons[i].gameObject.SetActive(true);
        }

        // 动画循环
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / animationDuration));

            for (int i = 0; i < count; i++)
            {
                if (buttons[i] == null) continue;
                canvasGroups[i].alpha = Mathf.Lerp(startAlphas[i], targetAlphas[i], t);
                var rt = buttons[i].transform as RectTransform;
                if (rt != null)
                    rt.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
            }

            yield return null;
        }

        // 动画结束：精确设置到目标值
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] == null) continue;
            int newDist = Mathf.Abs(i - newIndex);

            if (newDist > visibleRange)
            {
                buttons[i].gameObject.SetActive(false);
                canvasGroups[i].alpha = 0f;
            }
            else
            {
                canvasGroups[i].alpha = targetAlphas[i];
                var rt = buttons[i].transform as RectTransform;
                if (rt != null) rt.anchoredPosition = targetPositions[i];
            }
        }

        selectedIndex = newIndex;
        isAnimating = false;
        animCoroutine = null;
        OnSelectionChanged?.Invoke(selectedIndex);
    }
}
