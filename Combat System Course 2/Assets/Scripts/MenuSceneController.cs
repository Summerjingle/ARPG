using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MenuSceneController : MonoBehaviour
{
    [Header("�������")]
    public Animator playerAnimator;
    public Transform portalTransform;

    [Header("UI����")]
    public Button playButton;
    public Button loadButton;
    public Button settingButton;
    public Button quitButton;
    public TextMeshProUGUI heading;

    [Header("��������")]
    public string gameSceneName = "00Scene_Village";
    public string loadingSceneName = "LoadingScene";

    [Header("�浵ϵͳ")]
    public GameObject archivePanel;
    public Transform archiveContent;
    public GameObject archivePrefab;
    public GameObject loadConfirmPanel;
    public GameObject deleteConfirmPanel;

    private bool isStartingGame = false;
    private bool hasReachedPortal = false;
    private string selectedSaveId;
    private string deleteSaveId;

    void Start()
    {
        

    // ǿ����ʾ�ͽ������
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    
    Debug.Log("���˵���������꣬��ʾ���");

        InitializeUI();
        playerAnimator.SetBool("IsSitting", true);

        // ȷ��60FPS����
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Update()
    {
        if (isStartingGame && !hasReachedPortal)
        {
            RotateTowardsPortal();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Portal") && isStartingGame)
        {
            EnterPortal();
        }
    }

    #region UI��ʼ��
    private void InitializeUI()
    {
        playButton.onClick.AddListener(StartNewGame);

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(ShowLoadPanel);
        }

        if (settingButton != null)
        {
            // ���ð�ť���ܿ�������������
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    #endregion

    #region ��Ϸ���̷���
    public void StartNewGame()
    {
        if (!isStartingGame)
        {
            Debug.Log("��ʼ����Ϸ - �����´浵");

            // ��������Ϸ��־
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CreateNewGame(-1);
            }

            isStartingGame = true;
            HideAllUI();
            StandUp();
        }
    }

    public void QuitGame()
    {
        Debug.Log("�˳���Ϸ");

        // �ڱ༭����ֹͣ���ţ��ڹ����汾���˳�Ӧ��
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private void StandUp()
    {
        playerAnimator.SetBool("IsSitting", false);
        playerAnimator.SetTrigger("IsStandingUp");
        StartCoroutine(StartWalkingAfterStandUp());
    }

    private IEnumerator StartWalkingAfterStandUp()
    {
        yield return new WaitForSeconds(0.01f);
        playerAnimator.SetBool("IsWalking", true);
    }

    private void RotateTowardsPortal()
    {
        Vector3 direction = (portalTransform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 3f
            );
        }
    }

    private void EnterPortal()
    {
        playerAnimator.SetBool("IsWalking", false);

        // ������Ϸ״̬
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        // ����Ŀ�곡��������
        PlayerPrefs.SetString("TargetScene", gameSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }
    #endregion

    #region �浵ϵͳ����
    public void ShowLoadPanel()
    {
        if (archivePanel != null)
        {
            archivePanel.SetActive(true);
            Animator panelAnimator = archivePanel.GetComponent<Animator>();
            panelAnimator.SetBool("IsOpen", true);

            ClearArchiveContent();
            PopulateArchiveList();
        }
    }

    public void CloseLoadPanel()
    {
        Animator panelAnimator = archivePanel.GetComponent<Animator>();
        panelAnimator.SetTrigger("IsClose");
        panelAnimator.SetBool("IsOpen", false);
    }

    private void ClearArchiveContent()
    {
        foreach (Transform child in archiveContent)
        {
            Destroy(child.gameObject);
        }
    }

    private void PopulateArchiveList()
    {
        List<GameSaveData> saves = SaveManager.Instance.GetAllSaves();

        foreach (GameSaveData saveData in saves.OrderByDescending(s => s.saveTime))
        {
            CreateArchiveItem(saveData);
        }

        if (saves.Count == 0)
        {
            ShowEmptyArchiveMessage();
        }
    }

    private void CreateArchiveItem(GameSaveData saveData)
    {
        GameObject archiveItem = Instantiate(archivePrefab, archiveContent);
        ArchiveItemUI itemUI = archiveItem.GetComponent<ArchiveItemUI>();

        itemUI.SetArchiveData(saveData);

        // �󶨼����¼�
        itemUI.onClick.RemoveAllListeners();
        itemUI.onClick.AddListener(() => SelectLoadArchive(saveData.saveId));

        // ��ɾ���¼�
        if (itemUI.deleteButton != null)
        {
            itemUI.deleteButton.onClick.RemoveAllListeners();
            itemUI.deleteButton.onClick.AddListener(() => SelectDeleteArchive(saveData.saveId));
        }
    }

    private void ShowEmptyArchiveMessage()
    {
        GameObject emptyText = new GameObject("EmptyText");
        emptyText.transform.SetParent(archiveContent);
        TextMeshProUGUI textComponent = emptyText.AddComponent<TextMeshProUGUI>();
        textComponent.text = "None";
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 24;
    }

    private void SelectLoadArchive(string saveId)
    {
        selectedSaveId = saveId;
        if (loadConfirmPanel != null)
        {
            loadConfirmPanel.SetActive(true);
        }
    }

    private void SelectDeleteArchive(string saveId)
    {
        deleteSaveId = saveId;
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
        }
    }

    public void ConfirmLoad()
    {
        if (!string.IsNullOrEmpty(selectedSaveId))
        {
            StartCoroutine(LoadGameCoroutine());
        }
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
    }

    public void ConfirmDelete()
    {
        if (!string.IsNullOrEmpty(deleteSaveId))
        {
            SaveManager.Instance.DeleteSave(deleteSaveId);
            ClearArchiveContent();
            PopulateArchiveList();
        }
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
        deleteSaveId = null;
    }

    public void CancelSelection()
    {
        selectedSaveId = null;
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
    }

    public void CancelDelete()
    {
        deleteSaveId = null;
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
    }

    private IEnumerator LoadGameCoroutine()
    {
        if (!isStartingGame && !string.IsNullOrEmpty(selectedSaveId))
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadGame(selectedSaveId);
            }

            isStartingGame = true;
            HideAllUI();
            StandUp();
        }
        yield return null;
    }
    #endregion

    #region ���߷���
    private void HideAllUI()
    {
        playButton.gameObject.SetActive(false);
        if (loadButton != null) loadButton.gameObject.SetActive(false);
        if (settingButton != null) settingButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (heading != null) heading.gameObject.SetActive(false);

        if (archivePanel != null) archivePanel.SetActive(false);
    }
    #endregion
}