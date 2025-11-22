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
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 自动获取 Animator 引用
        RefreshAnimators();
    }

    private void Update()
    {
        if (!autoUpdate) return;

        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            UpdateAllPanels();
            updateTimer = updateInterval;
        }
    }

    #region 公共方法

    public void SetMainQuest(Quest quest)
    {
        if (quest == null || quest.questType != QuestType.Main) return;

        currentMainQuest = quest;
        isMainQuestCompleting = false;
        UpdateMainQuestDisplay();

        if (mainMissionPanel != null) mainMissionPanel.SetActive(true);
        PlayAnimatorSafe(mainMissionAnimator, "MissionActive");
    }

    public void SetSideQuest(Quest quest)
    {
        if (quest == null || quest.questType != QuestType.Side) return;

        currentSideQuest = quest;
        isSideQuestCompleting = false;
        UpdateSideQuestDisplay();

        if (sideMissionPanel != null) sideMissionPanel.SetActive(true);
        PlayAnimatorSafe(sideMissionAnimator, "MissionActive");
    }

    public void RemoveMainQuest()
    {
        currentMainQuest = null;
        if (mainMissionPanel != null) mainMissionPanel.SetActive(false);
        isMainQuestCompleting = false;
    }

    public void RemoveSideQuest()
    {
        currentSideQuest = null;
        if (sideMissionPanel != null) sideMissionPanel.SetActive(false);
        isSideQuestCompleting = false;
    }

    public void UpdateAllPanels()
    {
        UpdateMainQuestDisplay();
        UpdateSideQuestDisplay();
    }

    /// <summary>
    /// 强制停止所有任务面板动画（场景切换安全）
    /// </summary>
    public void StopAllAnimations()
    {
        StopAllCoroutines();
        RefreshAnimators();

        ResetAnimatorSafe(mainMissionAnimator);
        ResetAnimatorSafe(sideMissionAnimator);

        isMainQuestCompleting = false;
        isSideQuestCompleting = false;
    }

    #endregion

    #region 私有方法

    private void RefreshAnimators()
    {
        if (mainMissionPanel != null && mainMissionAnimator == null)
            mainMissionAnimator = mainMissionPanel.GetComponent<Animator>();
        if (sideMissionPanel != null && sideMissionAnimator == null)
            sideMissionAnimator = sideMissionPanel.GetComponent<Animator>();
    }

    private void ResetAnimatorSafe(Animator animator)
    {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;
        animator.ResetTrigger("MissionCompleted");
    }

    private void PlayAnimatorSafe(Animator animator, string stateName)
    {
        if (animator == null) return;
        if (animator.runtimeAnimatorController == null) return;
        animator.ResetTrigger("MissionCompleted");
        animator.Play(stateName);
    }

    private void UpdateMainQuestDisplay()
    {
        if (currentMainQuest == null || mainMissionNameText == null || mainMissionDetailText == null)
        {
            if (mainMissionPanel != null && !isMainQuestCompleting)
                mainMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentMainQuest);
        mainMissionNameText.text = $"{currentMainQuest.questName}[主线][{GetStateText(state)}]";
        mainMissionDetailText.text = currentMainQuest.description;

        if (state == QuestState.Completed && !isMainQuestCompleting)
        {
            StartCoroutine(CompleteMainQuestWithAnimation());
        }
    }

    private void UpdateSideQuestDisplay()
    {
        if (currentSideQuest == null || sideMissionNameText == null || sideMissionDetailText == null)
        {
            if (sideMissionPanel != null && !isSideQuestCompleting)
                sideMissionPanel.SetActive(false);
            return;
        }

        QuestState state = QuestManager.Instance.GetQuestState(currentSideQuest);
        sideMissionNameText.text = $"{currentSideQuest.questName}[支线][{GetStateText(state)}]";
        sideMissionDetailText.text = currentSideQuest.description;

        if (state == QuestState.Completed && !isSideQuestCompleting)
        {
            StartCoroutine(CompleteSideQuestWithAnimation());
        }
    }

    private IEnumerator CompleteMainQuestWithAnimation()
    {
        isMainQuestCompleting = true;
        PlayAnimatorSafe(mainMissionAnimator, "MissionCompleted");
        yield return new WaitForSeconds(1.5f);
        RemoveMainQuest();
    }

    private IEnumerator CompleteSideQuestWithAnimation()
    {
        isSideQuestCompleting = true;
        PlayAnimatorSafe(sideMissionAnimator, "MissionCompleted");
        yield return new WaitForSeconds(1.5f);
        RemoveSideQuest();
    }

    private string GetStateText(QuestState state)
    {
        return state switch
        {
            QuestState.NotAccepted => "未接受",
            QuestState.InProgress => "进行中",
            QuestState.CanComplete => "可完成",
            QuestState.Completed => "已完成",
            _ => "未知",
        };
    }

    #endregion
}
