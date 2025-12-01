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

    // 新增：任务目标运行时状态字典
    private Dictionary<Quest, List<ObjectiveRuntimeData>> objectiveRuntimeData = new Dictionary<Quest, List<ObjectiveRuntimeData>>();

    [System.Serializable]
    public class ObjectiveRuntimeData
    {
        public int currentAmount;
        public bool isCompleted;
    }

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

    #region 运行时状态管理
    /// <summary>
    /// 获取目标运行时状态
    /// </summary>
    public bool IsObjectiveCompleted(Quest quest, int objectiveIndex)
    {
        if (objectiveRuntimeData.ContainsKey(quest) && objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            return objectiveRuntimeData[quest][objectiveIndex].isCompleted;
        }

        // 如果运行时数据不存在，使用ScriptableObject的默认值（但应该避免）
        return quest.objectives != null && objectiveIndex < quest.objectives.Count ?
               quest.objectives[objectiveIndex].isCompleted : false;
    }

    /// <summary>
    /// 设置目标完成状态
    /// </summary>
    public void SetObjectiveCompleted(Quest quest, int objectiveIndex, bool completed)
    {
        // 确保运行时数据存在
        if (!objectiveRuntimeData.ContainsKey(quest))
        {
            InitializeObjectiveRuntimeData(quest);
        }

        if (objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            objectiveRuntimeData[quest][objectiveIndex].isCompleted = completed;
            Debug.Log($"设置目标完成状态: {quest.questName} 目标{objectiveIndex} -> {completed}");
        }
    }

    /// <summary>
    /// 设置目标当前数量
    /// </summary>
    public void SetObjectiveCurrentAmount(Quest quest, int objectiveIndex, int amount)
    {
        if (!objectiveRuntimeData.ContainsKey(quest))
        {
            InitializeObjectiveRuntimeData(quest);
        }

        if (objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            objectiveRuntimeData[quest][objectiveIndex].currentAmount = amount;
        }
    }

    /// <summary>
    /// 增加目标进度
    /// </summary>
    public void AddObjectiveProgress(Quest quest, int objectiveIndex, int amount)
    {
        if (!objectiveRuntimeData.ContainsKey(quest))
        {
            InitializeObjectiveRuntimeData(quest);
        }

        if (objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            var objective = quest.objectives[objectiveIndex];
            var runtimeData = objectiveRuntimeData[quest][objectiveIndex];

            runtimeData.currentAmount = Mathf.Min(runtimeData.currentAmount + amount, objective.requiredAmount);

            // 检查目标是否完成
            if (runtimeData.currentAmount >= objective.requiredAmount)
            {
                runtimeData.isCompleted = true;
                Debug.Log($"目标自动完成: {quest.questName} 目标{objectiveIndex}");
                CheckQuestCompletion(quest);
            }
        }
    }

    /// <summary>
    /// 初始化运行时数据
    /// </summary>
    private void InitializeObjectiveRuntimeData(Quest quest)
    {
        objectiveRuntimeData[quest] = new List<ObjectiveRuntimeData>();

        if (quest.objectives != null)
        {
            foreach (var objective in quest.objectives)
            {
                objectiveRuntimeData[quest].Add(new ObjectiveRuntimeData
                {
                    currentAmount = objective.currentAmount,
                    isCompleted = objective.isCompleted
                });
            }
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
        UpdateKillQuests(targetID, enemyTypeID);
    }

    /// <summary>
    /// 更新所有相关的杀敌任务
    /// </summary>
    private void UpdateKillQuests(string targetID, string enemyTypeID)
    {
        if (QuestDBManager.Instance == null || QuestDBManager.Instance.questDatabase == null)
        {
            Debug.LogError("任务数据库未初始化");
            return;
        }

        bool foundMatchingQuest = false;

        foreach (var quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            var questState = GetQuestState(quest);
            if (questState != QuestState.InProgress) continue;
            if (quest.objectives == null) continue;

            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var objective = quest.objectives[i];

                if (objective.objectiveType == ObjectiveType.Kill &&
                    (objective.targetID == targetID || objective.targetID == enemyTypeID) &&
                    !IsObjectiveCompleted(quest, i))  // 使用运行时状态检查
                {
                    AddObjectiveProgress(quest, i, 1);
                    foundMatchingQuest = true;
                    Debug.Log($"杀敌任务进度更新: {quest.questName} - {objective.targetID}");
                }
            }
        }

        if (!foundMatchingQuest)
        {
            Debug.LogWarning($"没有找到匹配的杀敌任务: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");
        }
    }
    #endregion

    #region 谈话任务支持
    /// <summary>
    /// NPC对话时调用 - 用于所有NPC
    /// </summary>
    public void OnNPCTalked(string npcID)
    {
        Debug.Log($"OnNPCTalked 被调用: npcID='{npcID}'");
        UpdateTalkQuests(npcID);
    }

    /// <summary>
    /// 更新所有相关的谈话任务
    /// </summary>
    private void UpdateTalkQuests(string npcID)
    {
        if (QuestDBManager.Instance == null || QuestDBManager.Instance.questDatabase == null)
        {
            Debug.LogError("任务数据库未初始化");
            return;
        }

        bool foundMatchingQuest = false;

        foreach (var quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            var questState = GetQuestState(quest);
            if (questState != QuestState.InProgress) continue;
            if (quest.objectives == null) continue;

            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var objective = quest.objectives[i];

                if (objective.objectiveType == ObjectiveType.Talk &&
                    objective.targetID == npcID &&
                    !IsObjectiveCompleted(quest, i))  // 使用运行时状态检查
                {
                    // 使用运行时状态，而不是直接修改ScriptableObject
                    SetObjectiveCompleted(quest, i, true);
                    SetObjectiveCurrentAmount(quest, i, objective.requiredAmount);

                    foundMatchingQuest = true;
                    Debug.Log($"谈话任务完成: {quest.questName} - 与 {npcID} 对话完成");

                    // 检查整个任务是否可完成
                    CheckQuestCompletion(quest);
                }
            }
        }

        if (!foundMatchingQuest)
        {
            Debug.Log($"没有找到与NPC {npcID} 相关的进行中谈话任务");
        }
    }

    /// <summary>
    /// 检查是否有以此NPC为目标的进行中谈话任务
    /// </summary>
    public bool HasActiveTalkQuestForNPC(string npcID)
    {
        if (QuestDBManager.Instance == null || QuestDBManager.Instance.questDatabase == null)
            return false;

        foreach (var quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            var questState = GetQuestState(quest);
            if (questState != QuestState.InProgress) continue;
            if (quest.objectives == null) continue;

            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var objective = quest.objectives[i];
                if (objective.objectiveType == ObjectiveType.Talk &&
                    objective.targetID == npcID &&
                    !IsObjectiveCompleted(quest, i))  // 使用运行时状态检查
                {
                    return true;
                }
            }
        }
        return false;
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

        if (newState == QuestState.CanComplete && quest.autoComplete)
        {
            Debug.Log($"检测到自动完成任务: {quest.questName}，立即完成");
            CompleteQuest(quest);
        }
        else
        {
            HandleQuestStateChange(quest, newState);
        }
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
    public bool IsQuestAccepted(Quest quest) => GetQuestState(quest) != QuestState.NotAccepted;
    public bool IsQuestInProgress(Quest quest) => GetQuestState(quest) == QuestState.InProgress;
    public bool IsQuestCanComplete(Quest quest) => GetQuestState(quest) == QuestState.CanComplete;
    public bool IsQuestCompleted(Quest quest) => GetQuestState(quest) == QuestState.Completed;
    #endregion

    #region 任务完成检查
    /// <summary>
    /// 检查任务是否可完成
    /// </summary>
    private void CheckQuestCompletion(Quest quest)
    {
        if (quest.objectives == null) return;

        bool allCompleted = true;
        for (int i = 0; i < quest.objectives.Count; i++)
        {
            if (!IsObjectiveCompleted(quest, i))
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            SetQuestState(quest, QuestState.CanComplete);
        }
    }

   
    #endregion

    #region 任务完成与奖励
    /// <summary>
    /// 完成任务并给予奖励
    /// </summary>
    public void CompleteQuest(Quest quest)
    {
        if (quest == null) { Debug.Log("无法发放奖励"); return; }

        // 给予奖励
        GiveQuestRewards(quest);

        // 设置任务状态为已完成
        SetQuestState(quest, QuestState.Completed);

        Debug.Log($"任务完成: {quest.questName}");
    }

    /// <summary>
    /// 给予任务奖励
    /// </summary>
    public void GiveQuestRewards(Quest quest)
    {
        if (quest.rewardCoins > 0)
        {
            CurrencySystem.Instance.AddCoins(quest.rewardCoins);
            Debug.Log($"获得金币奖励: {quest.rewardCoins}");
        }

        if (quest.rewardExp > 0)
        {
            Debug.Log($"获得经验奖励: {quest.rewardExp}");
        }

        if (quest.rewardItems != null)
        {
            foreach (var item in quest.rewardItems)
            {
                if (item != null)
                {
                    InventoryManager.Instance?.AddItem(item);
                    Debug.Log($"获得物品奖励: {item.nameOfItem}");
                }
            }
        }

        // 自动保存游戏
        SaveManager.Instance?.SaveGame();
    }
    #endregion

    #region UI标记管理
    /// <summary>
    /// 处理任务状态变化的UI效果
    /// </summary>
    private void HandleQuestStateChange(Quest quest, QuestState newState)
    {
        if (newState == QuestState.CanComplete)
        {
            ShowCompletionMark(quest);
        }
   
    }

    /// <summary>
    /// 显示任务完成标记
    /// </summary>
    private void ShowCompletionMark(Quest quest)
    {
        Animator markAnimator = null;

        if (quest.questType == QuestType.Main && mainQuestCompletedMark != null)
        {
            markAnimator = mainQuestCompletedMark.GetComponent<Animator>();
            mainQuestCompletedMark.SetActive(true);
        }
        else if (quest.questType == QuestType.Side && sideQuestCompletedMark != null)
        {
            markAnimator = sideQuestCompletedMark.GetComponent<Animator>();
            sideQuestCompletedMark.SetActive(true);
        }

        if (markAnimator != null)
        {
            markAnimator.SetTrigger("IsCompleted");
            Debug.Log($"触发完成标记动画: {quest.questName}");
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
        objectiveRuntimeData.Clear();
        ResetAllQuestObjectives();

        // 重置 UI 面板状态
        if (QuestPanelController.Instance != null)
        {
            QuestPanelController.Instance.RemoveMainQuest();
            QuestPanelController.Instance.RemoveSideQuest();
            QuestPanelController.Instance.StopAllAnimations();
            Debug.Log("任务面板已重置");
        }

        if (mainQuestCompletedMark != null) mainQuestCompletedMark.SetActive(false);
        if (sideQuestCompletedMark != null) sideQuestCompletedMark.SetActive(false);

        Debug.Log("所有任务状态已重置");
    }

    /// <summary>
    /// 重置所有任务目标进度
    /// </summary>
    private void ResetAllQuestObjectives()
    {
        if (QuestDBManager.Instance == null) return;

        // 重置ScriptableObject状态（确保编辑器状态正确）
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