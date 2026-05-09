using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ArchiveManager : MonoBehaviour
{
    [Header("UI����")]
    public GameObject archivePanel;
    public Transform archiveContent;
    public GameObject archivePrefab;
    public GameObject loadConfirmPanel;
    public GameObject deleteConfirmPanel;
   public CanvasGroup menuCanvas;
    [Header("��������")]
    public string gameSceneName = "WhiteBox_Village";
    public string loadingSceneName = "LoadingScene";

    private string selectedSaveId;
    private string deleteSaveId;

    // ��ȡѡ�еĴ浵ID
    public string GetSelectedSaveId() => selectedSaveId;

    // ��ʾ�浵���
    public void ShowPanel()
    {
        
        if (archivePanel == null) return;
        menuCanvas.alpha = 0;
        InputManager.Instance.SwitchToSaveMenu();
        archivePanel.SetActive(true);
        Animator panelAnimator = archivePanel.GetComponent<Animator>();
        if (panelAnimator != null) panelAnimator.SetBool("IsOpen", true);
        
        ClearArchiveContent();
        PopulateArchiveList();
    }

    // ���ش浵���
    public void HidePanel()
    {

        if (archivePanel == null) return;
       StartCoroutine(MenuLoom());
        InputManager.Instance.SwitchToMainMenuUI();
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

    // ��վ��б�
    private void ClearArchiveContent()
    {
        foreach (Transform child in archiveContent)
            Destroy(child.gameObject);
    }

    // ˢ�´浵�б�
    public void PopulateArchiveList()
    {
        List<GameSaveData> saves = SaveManager.Instance.GetAllSaves();

        foreach (GameSaveData saveData in saves.OrderByDescending(s => s.saveTime))
            CreateArchiveItem(saveData);

        if (saves.Count == 0) ShowEmptyArchiveMessage();
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
        if (loadConfirmPanel != null) loadConfirmPanel.SetActive(true);
    }

    private void SelectDeleteArchive(string saveId)
    {
        deleteSaveId = saveId;
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true);
    }

    public void ConfirmLoad()
    {
        if (!string.IsNullOrEmpty(selectedSaveId))
        {
            SaveManager.Instance.LoadGame(selectedSaveId);
            PlayerPrefs.SetString("TargetScene", gameSceneName);
            SceneManager.LoadScene(loadingSceneName);
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

    IEnumerator MenuLoom()
    {
        yield return new WaitForSeconds(0.5f);
        menuCanvas.alpha = 1;
    }
}