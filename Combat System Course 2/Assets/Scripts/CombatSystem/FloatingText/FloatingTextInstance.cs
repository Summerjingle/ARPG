using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 飘字实例：挂载在对象池中的 TMP 对象上，负责单个飘字的动画播放
/// </summary>
public class FloatingTextInstance : MonoBehaviour
{
    [HideInInspector] public TextMeshPro tmpText;
    [HideInInspector] public SpriteRenderer iconRenderer;

    private Coroutine animationCoroutine;
    private Action onComplete;
    private Vector3 startWorldPos;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        tmpText = GetComponent<TextMeshPro>();
        if (tmpText == null)
            tmpText = GetComponentInChildren<TextMeshPro>();

        // 图标子对象（可选，含子级查找）
        var iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            iconRenderer = iconTransform.GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 开始飘字动画，完成后回调 onComplete
    /// </summary>
    public void PlayAnimation(FloatingTextConfig config, Action onComplete)
    {
        this.onComplete = onComplete;
        startWorldPos = transform.position;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateRoutine(config));
    }

    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator AnimateRoutine(FloatingTextConfig config)
    {
        float t = 0f;
        float duration = config.duration;

        // 初始状态
        SetAlpha(config.alphaCurve.Evaluate(0));
        SetScale(config.sizeCurve.Evaluate(0));

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            float alpha = config.alphaCurve.Evaluate(t);
            float scale = config.sizeCurve.Evaluate(t);
            float horizontalOffset = config.horizontalOffsetCurve.Evaluate(t);

            SetAlpha(alpha);
            SetScale(scale);

            // 世界坐标：起始位置 + 曲线驱动的水平偏移
            transform.position = startWorldPos + Vector3.right * horizontalOffset;

            // 微小的上浮（可选，通过 heightOffset curve 控制更好，这里留个简化版）
            // 如果 horizontalOffsetCurve 的第二维用作高度，可以再映射一下

            yield return null;
        }

        // 确保最终状态
        SetAlpha(config.alphaCurve.Evaluate(1f));
        SetScale(config.sizeCurve.Evaluate(1f));

        onComplete?.Invoke();
        onComplete = null;
    }

    private void SetAlpha(float alpha)
    {
        if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = alpha;
            tmpText.color = c;
        }

        if (iconRenderer != null)
        {
            Color c = iconRenderer.color;
            c.a = alpha;
            iconRenderer.color = c;
        }
    }

    private void SetScale(float scale)
    {
        if (tmpText != null)
            tmpText.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }
}
