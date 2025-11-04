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
    public Button deleteButton; // 新增删除按钮字段

    [HideInInspector] public UnityEvent onClick = new UnityEvent();

    private void Start()
    {
        button.onClick.AddListener(() => onClick.Invoke());

        // 如果有删除按钮，也为其添加监听
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

        // 设置按钮为已存档状态的颜色
        button.image.color = Color.white;
    }

    public void SetEmptySlot(int slotIndex)
    {
        dateText.text = "空存档槽";
        levelText.text = "---";
        sceneText.text = "---";
        healthText.text = "---";
        expText.text = "---";

        // 设置按钮为空槽位状态的颜色
        button.image.color = Color.gray;

        // 空槽位禁用删除按钮
        if (deleteButton != null)
        {
            deleteButton.interactable = false;
        }
    }

    // 新增：删除按钮点击事件
    private void OnDeleteButtonClicked()
    {
        // 这里可以添加删除逻辑
        // 由于我们直接在 MenuSceneController 中绑定了事件，这个方法可以留空
        // 或者您可以在这里添加一些视觉效果
        Debug.Log("删除按钮被点击");
    }
}