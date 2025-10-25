using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest")]
public class Quest : ScriptableObject
{
    public string questName;
    public string description;
    public int rewardGold;
    public QuestType questType = QuestType.Main;
    public bool isRepeatable = false;
}

public enum QuestType
{
    Main,
    Side
}

public enum QuestState
{
    NotAccepted,    // 未接受
    InProgress,     // 进行中
    CanComplete,    // 可完成
    Completed       // 已完成
}