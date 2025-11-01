using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // 单例模式
    public static QuestManager Instance { get; private set; }

    [Header("任务完成标记")]
    [SerializeField] private GameObject mainQuestCompletedMark;
    [SerializeField] private GameObject sideQuestCompletedMark;

    // 任务状态字典
    private Dictionary<Quest, QuestState> questProgress = new Dictionary<Quest, QuestState>();

    private void Awake()
    {
        InitializeSingleton();
    }

    #region 初始化
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region 杀敌任务支持
    /// <summary>
    /// 敌人被杀死时调用
    /// </summary>
    public void OnEnemyKilled(string targetID, string enemyTypeID)
    {
        Debug.Log($"OnEnemyKilled 被调用: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");

        if (string.IsNullOrEmpty(targetID))
        {
            Debug.LogWarning("OnEnemyKilled: targetID 为空");
            return;
        }

        UpdateKillQuests(targetID, enemyTypeID);
    }

    /// <summary>
    /// 更新所有相关的杀敌任务
    /// </summary>
    private void UpdateKillQuests(string targetID, string enemyTypeID)
    {
        Debug.Log("=== 开始查找匹配的杀敌任务 ===");
        Debug.Log($"查找条件: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");

        if (QuestDBManager.Instance == null)
        {
            Debug.LogError("QuestDBManager.Instance 为 null!");
            return;
        }

        if (QuestDBManager.Instance.questDatabase == null)
        {
            Debug.LogError("questDatabase 为 null!");
            return;
        }

        Debug.Log($"任务数据库中共有 {QuestDBManager.Instance.questDatabase.allQuests.Count} 个任务");

        bool foundMatchingQuest = false;
        int questCount = 0;

        // 遍历所有任务
        foreach (var quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            questCount++;
            Debug.Log($"检查任务 #{questCount}: {quest?.questName}");

            if (quest == null)
            {
                Debug.LogWarning("发现空任务，跳过");
                continue;
            }

            Debug.Log($"任务: {quest.questName} (ID: {quest.questID}, 类型: {quest.questType})");

            // 检查任务状态
            var questState = GetQuestState(quest);
            Debug.Log($"任务状态: {questState}");

            if (questState != QuestState.InProgress)
            {
                Debug.Log($"任务状态不是进行中，跳过");
                continue;
            }

            if (quest.objectives == null || quest.objectives.Count == 0)
            {
                Debug.Log($"任务没有目标，跳过");
                continue;
            }

            Debug.Log($"任务有 {quest.objectives.Count} 个目标");

            // 更新匹配的目标
            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var objective = quest.objectives[i];
                Debug.Log($"目标 {i}: 类型={objective.objectiveType}, 目标ID='{objective.targetID}', 所需={objective.requiredAmount}, 当前={objective.currentAmount}");

                bool typeMatches = objective.objectiveType == ObjectiveType.Kill;
                bool targetIDMatches = objective.targetID == targetID;
                bool enemyTypeMatches = objective.targetID == enemyTypeID;

                Debug.Log($"匹配检查: 类型匹配={typeMatches}, 目标ID匹配={targetIDMatches} ('{objective.targetID}' == '{targetID}'), 敌人类型匹配={enemyTypeMatches} ('{objective.targetID}' == '{enemyTypeID}')");

                if (typeMatches && (targetIDMatches || enemyTypeMatches))
                {
                    Debug.Log($"找到匹配的杀敌目标!");
                    // 更新任务目标进度
                    UpdateQuestObjective(quest, i, 1);
                    foundMatchingQuest = true;
                    Debug.Log($"更新杀敌任务进度: {quest.questName} - {objective.targetID} ({objective.currentAmount}/{objective.requiredAmount})");
                }
                else
                {
                    Debug.Log($"目标不匹配，跳过");
                }
            }
        }

        if (!foundMatchingQuest)
        {
            Debug.LogWarning($"没有找到匹配的杀敌任务: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");
            Debug.Log($"可能的原因:");
            Debug.Log($"   - 任务状态不是 InProgress");
            Debug.Log($"   - 任务目标类型不是 Kill");
            Debug.Log($"   - 任务目标ID不匹配 ('Wolf' vs 实际目标ID)");
            Debug.Log($"   - 任务数据库中没有任何任务");
        }
        else
        {
            Debug.Log($"成功找到并更新了匹配的杀敌任务!");
        }

        Debug.Log($"=== 结束查找匹配的杀敌任务 === (检查了 {questCount} 个任务)");
    }
    #endregion

    #region 任务状态管理
    /// <summary>
    /// 设置任务状态
    /// </summary>
    public void SetQuestState(Quest quest, QuestState newState)
    {
        if (quest == null) return;

        if (questProgress.ContainsKey(quest))
        {
            questProgress[quest] = newState;
        }
        else
        {
            questProgress.Add(quest, newState);
        }

        Debug.Log($"任务状态更新: {quest.questName} -> {newState}");
        HandleQuestStateChange(quest, newState);
    }

    /// <summary>
    /// 获取当前任务状态
    /// </summary>
    public QuestState GetQuestState(Quest quest)
    {
        return questProgress.ContainsKey(quest) ? questProgress[quest] : QuestState.NotAccepted;
    }
    #endregion

    #region 任务状态检查
    /// <summary>
    /// 检查任务是否已接受
    /// </summary>
    public bool IsQuestAccepted(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] != QuestState.NotAccepted;
    }

    /// <summary>
    /// 检查任务是否进行中
    /// </summary>
    public bool IsQuestInProgress(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.InProgress;
    }

    /// <summary>
    /// 检查任务是否可完成
    /// </summary>
    public bool IsQuestCanComplete(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.CanComplete;
    }

    /// <summary>
    /// 检查任务是否已完成
    /// </summary>
    public bool IsQuestCompleted(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.Completed;
    }
    #endregion

    #region 任务目标进度管理
    /// <summary>
    /// 更新任务目标进度
    /// </summary>
    public void UpdateQuestObjective(Quest quest, int objectiveIndex, int amount)
    {
        if (quest == null || quest.objectives == null || objectiveIndex >= quest.objectives.Count)
        {
            Debug.LogWarning("更新任务目标失败：参数无效");
            return;
        }

        var objective = quest.objectives[objectiveIndex];
        objective.currentAmount = Mathf.Min(objective.currentAmount + amount, objective.requiredAmount);

        // 检查目标是否完成
        if (objective.currentAmount >= objective.requiredAmount)
        {
            objective.isCompleted = true;
            CheckQuestCompletion(quest);
        }

        // 自动保存进度
        SaveManager.Instance?.SaveGame();
    }

    /// <summary>
    /// 获取任务目标进度
    /// </summary>
    public (int current, int required) GetObjectiveProgress(Quest quest, int objectiveIndex)
    {
        if (quest == null || quest.objectives == null || objectiveIndex >= quest.objectives.Count)
            return (0, 0);

        var objective = quest.objectives[objectiveIndex];
        return (objective.currentAmount, objective.requiredAmount);
    }

    /// <summary>
    /// 检查任务是否可完成
    /// </summary>
    private void CheckQuestCompletion(Quest quest)
    {
        if (quest.objectives.All(obj => obj.isCompleted))
        {
            SetQuestState(quest, QuestState.CanComplete);
        }
    }
    #endregion

    #region UI标记管理
    /// <summary>
    /// 处理任务状态变化的UI效果
    /// </summary>
    private void HandleQuestStateChange(Quest quest, QuestState newState)
    {
        switch (newState)
        {
            case QuestState.CanComplete:
                ShowCompletionMark(quest);
                break;
            case QuestState.Completed:
                // 可以在这里添加任务完成的其他UI效果
                break;
        }
    }

    /// <summary>
    /// 显示任务完成标记
    /// </summary>
    private void ShowCompletionMark(Quest quest)
    {
        if (quest.questType == QuestType.Main && mainQuestCompletedMark != null)
        {
            mainQuestCompletedMark.SetActive(true);
        }
        else if (quest.questType == QuestType.Side && sideQuestCompletedMark != null)
        {
            sideQuestCompletedMark.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏任务完成标记（如果需要的话）
    /// </summary>
    private void HideCompletionMark(Quest quest)
    {
        if (quest.questType == QuestType.Main && mainQuestCompletedMark != null)
        {
            mainQuestCompletedMark.SetActive(false);
        }
        else if (quest.questType == QuestType.Side && sideQuestCompletedMark != null)
        {
            sideQuestCompletedMark.SetActive(false);
        }
    }
    #endregion

    #region 系统重置
    /// <summary>
    /// 重置所有任务状态
    /// </summary>
    public void ResetAllQuests()
    {
        questProgress.Clear();
        ResetAllQuestObjectives();

        // 重置UI标记
        if (mainQuestCompletedMark != null) mainQuestCompletedMark.SetActive(false);
        if (sideQuestCompletedMark != null) sideQuestCompletedMark.SetActive(false);
    }

    /// <summary>
    /// 重置所有任务目标进度
    /// </summary>
    private void ResetAllQuestObjectives()
    {
        if (QuestDBManager.Instance == null) return;

        foreach (var quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            if (quest.objectives != null)
            {
                foreach (var objective in quest.objectives)
                {
                    objective.currentAmount = 0;
                    objective.isCompleted = false;
                }
            }
        }
    }
    #endregion
}