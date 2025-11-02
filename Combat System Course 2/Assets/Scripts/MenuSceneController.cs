using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MenuSceneController : MonoBehaviour
{
    public Animator playerAnimator;
    public Transform portalTransform;
    public Button playButton;
    public Button loadButton;
    public TextMeshProUGUI Heading;
    public Button SettingButton;
    public Button QuitButton;
    public string gameSceneName = "00Scene_Village";
    public string loadingSceneName = "LoadingScene";
    public GameObject archivePanel;
   

    // 存档UI相关
    public Transform archiveContent; // content 对象的Transform
    public GameObject archivePrefab; // archive 预制体
    public GameObject loadConfirmPanel;

    private bool isStartingGame = false;
    private bool hasReachedPortal = false;
    private string selectedSaveId;

    void Start()
    {
        playButton.onClick.AddListener(StartNewGame);
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(ShowLoadPanel);
        }
        playerAnimator.SetBool("IsSitting", true);

        // 确保60FPS运行
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    public void StartNewGame()
    {
        if (!isStartingGame)
        {
            Debug.Log("开始新游戏 - 创建新存档");

            // 设置新游戏标志
            if (SaveManager.Instance != null)
            {
                // 直接创建新存档（不覆盖现有存档）
                // 这里传入-1表示自动选择空槽位
                SaveManager.Instance.CreateNewGame(-1);
            }

            isStartingGame = true;
            HideAllUI();
            StandUp();
        }
    }

   
    // 显示加载面板
    public void ShowLoadPanel()
    {
        if (archivePanel != null)
        {
            archivePanel.SetActive(true);
            Animator panelAnimator = archivePanel.GetComponent<Animator>();
            panelAnimator.SetBool("IsOpen", true);
            

            // 清空现有存档显示
            foreach (Transform child in archiveContent)
            {
                Destroy(child.gameObject);
            }

            // 显示所有已有存档
            List<GameSaveData> saves = SaveManager.Instance.GetAllSaves();
            foreach (GameSaveData saveData in saves.OrderByDescending(s => s.saveTime))
            {
                GameObject archiveItem = Instantiate(archivePrefab, archiveContent);
                ArchiveItemUI itemUI = archiveItem.GetComponent<ArchiveItemUI>();

                itemUI.SetArchiveData(saveData);
                itemUI.onClick.RemoveAllListeners();
                itemUI.onClick.AddListener(() => SelectLoadArchive(saveData.saveId));
            }

            // 如果没有存档，显示提示
            if (saves.Count == 0)
            {
                GameObject emptyText = new GameObject("EmptyText");
                emptyText.transform.SetParent(archiveContent);
                TextMeshProUGUI textComponent = emptyText.AddComponent<TextMeshProUGUI>();
                textComponent.text = "NoAnyArchive";
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.fontSize = 24;
            }
        }
    }

    public void CloseLoadPanel()
    {
        
        Animator panelAnimator = archivePanel.GetComponent<Animator>();
        panelAnimator.SetTrigger("IsClose");
        panelAnimator.SetBool("IsOpen", false);
        
    }

    // 选择加载存档
    private void SelectLoadArchive(string saveId)
    {
        selectedSaveId = saveId;
        if (loadConfirmPanel != null)
        {
            loadConfirmPanel.SetActive(true);
        }
    }

    // 确认加载
    public void ConfirmLoad()
    {
        if (!string.IsNullOrEmpty(selectedSaveId))
        {
            StartCoroutine(LoadGameCoroutine());
        }
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
    }

    // 取消选择
    public void CancelSelection()
    {
        selectedSaveId = null;
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
    }

    private IEnumerator LoadGameCoroutine()
    {
        if (!isStartingGame && !string.IsNullOrEmpty(selectedSaveId))
        {
            // 设置加载游戏标志
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

    // 原有的方法保持不变
    void Update()
    {
        if (isStartingGame && !hasReachedPortal)
        {
            RotateTowardsPortal();
        }
    }

    void RotateTowardsPortal()
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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Portal") && isStartingGame)
        {
            EnterPortal();
        }
    }

    void StandUp()
    {
        playerAnimator.SetBool("IsSitting", false);
        playerAnimator.SetTrigger("IsStandingUp");
        StartCoroutine(StartWalkingAfterStandUp());
    }

    IEnumerator StartWalkingAfterStandUp()
    {
        yield return new WaitForSeconds(0.01f);
        playerAnimator.SetBool("IsWalking", true);
    }

    void EnterPortal()
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

    private void HideAllUI()
    {
        playButton.gameObject.SetActive(false);
        if (loadButton != null) loadButton.gameObject.SetActive(false);
        SettingButton.gameObject.SetActive(false);
        QuitButton.gameObject.SetActive(false);
        Heading.gameObject.SetActive(false);

        // 关闭存档面板
        if (archivePanel != null) archivePanel.SetActive(false);
    }
}