using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuestDBManager : MonoBehaviour
{
    public static QuestDBManager Instance { get; private set; }
    public QuestDatabase questDatabase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 获取可接取的任务（根据玩家等级和已完成的前置任务）
    public List<Quest> GetAvailableQuests(int playerLevel, List<Quest> completedQuests)
    {
        return questDatabase.allQuests.Where(quest =>
            quest.requiredLevel <= playerLevel &&
            ArePrerequisitesMet(quest, completedQuests) &&
            !QuestManager.Instance.IsQuestAccepted(quest)
        ).ToList();
    }

    // 检查前置任务是否完成
    private bool ArePrerequisitesMet(Quest quest, List<Quest> completedQuests)
    {
        if (quest.requiredQuests == null || quest.requiredQuests.Count == 0)
            return true;

        foreach (var requiredQuest in quest.requiredQuests)
        {
            if (!completedQuests.Contains(requiredQuest))
                return false;
        }
        return true;
    }

    // 获取随机任务（用于日常任务等）
    public Quest GetRandomQuest(QuestType questType, int playerLevel)
    {
        var availableQuests = questDatabase.allQuests.Where(quest =>
            quest.questType == questType &&
            quest.requiredLevel <= playerLevel &&
            quest.isRepeatable
        ).ToList();

        if (availableQuests.Count > 0)
        {
            int randomIndex = Random.Range(0, availableQuests.Count);
            return availableQuests[randomIndex];
        }

        return null;
    }
}