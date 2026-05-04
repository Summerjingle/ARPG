using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }
    public event Action<bool> OnDeviceChanged;//设备切换：手柄、键鼠
    public bool IsUsingGamepad { get; private set; }
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
        InputSystem.onActionChange += DetectDeviceChange;
    }

    private void OnDisable()
    {
        Actions.Disable();
        InputSystem.onActionChange -= DetectDeviceChange;
    }
    private void DetectDeviceChange(object obj, InputActionChange change)
    {
        // 只有当有按键被按下/触发时，才检测设备
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl?.device;

            if (device != null)
            {
                // 判断当前触发的设备是不是手柄
                bool isGamepad = device is Gamepad;

                // 如果设备状态发生了变化，就触发事件
                if (isGamepad != IsUsingGamepad)
                {
                    IsUsingGamepad = isGamepad;
                    OnDeviceChanged?.Invoke(IsUsingGamepad);
                    Debug.Log("输入设备已切换，当前是否为手柄：" + IsUsingGamepad);
                }
            }
        }
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
        Actions.UI_Inventory.Disable();
        
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
