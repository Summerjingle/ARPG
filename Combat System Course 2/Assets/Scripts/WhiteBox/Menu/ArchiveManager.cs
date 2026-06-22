using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ArchiveManager : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject archivePanel;
    public Transform archiveContent;
    public GameObject archivePrefab;
    public GameObject loadConfirmPanel;
    public GameObject deleteConfirmPanel;
    public CanvasGroup menuCanvas;

    [Header("导航")]
    public MenuListController menuListController;
    public ScrollRect scrollRect;
    public MenuListController mainMenuListController;

    [Header("场景设置")]
    public string gameSceneName = "WhiteBox_Village";
    public string loadingSceneName = "LoadingScene";

    private string selectedSaveId;
    private string deleteSaveId;
    private List<string> archiveSaveIds = new List<string>();
    private List<ArchiveItemUI> archiveItems = new List<ArchiveItemUI>();

    public string GetSelectedSaveId() => selectedSaveId;

    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnArchiveCancel += OnCancelPressed;
            InputManager.Instance.OnArchiveDelete += OnDeletePressed;
            InputManager.Instance.OnConfirmSubmit += OnConfirmSubmitPressed;
            InputManager.Instance.OnConfirmCancel += OnConfirmCancelPressed;
        }

        if (menuListController != null)
        {
            menuListController.OnSelectionChanged.AddListener(OnSelectionChanged);
            menuListController.OnSubmit.AddListener(OnSubmitPressed);
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnArchiveCancel -= OnCancelPressed;
            InputManager.Instance.OnArchiveDelete -= OnDeletePressed;
            InputManager.Instance.OnConfirmSubmit -= OnConfirmSubmitPressed;
            InputManager.Instance.OnConfirmCancel -= OnConfirmCancelPressed;
        }

        if (menuListController != null)
        {
            menuListController.OnSelectionChanged.RemoveListener(OnSelectionChanged);
            menuListController.OnSubmit.RemoveListener(OnSubmitPressed);
        }
    }

    public void ShowPanel()
    {
        if (archivePanel == null) return;

        menuCanvas.alpha = 0;
        menuCanvas.interactable = false;
        menuCanvas.blocksRaycasts = false;

        CancelSelection();
        CancelDelete();

        InputManager.Instance.SwitchToArchiveMenu();
        archivePanel.SetActive(true);

        if (mainMenuListController != null)
            mainMenuListController.enabled = false;

        Animator panelAnimator = archivePanel.GetComponent<Animator>();
        if (panelAnimator != null) panelAnimator.SetBool("IsOpen", true);

        ClearArchiveContent();
        PopulateArchiveList();

        if (menuListController != null)
            menuListController.index = 0;
    }

    public void HidePanel()
    {
        if (archivePanel == null) return;

        CancelSelection();
        CancelDelete();

        StartCoroutine(MenuLoom());
        InputManager.Instance.SwitchToMainMenuUI();

        if (mainMenuListController != null)
            mainMenuListController.enabled = true;

        Animator panelAnimator = archivePanel.GetComponent<Animator>();
        if (panelAnimator != null)
        {
            panelAnimator.SetTrigger("IsClose");
            panelAnimator.SetBool("IsOpen", false);
        }
        else
        {
            archivePanel.SetActive(false);
        }
    }

    private void ClearArchiveContent()
    {
        archiveSaveIds.Clear();
        archiveItems.Clear();
        foreach (Transform child in archiveContent)
            Destroy(child.gameObject);
    }

    public void PopulateArchiveList()
    {
        List<GameSaveData> saves = SaveManager.Instance.GetAllSaves();
        // Debug.Log($"[Archive] GetAllSaves 返回 {saves.Count} 条存档");

        int i = 0;
        foreach (GameSaveData saveData in saves.OrderByDescending(s => s.saveTime))
        {
            CreateArchiveItem(saveData, i);
            i++;
        }

        if (menuListController != null)
        {
            menuListController.maxIndex = Mathf.Max(0, saves.Count - 1);
            menuListController.index = 0;
        }

        if (saves.Count == 0)
            ShowEmptyArchiveMessage();
        else
            HighlightItem(0);
    }

    private void CreateArchiveItem(GameSaveData saveData, int index)
    {
        // Debug.Log($"[Archive] 创建条目: {saveData.saveId}, index={index}");
        GameObject archiveItem = Instantiate(archivePrefab, archiveContent);
        ArchiveItemUI itemUI = archiveItem.GetComponent<ArchiveItemUI>();

        archiveSaveIds.Add(saveData.saveId);
        archiveItems.Add(itemUI);

        itemUI.SetArchiveData(saveData);

        itemUI.onClick.RemoveAllListeners();
        itemUI.onClick.AddListener(() => SelectLoadArchive(saveData.saveId));

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

    private void HighlightItem(int index)
    {
        // Debug.Log($"[Archive] HighlightItem index={index}, archiveItems.Count={archiveItems.Count}");
        for (int i = 0; i < archiveItems.Count; i++)
        {
            if (archiveItems[i] == null) continue;

            bool isSelected = (i == index);
            archiveItems[i].SetHighlighted(isSelected);
            archiveItems[i].transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
        }

        ScrollToItem(index);
    }

    private void ScrollToItem(int index)
    {
        if (scrollRect == null || archiveItems.Count == 0) return;

        float t = archiveItems.Count > 1
            ? 1f - (float)index / (archiveItems.Count - 1)
            : 1f;
        scrollRect.verticalNormalizedPosition = t;
    }

    // === archive list input (UI_ArchiveMenu map) ===

    private void OnSelectionChanged(int index)
    {
        HighlightItem(index);
    }

    private void OnSubmitPressed(int index)
    {
        if (index >= 0 && index < archiveSaveIds.Count)
            SelectLoadArchive(archiveSaveIds[index]);
    }

    private void OnCancelPressed()
    {
        HidePanel();
    }

    private void OnDeletePressed()
    {
        // Debug.Log($"[Archive] OnDeletePressed, index={menuListController?.index}, saves={archiveSaveIds.Count}");
        if (menuListController != null &&
            menuListController.index >= 0 &&
            menuListController.index < archiveSaveIds.Count)
        {
            SelectDeleteArchive(archiveSaveIds[menuListController.index]);
        }
    }

    // === confirm panel input (UI_Confirm map) ===

    private void OnConfirmSubmitPressed()
    {
        if (loadConfirmPanel != null && loadConfirmPanel.activeSelf)
            ConfirmLoad();
        else if (deleteConfirmPanel != null && deleteConfirmPanel.activeSelf)
            ConfirmDelete();
    }

    private void OnConfirmCancelPressed()
    {
        if (loadConfirmPanel != null && loadConfirmPanel.activeSelf)
            CancelSelection();
        else if (deleteConfirmPanel != null && deleteConfirmPanel.activeSelf)
            CancelDelete();
    }

    // === archive selection ===

    private void SelectLoadArchive(string saveId)
    {
        CancelDelete();
        selectedSaveId = saveId;
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(true);
        if (menuListController != null) menuListController.enabled = false;
        InputManager.Instance.SwitchToConfirm();
    }

    private void SelectDeleteArchive(string saveId)
    {
        CancelSelection();
        deleteSaveId = saveId;
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true);
        if (menuListController != null) menuListController.enabled = false;
        InputManager.Instance.SwitchToConfirm();
    }

    // === confirm / cancel actions ===

    public void ConfirmLoad()
    {
        Debug.Log($"<color=red>[TRACE] ConfirmLoad called, frame={Time.frameCount}</color>");
        if (!string.IsNullOrEmpty(selectedSaveId))
        {
            SaveManager.Instance.LoadGame(selectedSaveId);
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SceneManager.LoadScene(loadingSceneName);
        }
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
        if (menuListController != null) menuListController.enabled = true;
        InputManager.Instance.SwitchToArchiveMenu();
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
        if (menuListController != null) menuListController.enabled = true;
        InputManager.Instance.SwitchToArchiveMenu();
    }

    public void CancelSelection()
    {
        selectedSaveId = null;
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(false);
        if (menuListController != null) menuListController.enabled = true;
        InputManager.Instance.SwitchToArchiveMenu();
    }

    public void CancelDelete()
    {
        deleteSaveId = null;
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
        if (menuListController != null) menuListController.enabled = true;
        InputManager.Instance.SwitchToArchiveMenu();
    }

    IEnumerator MenuLoom()
    {
        yield return new WaitForSeconds(0.5f);
        menuCanvas.alpha = 1;
        menuCanvas.interactable = true;
        menuCanvas.blocksRaycasts = true;
    }

#if UNITY_EDITOR
    [ContextMenu("生成测试存档 x3")]
    private void GenerateTestSaves()
    {
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "saves");
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        for (int i = 0; i < 3; i++)
        {
            var data = new GameSaveData(i);
            data.currentScene = "WhiteBox_Village";
            data.level = 10 + i * 5;
            data.currEXP = 1000 * (i + 1);
            data.hpValue = 100;
            data.maxHealth = 100;
            data.energyValue = 50;
            data.armorValue = 20;
            data.currCoins = 500 * (i + 1);
            data.saveName = $"存档 {i + 1}";
            data.saveSlot = i;
            data.saveTime = System.DateTime.Now.AddMinutes(-i * 30);

            string path = System.IO.Path.Combine(dir, $"{data.saveId}.json");
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"测试存档已生成: {path}");
        }
        Debug.Log("3 条测试存档生成完毕");
    }
#endif
}
