using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // 单例模式
    public static GameManager Instance { get; private set; }

    // 任务状态字典
    private Dictionary<Quest, QuestState> questProgress = new Dictionary<Quest, QuestState>();

    public List<Quest> allQuests;
    private void Awake()
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

    // 检查任务是否已接受
    public bool IsQuestAccepted(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] != QuestState.NotAccepted;
    }

    // 设置任务状态
    public void SetQuestState(Quest quest, QuestState newState)
    {
        if (questProgress.ContainsKey(quest))
        {
            questProgress[quest] = newState;
        }
        else
        {
            questProgress.Add(quest, newState);
        }
        Debug.Log($"任务状态更新: {quest.questName} -> {newState}");
    }

    // 获取当前任务状态
    public QuestState GetQuestState(Quest quest)
    {
        return questProgress.ContainsKey(quest) ? questProgress[quest] : QuestState.NotAccepted;
    }

    // 检查任务是否已完成
    public bool IsQuestCompleted(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.Completed;
    }

    // 检查任务是否可完成
    public bool IsQuestCanComplete(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.CanComplete;
    }

    // 检查任务是否进行中
    public bool IsQuestInProgress(Quest quest)
    {
        return questProgress.ContainsKey(quest) &&
               questProgress[quest] == QuestState.InProgress;
    }
}