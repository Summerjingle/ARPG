using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausePanelCtrl : MonoBehaviour
{
    public GameObject pausePanel;
    public AudioClip pauseSound;
    public bool isGamePaused=false;
    void OnEnable()
    {
        InputManager.Instance.OnGamePause+= TogglePauseMenu;
    }
    void OnDisable()
    {
        InputManager.Instance.OnGamePause-=TogglePauseMenu;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TogglePauseMenu()
    {   
         
        if (pauseSound != null)
            AudioManager.Instance.PlayUI(pauseSound);
        
        isGamePaused = !isGamePaused;
        pausePanel.SetActive(isGamePaused);
        Time.timeScale = isGamePaused ? 0 : 1;
        if (isGamePaused)
            {
                // 暂停时切换到暂停菜单输入模式
                InputManager.Instance.SwitchToPauseMenu();
            }
            else
            {
                // 恢复时切换回玩家输入模式
                InputManager.Instance.SwitchToPlayer();
            }
    }
}
