/// <summary>
/// 装备条件：全部条件 AND 且 >= 才可装备
/// </summary>
[System.Serializable]
public class EquipCondition
{
    public StatType statType;
    public int requiredValue;
}
