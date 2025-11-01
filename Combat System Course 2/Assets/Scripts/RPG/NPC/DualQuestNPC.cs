using UnityEngine;

public class DualQuestNPC : NPC
{
    [Header("主线任务")]
    public Quest mainQuest;
    public string[] mainNotAcceptedDialogue;    // 未接受状态对话
    public string[] mainInProgressDialogue;     // 进行中状态对话（内容始终相同）
    public string[] mainCanCompleteDialogue;     // 可完成状态对话
    public string[] mainCompletedDialogue;      // 已完成状态对话

    [Header("支线任务")]
    public Quest sideQuest;

    private bool hasIntroducedSideQuest = false; // 标记是否已经接取过支线任务

    public override void Interact()
    {
        if (QuestManager.Instance == null || DialogueManager.Instance == null)
        {
            Debug.LogError("GameManager 或 DialogueManager 未初始化");
            return;
        }

        // 首先检查主线任务状态
        QuestState mainState = QuestManager.Instance.GetQuestState(mainQuest);

        // 根据主线任务状态决定交互内容
        switch (mainState)
        {
            case QuestState.NotAccepted:
                // 提供主线任务（有接受/拒绝选项）
                DialogueManager.Instance.StartDialogueWithButtons(
                    mainNotAcceptedDialogue,
                    AcceptMainQuest,
                    () => Debug.Log("拒绝了主线任务")
                );
                break;

            case QuestState.InProgress:
                // 主线任务进行中，处理支线任务接取逻辑
                HandleMainQuestInProgress();
                break;

            case QuestState.CanComplete:
                // 主线任务可完成
                DialogueManager.Instance.StartDialogueWithButtons(
                    mainCanCompleteDialogue,
                    CompleteMainQuest,
                    null
                );
                break;

            case QuestState.Completed:
                // 主线任务已完成
                DialogueManager.Instance.StartDialogue(mainCompletedDialogue);
                break;
        }
    }

    // 处理主线任务进行中的情况
    private void HandleMainQuestInProgress()
    {
        // 检查是否需要接取支线任务
        if (sideQuest != null && !hasIntroducedSideQuest &&
            QuestManager.Instance.GetQuestState(sideQuest) == QuestState.NotAccepted)
        {
            // 首次进行主线任务进行中对话，内容相同但结束后自动接取支线任务
            DialogueManager.Instance.StartDialogue(
                mainInProgressDialogue,
                AutoAcceptSideQuest
            );
        }
        else
        {
            // 已经接取了支线任务或不需要接取，正常显示进行中对话
            DialogueManager.Instance.StartDialogue(mainInProgressDialogue);
        }
    }

    // 接受主线任务
    private void AcceptMainQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuestState(mainQuest, QuestState.InProgress);
            Debug.Log($"已接受主线任务: {mainQuest.questName}");

            // 更新任务面板
            if (QuestPanelController.Instance != null)
            {
                QuestPanelController.Instance.SetMainQuest(mainQuest);
            }
        }
    }

    // 完成主线任务
    private void CompleteMainQuest()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuestState(mainQuest, QuestState.Completed);
            Debug.Log($"完成主线任务: {mainQuest.questName}，获得奖励: {mainQuest.rewardGold}金币");

            // 更新任务面板
            if (QuestPanelController.Instance != null)
            {
                QuestPanelController.Instance.UpdateAllPanels();
            }
        }
    }

    // 自动接受支线任务（在主线任务进行中对话结束后调用）
    private void AutoAcceptSideQuest()
    {
        if (sideQuest != null && QuestManager.Instance != null)
        {
            QuestManager.Instance.SetQuestState(sideQuest, QuestState.InProgress);
            hasIntroducedSideQuest = true;
            Debug.Log($"已自动接受支线任务: {sideQuest.questName}");

            // 更新任务面板
            if (QuestPanelController.Instance != null)
            {
                QuestPanelController.Instance.SetSideQuest(sideQuest);
            }
        }
    }
}