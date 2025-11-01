using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Game/Quests/Quest")]
public class Quest : ScriptableObject
{
    [Header("基础信息")]
    public string questID;                    // 唯一标识符
    public string questName;                  // 显示名称
    [TextArea(3, 5)]
    public string description;                // 任务描述
    public QuestType questType = QuestType.Main;

    [Header("任务要求")]
    public int requiredLevel = 1;             // 接取等级要求
    public List<Quest> requiredQuests;        // 前置任务
    public List<QuestObjective> objectives;   // 任务目标

    [Header("奖励")]
    public int rewardGold;
    public int rewardExp;
    public List<ItemSO> rewardItems;          // 奖励物品

    [Header("设置")]
    public bool isRepeatable = false;
    public bool autoComplete = false;         // 是否自动完成
}

[System.Serializable]
public class QuestObjective
{
    public ObjectiveType objectiveType;
    public string targetID;                   // 目标ID（NPC、物品、敌人等）
    public int requiredAmount = 1;
    public int currentAmount = 0;
    public bool isCompleted = false;

    public string GetDescription()
    {
        switch (objectiveType)
        {
            case ObjectiveType.Kill: return $"击败 {targetID} ({currentAmount}/{requiredAmount})";
            case ObjectiveType.Collect: return $"收集 {targetID} ({currentAmount}/{requiredAmount})";
            case ObjectiveType.Talk: return $"与 {targetID} 对话";
            case ObjectiveType.Explore: return $"探索 {targetID}";
            default: return "未知目标";
        }
    }
}

public enum ObjectiveType
{
    Kill,       // 击败敌人
    Collect,    // 收集物品
    Talk,       // 与NPC对话
    Explore     // 探索区域
}

public enum QuestType
{
    Main,       // 主线任务
    Side        // 支线任务
}

public enum QuestState
{
    NotAccepted,    // 未接受
    InProgress,     // 进行中
    CanComplete,    // 可完成
    Completed       // 已完成
}