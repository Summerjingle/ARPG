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

    [HideInInspector] public UnityEvent onClick = new UnityEvent();

    private void Start()
    {
        button.onClick.AddListener(() => onClick.Invoke());
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
    }
}