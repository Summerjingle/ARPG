using UnityEngine;

public class EventNPC : NPC
{
    [Header("首次对话内容")]
    public string[] firstTimeDialogue;

    [Header("后续对话内容")]
    public string[] repeatDialogue;

    [Header("触发事件类型")]
    public EventType eventType = EventType.None;

    [Header("完成任务（如果事件类型是CompleteQuest）")]
    public Quest questToComplete;

    [Header("获得物品（如果事件类型是GetItem）")]
    public ItemSO itemToGive; // 改为使用 ItemSO 类型
    public int itemQuantity = 1;

    [Header("触发其他事件")]
    public UnityEngine.Events.UnityEvent onDialogueEnd;

    [Header("对话设置")]
    [Tooltip("是否允许重复触发事件")]
    public bool canRepeatEvent = false;
    [Tooltip("是否使用不同的后续对话")]
    public bool useDifferentRepeatDialogue = true;

    private bool hasInteracted = false; // 标记是否已经交互过

    public enum EventType
    {
        None,           // 纯对话，不触发事件
        CompleteQuest,  // 完成任务
        GetItem,        // 获得物品
        CustomEvent     // 自定义事件
    }

    public override void Interact()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager 未初始化");
            return;
        }

        // 决定使用哪组对话
        string[] dialogueToUse;
        System.Action onComplete = null;

        if (!hasInteracted || canRepeatEvent)
        {
            // 第一次交互或允许重复触发事件
            dialogueToUse = firstTimeDialogue;
            onComplete = TriggerEvent;

            if (!canRepeatEvent)
            {
                hasInteracted = true;
            }
        }
        else
        {
            // 后续交互，使用重复对话
            dialogueToUse = useDifferentRepeatDialogue && repeatDialogue.Length > 0 ?
                           repeatDialogue : firstTimeDialogue;
        }

        // 开始对话
        DialogueManager.Instance.StartDialogue(dialogueToUse, onComplete);
    }

    private void TriggerEvent()
    {
        switch (eventType)
        {
            case EventType.CompleteQuest:
                CompleteQuest();
                break;
            case EventType.GetItem:
                GiveItem();
                break;
            case EventType.CustomEvent:
                TriggerCustomEvent();
                break;
            case EventType.None:
            default:
                // 纯对话，不触发任何事件
                break;
        }
    }

    private void CompleteQuest()
    {
        if (questToComplete != null && QuestManager.Instance != null)
        {
            QuestState currentState = QuestManager.Instance.GetQuestState(questToComplete);
            if (currentState != QuestState.Completed)
            {
                QuestManager.Instance.SetQuestState(questToComplete, QuestState.Completed);
                Debug.Log($"通过对话完成任务: {questToComplete.questName}");

                // 更新任务面板
                if (QuestPanelController.Instance != null)
                {
                    QuestPanelController.Instance.UpdateAllPanels();
                }
            }
        }
        else
        {
            Debug.LogWarning("任务完成事件配置错误: questToComplete 或 GameManager 为 null");
        }
    }

    private void GiveItem()
    {
        if (itemToGive != null && InventoryManager.Instance != null)
        {
            // 根据数量多次添加物品
            for (int i = 0; i < itemQuantity; i++)
            {
                InventoryManager.Instance.AddItem(itemToGive);
            }
            Debug.Log($"获得物品: {itemToGive.name} x{itemQuantity}");
        }
        else
        {
            Debug.LogWarning("物品给予事件配置错误: itemToGive 或 InventoryManager 为 null");
        }
    }

    private void TriggerCustomEvent()
    {
        if (onDialogueEnd != null)
        {
            onDialogueEnd.Invoke();
            Debug.Log("触发自定义事件");
        }
    }

    // 重置交互状态（可选，用于重新触发事件）
    public void ResetInteraction()
    {
        hasInteracted = false;
        Debug.Log($"NPC {npcName} 交互状态已重置");
    }

    // 检查是否已经交互过
    public bool HasInteracted()
    {
        return hasInteracted;
    }
}