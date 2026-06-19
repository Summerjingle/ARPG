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

    public event Action OnGamePause;
    public event Action OnLock;
    public event Action OnBonfireExit;
    public event Action<bool> OnQuickItemModifierChanged;   // true=按下, false=松开
    public event Action<int> OnQuickItemNavigate;            // -1=左, 1=右
    public event Action OnItemDetailUse;
    public event Action OnItemDetailCancel;
    public event Action OnItemDetailSetQuickSlot;
    public event Action OnQuickUseConfirm;
    public event Action OnQuickUseCancel;
    public event Action OnQuickItemUse;




    
    

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
        Actions.Player.Lock.performed += _ => OnLock?.Invoke();

        Actions.Global.Bag.performed += _ => OnToggleInventory?.Invoke();
        Actions.Global.Pause.performed += _ => OnGamePause?.Invoke();

        Actions.UI_Inventory.Navigate.performed += ctx => OnUINavigate?.Invoke(ctx.ReadValue<Vector2>());
        Actions.UI_Inventory.Submit.performed += _ => OnUISubmit?.Invoke();
        Actions.UI_Inventory.Cancel.performed += _ => OnUICancel?.Invoke();

        Actions.UI_Inventory.SwitchLeft.performed+=ctx =>OnUISwitchLeft?.Invoke();
        Actions.UI_Inventory.SwitchRight.performed+=ctx =>OnUISwitchRight?.Invoke();

        Actions.UI_ItemDetail.Use.performed += _ => OnItemDetailUse?.Invoke();
        Actions.UI_ItemDetail.Cancel.performed += _ => OnItemDetailCancel?.Invoke();
        Actions.UI_ItemDetail.SetQuickSlot.performed += _ => OnItemDetailSetQuickSlot?.Invoke();

        Actions.UI_QuickUseBar.Confirm.performed += _ => OnQuickUseConfirm?.Invoke();
        Actions.UI_QuickUseBar.Cancel.performed += _ => OnQuickUseCancel?.Invoke();

        Actions.UI_PauseMenu.Cancel.performed+= _ =>OnGamePause?.Invoke();

        Actions.UI_BonfireMenu.Exit.performed += _ => OnBonfireExit?.Invoke();

        Actions.Player.QuickItemModifier.started += _ => OnQuickItemModifierChanged?.Invoke(true);
        Actions.Player.QuickItemModifier.canceled += _ => OnQuickItemModifierChanged?.Invoke(false);
        Actions.Player.QuickItemNavigate.performed += ctx =>
        {
            var v = ctx.ReadValue<Vector2>();
            int dir = 0;
            if (v.x > 0.5f || v.y > 0) dir = 1;
            else if (v.x < -0.5f || v.y < 0) dir = -1;
            if (dir != 0) OnQuickItemNavigate?.Invoke(dir);
        };

        Actions.Player.QuickItemUse.performed += _ => OnQuickItemUse?.Invoke();
        



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
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.Empty.Disable();
        Actions.UI_MainMenu.Enable();
        UIStateManager.SetUIActive(true);
    }

    public void SwitchToPlayer()
    {
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();

        Actions.Player.Enable();
        Actions.Global.Enable();
        UIStateManager.SetUIActive(false);
    }
    public void SwitchToSaveMenu()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.Empty.Disable();
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
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.Empty.Disable();

        // 启用背包专用的UI输入
        Actions.UI_Inventory.Enable();
        Actions.Global.Disable();
        UIStateManager.SetUIActive(true);
        
        Debug.Log("已切换到 Inventory UI 输入模式");
    }

    public void SwitchToItemDetail()
    {
        Actions.Player.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();
        Actions.Global.Disable();

        Actions.UI_ItemDetail.Enable();
        // Cursor 由 UIStateManager 统一管理，打开背包时已设为可见
    }

    public void SwitchToQuickUseBar()
    {
        Actions.Player.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();
        Actions.Global.Disable();

        Actions.UI_QuickUseBar.Enable();
    }

    public void SwitchToPauseMenu()
    {
        // 禁用所有游戏相关输入
        Actions.Player.Disable();
        Actions.Global.Disable();

        // 禁用其他UI（确保不会干扰）
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();

        // 启用暂停菜单UI输入
        Actions.UI_PauseMenu.Enable();
        
        UIStateManager.SetUIActive(true);
        Debug.Log("已切换到 Pause Menu 输入模式.");

    }

    public void SwitchToBonfireMenu()
    {
        Actions.Player.Disable();
        Actions.Global.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.Empty.Disable();

        Actions.UI_BonfireMenu.Enable();
        UIStateManager.SetUIActive(true);
    }

    public void SwitchToEmpty()
    {
        Actions.Player.Disable();
        Actions.Global.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_SaveMenu.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Enable();
        UIStateManager.SetUIActive(true);
    }
}
