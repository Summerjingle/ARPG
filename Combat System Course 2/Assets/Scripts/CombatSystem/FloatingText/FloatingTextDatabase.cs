using UnityEngine;

/// <summary>
/// 聚合三种飘字配置的 ScriptableObject 容器
/// 参考项目现有 ItemDBSO 的聚合模式
/// </summary>
[CreateAssetMenu(menuName = "Combat System/Floating Text Database")]
public class FloatingTextDatabase : ScriptableObject
{
    [Header("普通攻击 — 白色")]
    public FloatingTextConfig attackConfig;

    [Header("暴击 — 红色 + 图标")]
    public FloatingTextConfig critConfig;

    [Header("恢复 — 绿色")]
    public FloatingTextConfig recoveryConfig;

    /// <summary>
    /// 根据血量变化信息选择对应的配置
    /// </summary>
    public FloatingTextConfig SelectConfig(HealthChangeInfo info)
    {
        if (info.delta > 0)
            return recoveryConfig;
        if (info.delta < 0 && info.isCrit)
            return critConfig;
        if (info.delta < 0 && !info.isCrit)
            return attackConfig;
        return null;
    }
}
