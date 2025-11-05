using UnityEngine;
using TMPro;
using System.Collections;

public class QuestPanelController : MonoBehaviour
{
    [Header("主线任务面板")]
    [SerializeField] private GameObject mainMissionPanel;
    [SerializeField] private TMP_Text mainMissionNameText;
    [SerializeField] private TMP_Text mainMissionDetailText;
    [SerializeField] private Animator mainMissionAnimator; 

    [Header("支线任务面板")]
    [SerializeField] private GameObject sideMissionPanel;
    [SerializeField] private TMP_Text sideMissionNameText;
    [SerializeField] private TMP_Text sideMissionDetailText;
    [SerializeField] private Animator sideMissionAnimator; 

    [Header("设置")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 0.5f;

    private Quest currentMainQuest;
    private Quest currentSideQuest;
    private float updateTimer;
    private bool isMainQuestCompleting = false;
    private bool isSideQuestCompleting = false;

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

        // 自动获取Animator
        if (mainMissionAnimator == null && mainMissionPanel != null)
            mainMissionAnimator = mainMissionPanel.GetComponent<Animator>();
        if (sideMissionAnimator == null && sideMissionPanel != null)
            sideMissionAnimator = sideMissionPanel.GetComponent<Animator>();
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
        isMainQuestCompleting = false;
        UpdateMainQuestDisplay();

        if (mainMissionPanel != null)
        {
            mainMissionPanel.SetActive(true);
            // 重置动画状态
            if (mainMissionAnimator != null)
            {
                mainMissionAnimator.ResetTrigger("MissionCompleted");
                mainMissionAnimator.Play("MissionActive");
            }
        }
    }

    // 设置支线任务
    public void SetSideQuest(Quest quest)
    {
        if (quest == null || quest.questType != QuestType.Side) return;

        currentSideQuest = quest;
        isSideQuestCompleting = false;
        UpdateSideQuestDisplay();

        if (sideMissionPanel != null)
        {
            sideMissionPanel.SetActive(true);
            // 重置动画状态
            if (sideMissionAnimator != null)
            {
                sideMissionAnimator.ResetTrigger("MissionCompleted");
                sideMissionAnimator.Play("MissionActive");
            }
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
        isMainQuestCompleting = false;
    }

    // 移除支线任务
    public void RemoveSideQuest()
    {
        currentSideQuest = null;
        if (sideMissionPanel != null)
        {
            sideMissionPanel.SetActive(false);
        }
        isSideQuestCompleting = false;
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
            if (mainMissionPanel != null && !isMainQuestCompleting)
                mainMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentMainQuest);
        mainMissionNameText.text = $"{currentMainQuest.questName}[主线][{GetStateText(state)}]";
        mainMissionDetailText.text = currentMainQuest.description;

        // 如果主线任务已完成且还没开始完成动画
        if (state == QuestState.Completed && !isMainQuestCompleting)
        {
            StartCoroutine(CompleteMainQuestWithAnimation());
        }
    }

    // 更新支线任务显示
    private void UpdateSideQuestDisplay()
    {
        if (currentSideQuest == null ||
            sideMissionNameText == null ||
            sideMissionDetailText == null)
        {
            if (sideMissionPanel != null && !isSideQuestCompleting)
                sideMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentSideQuest);
        sideMissionNameText.text = $"{currentSideQuest.questName}[支线][{GetStateText(state)}]";
        sideMissionDetailText.text = currentSideQuest.description;

        // 如果支线任务已完成且还没开始完成动画
        if (state == QuestState.Completed && !isSideQuestCompleting)
        {
            StartCoroutine(CompleteSideQuestWithAnimation());
        }
    }

    // 主线任务完成动画流程
    private IEnumerator CompleteMainQuestWithAnimation()
    {
        isMainQuestCompleting = true;

        // 触发完成动画
        if (mainMissionAnimator != null)
        {
            mainMissionAnimator.SetTrigger("MissionCompleted");
            Debug.Log("触发主线任务完成动画");
        }

        // 等待动画播放完成（你可以根据动画长度调整时间）
        yield return new WaitForSeconds(1.5f); // 假设动画长度1.5秒

        // 动画播放完成后移除面板
        RemoveMainQuest();
    }

    // 支线任务完成动画流程
    private IEnumerator CompleteSideQuestWithAnimation()
    {
        isSideQuestCompleting = true;

        // 触发完成动画
        if (sideMissionAnimator != null)
        {
            sideMissionAnimator.SetTrigger("MissionCompleted");
            Debug.Log("触发支线任务完成动画");
        }

        // 等待动画播放完成
        yield return new WaitForSeconds(1.5f); // 假设动画长度1.5秒

        // 动画播放完成后移除面板
        RemoveSideQuest();
    }

    // 获取状态文本
    private string GetStateText(QuestState state)
    {
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

    // 强制停止所有动画（在场景切换等情况下使用）
    public void StopAllAnimations()
    {
        StopAllCoroutines();

        if (mainMissionAnimator != null)
        {
            mainMissionAnimator.ResetTrigger("MissionCompleted");
        }

        if (sideMissionAnimator != null)
        {
            sideMissionAnimator.ResetTrigger("MissionCompleted");
        }

        isMainQuestCompleting = false;
        isSideQuestCompleting = false;
    }
}