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
    public void BackToMenuYes()
    {
        PortalConfirm.SetActive(false);
        UIStateManager.SetUIActive(false);

        // 保存当前存档（如果在游戏中）
        if (IsInGameScene())
            SaveManager.Instance.SaveGame();

        // 关键：真正重置所有状态
        SaveManager.Instance.ResetSaveManagerState();

        // 不加载存档，走默认流程
        SaveManager.shouldLoadFromSave = false;

        PlayerPrefs.SetString("TargetScene", "000Scene_Menu");
        PlayerPrefs.Save();

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
