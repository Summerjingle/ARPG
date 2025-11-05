using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkableNPC : NPC
{
    [Header("对话内容")]
    [TextArea(3, 5)]
    public string[] normalDialogue; // 没有任务时的对话
    [TextArea(3, 5)]
    public string[] questDialogue; // 有相关任务时的对话

    public override void Interact()
    {
        // 添加详细的调试信息
        Debug.Log($"=== TalkableNPC.Interact() 开始 ===");
        DebugAllActiveQuests(); 
        Debug.Log($"NPC ID: {npcID}");

        bool hasActiveTalkQuest = ShouldUseQuestDialogue();
        Debug.Log($"hasActiveTalkQuest: {hasActiveTalkQuest}");

        // 详细检查任务状态
        if (QuestManager.Instance != null)
        {
            Debug.Log($"QuestManager 实例存在");
            Debug.Log($"HasActiveTalkQuestForNPC({npcID}): {QuestManager.Instance.HasActiveTalkQuestForNPC(npcID)}");
        }
        else
        {
            Debug.LogError("QuestManager 实例为 null!");
        }

        string[] dialogueToUse = hasActiveTalkQuest ? questDialogue : normalDialogue;
        Debug.Log($"将使用对话: {(hasActiveTalkQuest ? "任务对话" : "普通对话")}, 长度: {dialogueToUse.Length}");

        // 3. 显示对话
        if (DialogueManager.Instance != null && dialogueToUse.Length > 0)
        {
            Debug.Log("开始显示对话...");
            DialogueManager.Instance.StartDialogue(dialogueToUse, () => {
                Debug.Log("对话结束回调执行");
                
                if (hasActiveTalkQuest)
                {
                    Debug.Log("触发任务完成...");
                    QuestManager.Instance.OnNPCTalked(npcID);
                }
                else
                {
                    Debug.Log("没有活跃的谈话任务，不触发任务完成");
                }
            });
        }
        else
        {
            Debug.LogWarning("DialogueManager 为 null 或对话内容为空");
           
        }

        Debug.Log($"=== TalkableNPC.Interact() 结束 ===");
    }

    private bool ShouldUseQuestDialogue()
    {
        if (QuestManager.Instance == null) return false;
        return QuestManager.Instance.HasActiveTalkQuestForNPC(npcID);
    }

    // 修复这个方法中的字段名
    private void DebugAllActiveQuests()
    {
        Debug.Log("=== 所有进行中任务检查 ===");

        foreach (Quest quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            if (quest != null)
            {
                // 使用 QuestManager 获取状态，而不是直接访问 quest.questState
                QuestState state = QuestManager.Instance.GetQuestState(quest);

                if (state == QuestState.InProgress)
                {
                    Debug.Log($"进行中任务: {quest.questID} - {quest.questName}");

                    // 检查任务目标 - 使用正确的字段名
                    if (quest.objectives != null)
                    {
                        foreach (var objective in quest.objectives)
                        {
                            // 使用正确的字段名和方法
                            Debug.Log($"  目标描述: {objective.GetDescription()}");
                            Debug.Log($"  目标ID: {objective.targetID}");
                            Debug.Log($"  目标类型: {objective.objectiveType}");
                            Debug.Log($"  完成状态: {objective.isCompleted}");
                        }
                    }
                }
            }
        }
    }
}