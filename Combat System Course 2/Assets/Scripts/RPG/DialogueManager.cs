using UnityEngine;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;


    public CameraController cameraController;
    [SerializeField] private GameObject resumeTips;

    [Header("State")]
    private bool isDialogueActive;
    private Coroutine currentDialogue;
    private System.Action currentOnAccept;
    private System.Action currentOnReject;
    private System.Action currentOnComplete;
    private bool hasChoiceButtons;

    public bool IsDialogueActive => isDialogueActive;

    void Awake()
    {
        resumeTips.SetActive(true);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);

            return;
        }

        Instance = this;

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }
        DontDestroyOnLoad(gameObject);
        cameraController = Camera.main.GetComponent<CameraController>();
    }

    // 普通对话（不带选项）
    public void StartDialogue(string[] sentences)
    {

        StartDialogueInternal(sentences, null, null, null);
    }

    // 带选项的对话
    public void StartDialogueWithButtons(string[] sentences, System.Action onAccept, System.Action onReject)
    {

        StartDialogueInternal(sentences, onAccept, onReject, null);
    }

    // 带完成回调的对话
    public void StartDialogue(string[] sentences, System.Action onComplete)
    {

        StartDialogueInternal(sentences, null, null, onComplete);
    }

    // 内部启动对话方法
    private void StartDialogueInternal(string[] sentences, System.Action onAccept, System.Action onReject, System.Action onComplete)
    {
        resumeTips.SetActive(true);
        ForceStopDialogue();

        currentOnAccept = onAccept;
        currentOnReject = onReject;
        currentOnComplete = onComplete;
        hasChoiceButtons = (onAccept != null || onReject != null);

        currentDialogue = StartCoroutine(RunDialogueCoroutine(sentences));
    }

    private IEnumerator RunDialogueCoroutine(string[] sentences)
    {
        isDialogueActive = true;

        // 隐藏交互提示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInteractPrompt();
        }

        UIStateManager.SetUIActive(true);
        // 显示所有对话句子
        for (int i = 0; i < sentences.Length; i++)
        {
            // 显示当前句子
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDialogue(sentences[i]);
            }

            // 检查是否是最后一句且有选项
            bool isLastSentence = (i == sentences.Length - 1);
            bool hasOptions = hasChoiceButtons;

            if (isLastSentence && hasOptions)
            {
                resumeTips.SetActive(false);
                break;
            }

            // 如果不是最后一句，等待空格继续
            if (!isLastSentence)
            {
                yield return WaitForSpaceInput();
            }
        }

        // 显示选项按钮（如果有）
        if (hasChoiceButtons)
        {
            if (UIManager.Instance != null)
            {
                System.Action wrappedAccept = () =>
                {
                    currentOnAccept?.Invoke();
                    CleanupDialogue();
                };

                System.Action wrappedReject = () =>
                {
                    currentOnReject?.Invoke();
                    CleanupDialogue();
                };

                UIManager.Instance.ShowChoiceButtons(wrappedAccept, wrappedReject);
                Debug.Log("选项按钮已显示，等待玩家选择");
            }
            yield break;
        }
        else
        {
            // 普通对话：显示完最后一句后直接等待一次空格结束
            yield return WaitForSpaceInput();

            // 执行完成回调（如果存在）
            currentOnComplete?.Invoke();

            CleanupDialogue();
        }
    }

    // 等待空格输入
    private IEnumerator WaitForSpaceInput()
    {
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        yield return null;
    }

    // 结束对话
    public void EndDialogue()
    {
        ForceStopDialogue();
    }

    // 强制停止对话
    private void ForceStopDialogue()
    {
        if (currentDialogue != null)
        {
            StopCoroutine(currentDialogue);
            currentDialogue = null;
        }
        CleanupDialogue();
    }

    // 清理对话状态
    private void CleanupDialogue()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideDialogue();
            UIManager.Instance.HideChoiceButtons();
        }

        UIStateManager.SetUIActive(false);


        currentOnAccept = null;
        currentOnReject = null;
        currentOnComplete = null;
        hasChoiceButtons = false;
        isDialogueActive = false;

        Debug.Log("对话清理完成");
    }
  
}