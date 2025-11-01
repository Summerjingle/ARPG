using UnityEngine;
using System.Collections;

public class QuestNPC : NPC
{
    public Quest quest;
    public string[] notAcceptedDialogue;
    public string[] inProgressDialogue;
    public string[] canCompleteDialogue; // 可完成时的对话
    public string[] completionDialogue;   // 完成后的对话

    public override void Interact()
    {
        if (QuestManager.Instance == null || DialogueManager.Instance == null)
        {
            Debug.LogError("GameManager 或 DialogueManager 未初始化");
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(quest);

        switch (state)
        {
            case QuestState.NotAccepted:
                DialogueManager.Instance.StartDialogueWithButtons(notAcceptedDialogue, AcceptQuest, RejectQuest);
                break;

            case QuestState.InProgress:
                DialogueManager.Instance.StartDialogue(inProgressDialogue);
                break;

            case QuestState.CanComplete:
                // 直接播放完成对话并自动提交任务
                DialogueManager.Instance.StartDialogue(canCompleteDialogue, OnCanCompleteDialogueEnd);
                break;

            case QuestState.Completed:
                DialogueManager.Instance.StartDialogue(completionDialogue);
                break;
        }
    }

    // 可完成状态对话结束后的回调
    private void OnCanCompleteDialogueEnd()
    {
        CompleteQuest();
    }

    private void AcceptQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuestState(quest, QuestState.InProgress);
            Debug.Log($"已接受任务: {quest.questName}");

            // 更新任务面板 - 使用双面板控制器
            if (QuestPanelController.Instance != null)
            {
                if (quest.questType == QuestType.Main)
                {
                    QuestPanelController.Instance.SetMainQuest(quest);
                }
                else
                {
                    QuestPanelController.Instance.SetSideQuest(quest);
                }
            }
        }
    }

    private void RejectQuest()
    {
        Debug.Log($"已拒绝任务: {quest.questName}");
    }

    private void CompleteQuest()
    {
        if (QuestManager.Instance != null)
        {
            // 给予奖励
            
            }

        QuestManager.Instance.SetQuestState(quest, QuestState.Completed);
        Debug.Log($"任务完成: {quest.questName}");

        // 更新任务面板 - 使用现有的UpdateQuestDisplay方法
        if (QuestPanelController.Instance != null)
        {
            QuestPanelController.Instance.UpdateAllPanels();
        }
    }
}