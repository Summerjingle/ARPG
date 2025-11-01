using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Game/Quests/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    public List<Quest> allQuests = new List<Quest>();

    // 通过ID查找任务
    public Quest GetQuestByID(string questID)
    {
        return allQuests.Find(quest => quest.questID == questID);
    }

    // 通过名称查找任务
    public Quest GetQuestByName(string questName)
    {
        return allQuests.Find(quest => quest.questName == questName);
    }

    // 获取所有主线任务
    public List<Quest> GetAllMainQuests()
    {
        return allQuests.FindAll(quest => quest.questType == QuestType.Main);
    }

    // 获取所有支线任务
    public List<Quest> GetAllSideQuests()
    {
        return allQuests.FindAll(quest => quest.questType == QuestType.Side);
    }
}