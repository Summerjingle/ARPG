using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("对话UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("交互提示UI")]
    [SerializeField] private GameObject interactPrompt;
    


    [Header("选项按钮UI")]
    [SerializeField] private GameObject choiceButtons;
    [SerializeField] private UnityEngine.UI.Button acceptButton;
    [SerializeField] private UnityEngine.UI.Button rejectButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 显示对话
    public void ShowDialogue(string text)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }

    // 隐藏对话
    public void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // 显示交互提示
    public void ShowInteractPrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
        }
    }

    // 隐藏交互提示
    public void HideInteractPrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
    }

    // 显示选项按钮
    public void ShowChoiceButtons(System.Action onAccept, System.Action onReject)
    {
        if (choiceButtons != null)
        {
            choiceButtons.SetActive(true);

            // 设置按钮点击事件
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(() => {
                    onAccept?.Invoke();
                    HideChoiceButtons();
                });
            }

            if (rejectButton != null)
            {
                rejectButton.onClick.RemoveAllListeners();
                rejectButton.onClick.AddListener(() => {
                    onReject?.Invoke();
                    HideChoiceButtons();
                });
            }
        }
    }

    // 隐藏选项按钮
    public void HideChoiceButtons()
    {
        if (choiceButtons != null)
        {
            choiceButtons.SetActive(false);
        }
    }
}