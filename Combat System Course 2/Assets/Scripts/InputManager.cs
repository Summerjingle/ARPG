using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }
    

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Actions = new PlayerInputActions();

    }

    private void OnEnable()
    {
        Actions.Enable();
    }

    private void OnDisable()
    {
        Actions.Disable();
    }

    public void SwitchToMainMenuUI()
    {
       
        Actions.Player.Disable();
        Actions.UI_MainMenu.Enable();
        UIStateManager.SetUIActive(true); // 显示鼠标
    }

    public void SwitchToPlayer()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Enable();
        
        UIStateManager.SetUIActive(false); // 隐藏鼠标
    }
    public void SwitchToSaveMenu()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Disable();
        Actions.UI_SaveMenu.Enable();
        UIStateManager.SetUIActive(true); // 显示鼠标
    }
   
}
