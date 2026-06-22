using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;

public class ArchiveItemUI : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI sceneText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI expText;
    public Button button;
    public Button deleteButton;
    public GameObject selectionOutline; // 选中高亮边框

    [HideInInspector] public UnityEvent onClick = new UnityEvent();

    private void Start()
    {
        button.onClick.AddListener(() => onClick.Invoke());

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => OnDeleteButtonClicked());
        }
    }

    public void SetArchiveData(GameSaveData saveData)
    {
        dateText.text = saveData.saveTime.ToString("yyyy-MM-dd HH:mm");
        levelText.text = $"Lv. {saveData.level}";
        sceneText.text = saveData.currentScene;
        healthText.text = $"{saveData.hpValue}/{saveData.maxHealth}";
        expText.text = saveData.currEXP.ToString();

        button.image.color = Color.white;
    }

    public void SetEmptySlot(int slotIndex)
    {
        dateText.text = "空存档位";
        levelText.text = "---";
        sceneText.text = "---";
        healthText.text = "---";
        expText.text = "---";

        button.image.color = Color.gray;

        if (deleteButton != null)
        {
            deleteButton.interactable = false;
        }
    }

    /// <summary>
    /// 设置选中高亮状态
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        if (selectionOutline != null)
            selectionOutline.SetActive(highlighted);
    }

    private void OnDeleteButtonClicked()
    {
        Debug.Log("删除按钮被点击");
    }
}
