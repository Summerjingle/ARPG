using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MenuSceneController : MonoBehaviour
{
    [Header("玩家引用")]
    public Animator playerAnimator;
    public Transform portalTransform;

    [Header("UI引用")]
    public Button playButton;
    public Button loadButton;
    public Button settingButton;
    public Button quitButton;
    public TextMeshProUGUI heading;

    [Header("场景设置")]
    public string gameSceneName = "00Scene_Village";
    public string loadingSceneName = "LoadingScene";

    [Header("存档系统")]
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
        

    // 强制显示和解锁鼠标
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    
    Debug.Log("主菜单：解锁鼠标，显示光标");

        InitializeUI();
        playerAnimator.SetBool("IsSitting", true);

        // 确保60FPS运行
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

    #region UI初始化
    private void InitializeUI()
    {
        playButton.onClick.AddListener(StartNewGame);

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(ShowLoadPanel);
        }

        if (settingButton != null)
        {
            // 设置按钮功能可以在这里添加
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }
    #endregion

    #region 游戏流程方法
    public void StartNewGame()
    {
        if (!isStartingGame)
        {
            Debug.Log("开始新游戏 - 创建新存档");

            // 设置新游戏标志
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
        Debug.Log("退出游戏");

        // 在编辑器中停止播放，在构建版本中退出应用
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

        // 保存游戏状态
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        // 设置目标场景并加载
        PlayerPrefs.SetString("TargetScene", gameSceneName);
        SceneManager.LoadScene(loadingSceneName);
    }
    #endregion

    #region 存档系统方法
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

        // 绑定加载事件
        itemUI.onClick.RemoveAllListeners();
        itemUI.onClick.AddListener(() => SelectLoadArchive(saveData.saveId));

        // 绑定删除事件
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
            ShowLoadPanel(); // 刷新存档列表
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

    #region 工具方法
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