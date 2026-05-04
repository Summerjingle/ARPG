using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("�Ի�UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("������ʾUI")]
   [SerializeField] private LootUIPollower lootUIFollower;
    public PickupToastUI pickupToast;



    [Header("ѡ�ťUI")]
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

    // ��ʾ�Ի�
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

    // ���ضԻ�
    public void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // ��ʾ������ʾ
    public void ShowInteractPrompt(Transform targetTransform,ItemSO item = null)
    {
        if (lootUIFollower != null)
        {
            lootUIFollower.SetTarget(targetTransform,item);
        }
    }

    // ���ؽ�����ʾ
    public void HideInteractPrompt()
    {
        if (lootUIFollower != null)
        {
            lootUIFollower.SetTarget(null,null);
        }
    }

    // ��ʾѡ�ť
    public void ShowChoiceButtons(System.Action onAccept, System.Action onReject)
    {
        if (choiceButtons != null)
        {
            choiceButtons.SetActive(true);

            // ���ð�ť����¼�
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

    // ����ѡ�ť
    public void HideChoiceButtons()
    {
        if (choiceButtons != null)
        {
            choiceButtons.SetActive(false);
        }
    }
    public void ShowPickupToast(ItemSO item)
    {
        pickupToast.Show(item, 2f);
    }
}