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
    public event System.Action OnToggleInventory;
    public event System.Action<Vector2> OnUINavigate;
    public event System.Action OnUISubmit;   
    public event System.Action OnUICancel;
    public event Action OnUISwitchLeft;
    public event Action OnUISwitchRight;



    
    

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
        
        Actions.Player.DrawWeapon.started += _ => ToggleWeapon?.Invoke();

        Actions.Global.Bag.performed += _ => OnToggleInventory?.Invoke();
        Actions.UI_Inventory.Navigate.performed += ctx => OnUINavigate?.Invoke(ctx.ReadValue<Vector2>());

        Actions.UI_Inventory.Submit.performed += _ => OnUISubmit?.Invoke();
        Actions.UI_Inventory.Cancel.performed += _ => OnUICancel?.Invoke();

        Actions.UI_Inventory.SwitchLeft.performed+=ctx =>OnUISwitchLeft?.Invoke();
        Actions.UI_Inventory.SwitchRight.performed+=ctx =>OnUISwitchRight?.Invoke();



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
    public void SwitchToInventory()
    {
        // 禁用玩家输入
        Actions.Player.Disable();
        
        // 禁用其他UI（防止冲突）
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        
        // 启用背包专用的UI输入（如果你以后要做Navigate/Submit/Cancel）
        Actions.UI_Inventory.Enable();
        
        // Global 通常保持启用（因为Bag键还在Global里）
        Actions.Global.Enable();

        UIStateManager.SetUIActive(true);
        
        Debug.Log("已切换到 Inventory UI 输入模式");
    }

    // 新增：从背包切换回玩家模式（推荐和ToggleInventory配合使用）
    public void SwitchToPlayerFromInventory()
    {
        Actions.UI_Inventory.Disable();
        Actions.Player.Enable();
        Actions.Global.Enable();     // 保持Global可用
        
        UIStateManager.SetUIActive(false);
        
        Debug.Log("已切换回 Player 输入模式");
    }
   
}
