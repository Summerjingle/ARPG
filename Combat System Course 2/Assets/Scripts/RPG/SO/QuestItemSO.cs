using UnityEngine;

/// <summary>
/// 任务道具：不可使用、不可丢弃
/// </summary>
[CreateAssetMenu(menuName = "Items/Quest Item", fileName = "New Quest Item")]
public class QuestItemSO : ItemSO
{
    public override ItemType itemType => ItemType.QuestRelated;

    [Header("关联任务")]
    public string questID;

    public override bool IsStackable() => false;
}
