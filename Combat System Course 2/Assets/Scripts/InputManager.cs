using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }
    public event System.Action OnAttack;
    public event System.Action OnInteract;
    public event System.Action ToggleWeapon;



    
    

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
        Actions.Player.Attack.performed += _ => OnAttack?.Invoke();

        Actions.Player.Interact.performed += _ => OnInteract?.Invoke();
        
        Actions.Player.DrawWeapon.performed += _ => ToggleWeapon?.Invoke();


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
        UIStateManager.SetUIActive(true); // ��ʾ���
    }

    public void SwitchToPlayer()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Enable();
        
        UIStateManager.SetUIActive(false); // �������
    }
    public void SwitchToSaveMenu()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Disable();
        Actions.UI_SaveMenu.Enable();
        UIStateManager.SetUIActive(true); // ��ʾ���
    }
   
}
