using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MessageUI : MonoBehaviour
{
    public GameObject PortalConfirm;
    public static MessageUI Instance {  get; private set; }
    private TextMeshProUGUI messageText;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        messageText=transform.Find("Text").GetComponent<TextMeshProUGUI>();
        Hide();
    }

    public void Update()
    {
        //回到主菜单（快捷键esc键）
        if (Input.GetKeyDown(KeyCode.H))
        {
            
            
                PortalConfirm.SetActive(true);
                UIStateManager.SetUIActive(true);
            

        }
        if (messageText.enabled)
        {
            Color color=messageText.color;
            float alpha = Mathf.Lerp(color.a, 0, Time.deltaTime);
            messageText.color= new Color(color.r, color.g, color.b,alpha);
            if (alpha == 0) 
            { 
                messageText.enabled = false;
            }
        }
    }

    public void Show(string message)
    {
         messageText.enabled = true;
        messageText.text = message;
        messageText.color = Color.white;
    }
    public void Hide() 
    { 
        messageText.enabled = false;
    }
    public void BackToMenuYes()//供Yes按钮绑定
    {
        PortalConfirm.SetActive(false);
        UIStateManager.SetUIActive(false);
        // 保存当前游戏（如果有）
        if (IsInGameScene() && SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        // 关键：清空所有单例数据
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ClearInventory();
            Debug.Log("返回主菜单：InventoryManager 已清空");
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetAllQuests();
            Debug.Log("返回主菜单：QuestManager 已重置");
        }
        if (WeaponEquipmentManager.Instance != null)
        {
            WeaponEquipmentManager.Instance.UnequipWeapon();
            Debug.Log("返回主菜单：武器装备已重置");
        }
        if (ArmorEquipmentManager.Instance != null)
        {

            ArmorEquipmentManager.Instance.UnequipAll();
            Debug.Log("返回主菜单：护甲装备已重置");
        }

        SaveManager.Instance.currentSaveData.level = 1;
        SaveManager.Instance.currentSaveData.currEXP = 0;
        SaveManager.Instance.currentSaveData.hpValue = 100;
        SaveManager.Instance.currentSaveData.maxHealth = 100;
        SaveManager.Instance.currentSaveData.energyValue = 100;
        SaveManager.Instance.currentSaveData.armorValue = 0;

        // 重置装备信息
        SaveManager.Instance.currentSaveData.equippedWeapon = "";
        SaveManager.Instance.currentSaveData.equippedHelmet = "";
        SaveManager.Instance.currentSaveData.equippedChestplate = "";
        SaveManager.Instance.currentSaveData.equippedGauntlets = "";
        SaveManager.Instance.currentSaveData.equippedLeggings = "";
        SaveManager.Instance.currentSaveData.equippedBoots = "";

        // 清空场景物品拾取状态
        SaveManager.Instance.currentSaveData.scenePickedItems.Clear();

        // 清空机关激活状态
        SaveManager.Instance.currentSaveData.sceneMechanismStates.Clear();


        PlayerPrefs.SetString("TargetScene", "000Scene_Menu");
        PlayerPrefs.Save();
        SaveManager.shouldLoadFromSave = false;
        SceneManager.LoadScene("LoadingScene");
    }
    private bool IsInGameScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene != "000Scene_Menu" && currentScene != "LoadingScene";
    }
    public void BackToMenuNo()//供No按钮绑定
    {
        PortalConfirm.SetActive(false);
        UIStateManager.SetUIActive(false);
    }
}
