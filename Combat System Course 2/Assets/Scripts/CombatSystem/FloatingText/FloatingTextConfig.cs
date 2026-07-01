using UnityEngine;

/// <summary>
/// 单个飘字类型的 ScriptableObject 配置
/// 控制颜色、大小曲线、位置曲线、Alpha 曲线、图标等
/// </summary>
[CreateAssetMenu(menuName = "Combat System/Floating Text Config")]
public class FloatingTextConfig : ScriptableObject
{
    [Header("外观")]
    public Color textColor = Color.white;
    public float baseFontSize = 36f;
    [Tooltip("可选图标，如暴击图标")]
    public Sprite icon;

    [Header("动画曲线（0~1 归一化时间）")]
    [Tooltip("字体大小变化曲线，1 = 初始大小")]
    public AnimationCurve sizeCurve = AnimationCurve.Constant(0, 1, 1);
    [Tooltip("水平偏移曲线，Y轴值 = 偏移量")]
    public AnimationCurve horizontalOffsetCurve = AnimationCurve.Constant(0, 1, 0);
    [Tooltip("Alpha 变化曲线，1 = 完全不透明")]
    public AnimationCurve alphaCurve = AnimationCurve.Constant(0, 1, 1);

    [Header("行为")]
    [Tooltip("飘字动画总时长（秒）")]
    public float duration = 1.2f;
    [Tooltip("受击者头顶偏移高度")]
    public float heightOffset = 2.5f;
    [Tooltip("水平随机偏移范围")]
    public float randomHorizontalRange = 0.5f;
}
