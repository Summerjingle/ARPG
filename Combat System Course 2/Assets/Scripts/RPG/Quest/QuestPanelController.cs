using UnityEngine;
using TMPro;
using System.Collections;

public class QuestPanelController : MonoBehaviour
{
    [Header("主线任务面板")]
    [SerializeField] private GameObject mainMissionPanel;
    [SerializeField] private TMP_Text mainMissionNameText; // 现在将显示"任务名（状态）"
    [SerializeField] private TMP_Text mainMissionDetailText;
    

    [Header("支线任务面板")]
    [SerializeField] private GameObject sideMissionPanel;
    [SerializeField] private TMP_Text sideMissionNameText; // 现在将显示"任务名（状态）"
    [SerializeField] private TMP_Text sideMissionDetailText;
  

    [Header("设置")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 0.5f;

    private Quest currentMainQuest;
    private Quest currentSideQuest;
    private float updateTimer;

    // 单例模式
    public static QuestPanelController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.Log("销毁");
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 初始隐藏所有面板
        if (mainMissionPanel != null) mainMissionPanel.SetActive(false);
        if (sideMissionPanel != null) sideMissionPanel.SetActive(false);

    }

    private void Update()
    {
        if (autoUpdate)
        {
            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0)
            {
                UpdateAllPanels();
                updateTimer = updateInterval;
            }
        }
    }

    // 设置主线任务
    public void SetMainQuest(Quest quest)
    {
        if (quest == null || quest.questType != QuestType.Main) return;

        currentMainQuest = quest;
        UpdateMainQuestDisplay();

        if (mainMissionPanel != null)
        {
            mainMissionPanel.SetActive(true);
        }
    }

    // 设置支线任务
    public void SetSideQuest(Quest quest)
    {
        if (quest == null || quest.questType != QuestType.Side) return;

        currentSideQuest = quest;
        UpdateSideQuestDisplay();

        if (sideMissionPanel != null)
        {
            sideMissionPanel.SetActive(true);
        }
    }

    // 移除主线任务
    public void RemoveMainQuest()
    {
        currentMainQuest = null;
        if (mainMissionPanel != null)
        {
            mainMissionPanel.SetActive(false);
        }
    }

    // 移除支线任务
    public void RemoveSideQuest()
    {
        currentSideQuest = null;
        if (sideMissionPanel != null)
        {
            sideMissionPanel.SetActive(false);
        }
    }

    // 更新所有面板
    public void UpdateAllPanels()
    {
        UpdateMainQuestDisplay();
        UpdateSideQuestDisplay();
    }

    // 更新主线任务显示
    private void UpdateMainQuestDisplay()
    {
        if (currentMainQuest == null ||
            mainMissionNameText == null ||
            mainMissionDetailText == null)
        {
            if (mainMissionPanel != null) mainMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentMainQuest);
        mainMissionNameText.text = $"{currentMainQuest.questName} [主线] [{GetStateText(state)}]";
        mainMissionDetailText.text = currentMainQuest.description;

        // 如果主线任务已完成，延迟一段时间后移除
        if (state == QuestState.Completed)
        {
            StartCoroutine(RemoveMainQuestAfterDelay(2f));
        }
    }

    // 更新支线任务显示
    private void UpdateSideQuestDisplay()
    {
        if (currentSideQuest == null ||
            sideMissionNameText == null ||
            sideMissionDetailText == null)
        {
            if (sideMissionPanel != null) sideMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentSideQuest);
        sideMissionNameText.text = $"{currentSideQuest.questName} [支线] [{GetStateText(state)}]";
        sideMissionDetailText.text = currentSideQuest.description;

        // 如果支线任务已完成，延迟一段时间后移除
        if (state == QuestState.Completed)
        {
            StartCoroutine(RemoveSideQuestAfterDelay(2f));
        }
    }

    // 延迟移除主线任务
    private IEnumerator RemoveMainQuestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveMainQuest();
    }

    // 延迟移除支线任务
    private IEnumerator RemoveSideQuestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveSideQuest();
    }

    // 获取状态文本
    private string GetStateText(QuestState state)    {
        switch (state)
        {
            case QuestState.NotAccepted: return "未接受";
            case QuestState.InProgress: return "进行中";
            case QuestState.CanComplete: return "可完成";
            case QuestState.Completed: return "已完成";
            default: return "未知";
        }
    }

    // 检查是否有活跃的支线任务
    public bool HasActiveSideQuest()
    {
        return currentSideQuest != null &&
               QuestManager.Instance.GetQuestState(currentSideQuest) != QuestState.Completed;
    }
}