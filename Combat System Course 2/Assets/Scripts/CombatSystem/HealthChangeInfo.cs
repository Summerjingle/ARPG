/// <summary>
/// 血量变化信息，随 OnHealthChanged 事件传递
/// </summary>
public struct HealthChangeInfo
{
    /// <summary>血量变化量：正=恢复，负=伤害</summary>
    public float delta;

    /// <summary>是否暴击（仅伤害时有效）</summary>
    public bool isCrit;
}
