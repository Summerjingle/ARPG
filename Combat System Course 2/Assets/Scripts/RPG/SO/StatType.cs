/// <summary>
/// 属性类型枚举：Bonus类（装备提供，base+bonus=total），Curr类（消耗品直接加当前值）
/// </summary>
public enum StatType
{
    // === Bonus属性（装备提供）===
    MaxHP,
    MaxEnergy,
    Defense,
    CritRate,
    CritDamage,
    Strength,       // 预留：装备条件
    Luck,           // 预留

    // === Curr属性（消耗品提供）===
    CurrHP,
    CurrEnergy,
}
