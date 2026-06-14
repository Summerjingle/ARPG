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
        //�ص����˵�����ݼ�esc����
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
        UIStateManager.SetUIActive(false);
        Time.timeScale = 1f;
        if (IsInGameScene())
            SaveManager.Instance.SaveGame(updatePosition: false);

        SaveManager.Instance.ResetSaveManagerState();
        SaveManager.shouldLoadFromSave = false;

        PlayerPrefs.SetString("TargetScene", "WhiteBox_Menu");
        PlayerPrefs.Save();

        SceneManager.LoadScene("LoadingScene");
    }
    private bool IsInGameScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return currentScene != "WhiteBox_Menu" && currentScene != "LoadingScene";
    }
    public void BackToMenuNo()//��No��ť��
    {
        PortalConfirm.SetActive(false);
        UIStateManager.SetUIActive(false);
    }
}
