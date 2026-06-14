using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // ����ģʽ
    public static QuestManager Instance { get; private set; }

    [Header("������ɱ��")]
    [SerializeField] private GameObject mainQuestCompletedMark;
    [SerializeField] private GameObject sideQuestCompletedMark;

    // ����״̬�ֵ�
    private Dictionary<Quest, QuestState> questProgress = new Dictionary<Quest, QuestState>();

    // ����������Ŀ������ʱ״̬�ֵ�
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

    #region ��ʼ��
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

    #region ����ʱ״̬����
    /// <summary>
    /// ��ȡĿ������ʱ״̬
    /// </summary>
    public bool IsObjectiveCompleted(Quest quest, int objectiveIndex)
    {
        if (objectiveRuntimeData.ContainsKey(quest) && objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            return objectiveRuntimeData[quest][objectiveIndex].isCompleted;
        }

        // �������ʱ���ݲ����ڣ�ʹ��ScriptableObject��Ĭ��ֵ����Ӧ�ñ��⣩
        return quest.objectives != null && objectiveIndex < quest.objectives.Count ?
               quest.objectives[objectiveIndex].isCompleted : false;
    }

    /// <summary>
    /// ����Ŀ�����״̬
    /// </summary>
    public void SetObjectiveCompleted(Quest quest, int objectiveIndex, bool completed)
    {
        // ȷ������ʱ���ݴ���
        if (!objectiveRuntimeData.ContainsKey(quest))
        {
            InitializeObjectiveRuntimeData(quest);
        }

        if (objectiveIndex < objectiveRuntimeData[quest].Count)
        {
            objectiveRuntimeData[quest][objectiveIndex].isCompleted = completed;
            Debug.Log($"����Ŀ�����״̬: {quest.questName} Ŀ��{objectiveIndex} -> {completed}");
        }
    }

    /// <summary>
    /// ����Ŀ�굱ǰ����
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
    /// ����Ŀ�����
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

            // ���Ŀ���Ƿ����
            if (runtimeData.currentAmount >= objective.requiredAmount)
            {
                runtimeData.isCompleted = true;
                Debug.Log($"Ŀ���Զ����: {quest.questName} Ŀ��{objectiveIndex}");
                CheckQuestCompletion(quest);
            }
        }
    }

    /// <summary>
    /// ��ʼ������ʱ����
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

    #region ɱ������֧��
    /// <summary>
    /// ���˱�ɱ��ʱ����
    /// </summary>
    public void OnEnemyKilled(string targetID, string enemyTypeID)
    {
        Debug.Log($"OnEnemyKilled ������: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");
        UpdateKillQuests(targetID, enemyTypeID);
    }

    /// <summary>
    /// ����������ص�ɱ������
    /// </summary>
    private void UpdateKillQuests(string targetID, string enemyTypeID)
    {
        if (QuestDBManager.Instance == null || QuestDBManager.Instance.questDatabase == null)
        {
            Debug.LogError("�������ݿ�δ��ʼ��");
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
                    !IsObjectiveCompleted(quest, i))  // ʹ������ʱ״̬���
                {
                    AddObjectiveProgress(quest, i, 1);
                    foundMatchingQuest = true;
                    Debug.Log($"ɱ��������ȸ���: {quest.questName} - {objective.targetID}");
                }
            }
        }

        if (!foundMatchingQuest)
        {
            Debug.LogWarning($"û���ҵ�ƥ���ɱ������: targetID='{targetID}', enemyTypeID='{enemyTypeID}'");
        }
    }
    #endregion

    #region ̸������֧��
    /// <summary>
    /// NPC�Ի�ʱ���� - ��������NPC
    /// </summary>
    public void OnNPCTalked(string npcID)
    {
        Debug.Log($"OnNPCTalked ������: npcID='{npcID}'");
        UpdateTalkQuests(npcID);
    }

    /// <summary>
    /// ����������ص�̸������
    /// </summary>
    private void UpdateTalkQuests(string npcID)
    {
        if (QuestDBManager.Instance == null || QuestDBManager.Instance.questDatabase == null)
        {
            Debug.LogError("�������ݿ�δ��ʼ��");
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
                    !IsObjectiveCompleted(quest, i))  // ʹ������ʱ״̬���
                {
                    // ʹ������ʱ״̬��������ֱ���޸�ScriptableObject
                    SetObjectiveCompleted(quest, i, true);
                    SetObjectiveCurrentAmount(quest, i, objective.requiredAmount);

                    foundMatchingQuest = true;
                    Debug.Log($"̸���������: {quest.questName} - �� {npcID} �Ի����");

                    // ������������Ƿ�����
                    CheckQuestCompletion(quest);
                }
            }
        }

        if (!foundMatchingQuest)
        {
            Debug.Log($"û���ҵ���NPC {npcID} ��صĽ�����̸������");
        }
    }

    /// <summary>
    /// ����Ƿ����Դ�NPCΪĿ��Ľ�����̸������
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
                    !IsObjectiveCompleted(quest, i))  // ʹ������ʱ״̬���
                {
                    return true;
                }
            }
        }
        return false;
    }
    #endregion

    #region ����״̬����
    /// <summary>
    /// ��������״̬
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

        Debug.Log($"����״̬����: {quest.questName} -> {newState}");

        if (newState == QuestState.CanComplete && quest.autoComplete)
        {
            Debug.Log($"��⵽�Զ��������: {quest.questName}���������");
            CompleteQuest(quest);
        }
        else
        {
            HandleQuestStateChange(quest, newState);
        }
    }

    /// <summary>
    /// ��ȡ��ǰ����״̬
    /// </summary>
    public QuestState GetQuestState(Quest quest)
    {
        return questProgress.ContainsKey(quest) ? questProgress[quest] : QuestState.NotAccepted;
    }
    #endregion

    #region ����״̬���
    public bool IsQuestAccepted(Quest quest) => GetQuestState(quest) != QuestState.NotAccepted;
    public bool IsQuestInProgress(Quest quest) => GetQuestState(quest) == QuestState.InProgress;
    public bool IsQuestCanComplete(Quest quest) => GetQuestState(quest) == QuestState.CanComplete;
    public bool IsQuestCompleted(Quest quest) => GetQuestState(quest) == QuestState.Completed;
    #endregion

    #region ������ɼ��
    /// <summary>
    /// ��������Ƿ�����
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

    #region ��������뽱��
    /// <summary>
    /// ������񲢸��轱��
    /// </summary>
    public void CompleteQuest(Quest quest)
    {
        if (quest == null) { Debug.Log("�޷����Ž���"); return; }

        // ���轱��
        GiveQuestRewards(quest);

        // ��������״̬Ϊ�����
        SetQuestState(quest, QuestState.Completed);

        Debug.Log($"�������: {quest.questName}");
    }

    /// <summary>
    /// ����������
    /// </summary>
    public void GiveQuestRewards(Quest quest)
    {
        if (quest.rewardCoins > 0)
        {
            CurrencySystem.Instance.AddCoins(quest.rewardCoins);
            Debug.Log($"��ý�ҽ���: {quest.rewardCoins}");
        }

        if (quest.rewardExp > 0)
        {
            Debug.Log($"��þ��齱��: {quest.rewardExp}");
        }

        if (quest.rewardItems != null)
        {
            foreach (var item in quest.rewardItems)
            {
                if (item != null)
                {
                    InventoryManager.Instance?.AddItem(item);
                    Debug.Log($"�����Ʒ����: {item.nameOfItem}");
                }
            }
        }

        // �Զ�������Ϸ
        SaveManager.Instance?.SaveGame(updatePosition: false);
    }
    #endregion

    #region UI��ǹ���
    /// <summary>
    /// ��������״̬�仯��UIЧ��
    /// </summary>
    private void HandleQuestStateChange(Quest quest, QuestState newState)
    {
        if (newState == QuestState.CanComplete)
        {
            ShowCompletionMark(quest);
        }
   
    }

    /// <summary>
    /// ��ʾ������ɱ��
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
            Debug.Log($"������ɱ�Ƕ���: {quest.questName}");
        }
    }
    #endregion

    #region ϵͳ����
    /// <summary>
    /// ������������״̬
    /// </summary>
    public void ResetAllQuests()
    {
        questProgress.Clear();
        objectiveRuntimeData.Clear();
        ResetAllQuestObjectives();

        // ���� UI ���״̬
        if (QuestPanelController.Instance != null)
        {
            QuestPanelController.Instance.RemoveMainQuest();
            QuestPanelController.Instance.RemoveSideQuest();
            QuestPanelController.Instance.StopAllAnimations();
            Debug.Log("�������������");
        }

        if (mainQuestCompletedMark != null) mainQuestCompletedMark.SetActive(false);
        if (sideQuestCompletedMark != null) sideQuestCompletedMark.SetActive(false);

        Debug.Log("��������״̬������");
    }

    /// <summary>
    /// ������������Ŀ�����
    /// </summary>
    private void ResetAllQuestObjectives()
    {
        if (QuestDBManager.Instance == null) return;

        // ����ScriptableObject״̬��ȷ���༭��״̬��ȷ��
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