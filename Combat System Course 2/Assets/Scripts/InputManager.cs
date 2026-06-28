using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-100)]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public PlayerInputActions Actions { get; private set; }
    public event Action<bool> OnDeviceChanged;
    public bool IsUsingGamepad { get; private set; }
    public event Action OnAttack;
    public event Action OnBlock;
    public event Action OnInteract;
    public event Action ToggleWeapon;
    public event Action OnToggleInventory;
    Action<Vector2> OnUINavigate;
    public event Action OnUISubmit;
    public event Action OnUICancel;
    public event Action OnUISwitchLeft;
    public event Action OnUISwitchRight;

    public event Action OnGamePause;
    public event Action OnLock;
    public event Action OnBonfireExit;
    public event Action<Vector2> OnBonfireNavigate;
    public event Action OnBonfireSubmit;
    public event Action OnArchiveCancel;
    public event Action OnArchiveDelete;
    public event Action OnConfirmSubmit;
    public event Action OnConfirmCancel;
    public event Action<bool> OnQuickItemModifierChanged;
    public event Action<int> OnQuickItemNavigate;
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
        Actions.Player.Block.performed += _ => OnBlock?.Invoke();
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
        Actions.UI_BonfireMenu.Navigate.performed += ctx => OnBonfireNavigate?.Invoke(ctx.ReadValue<Vector2>());
        Actions.UI_BonfireMenu.Submit.performed += _ => OnBonfireSubmit?.Invoke();

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

        Actions.UI_ArchiveMenu.Cancel.performed += _ => OnArchiveCancel?.Invoke();
        Actions.UI_ArchiveMenu.Delete.performed += _ => OnArchiveDelete?.Invoke();

        Actions.UI_Confirm.Submit.performed += _ => OnConfirmSubmit?.Invoke();
        Actions.UI_Confirm.Cancel.performed += _ => OnConfirmCancel?.Invoke();
    }

    private void OnEnable()
    {
        InputSystem.onActionChange += DetectDeviceChange;
    }

    private void OnDisable()
    {
        Actions.Disable();
        InputSystem.onActionChange -= DetectDeviceChange;
    }

    private void DetectDeviceChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            InputAction action = (InputAction)obj;
            InputDevice device = action.activeControl?.device;

            if (device != null)
            {
                bool isGamepad = device is Gamepad;

                if (isGamepad != IsUsingGamepad)
                {
                    IsUsingGamepad = isGamepad;
                    OnDeviceChanged?.Invoke(IsUsingGamepad);
                    // Debug.Log("input device switched, is gamepad: " + IsUsingGamepad);
                }
            }
        }
    }

    private void EnableExclusive(InputActionMap map)
    {
        map.Disable();
        map.Enable();
    }

    public void SwitchToMainMenuUI()
    {
        Actions.Player.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.Empty.Disable();

        EnableExclusive(Actions.UI_MainMenu);
        UIStateManager.SetUIActive(true);
    }

    public void SwitchToPlayer()
    {
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();

        EnableExclusive(Actions.Player);
        EnableExclusive(Actions.Global);
        UIStateManager.SetUIActive(false);
    }

    public void SwitchToArchiveMenu()
    {
        Actions.UI_MainMenu.Disable();
        Actions.Player.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.Empty.Disable();
        Actions.UI_Confirm.Disable();

        EnableExclusive(Actions.UI_ArchiveMenu);
        UIStateManager.SetUIActive(true);
    }

    public void SwitchToConfirm()
    {
        Actions.Player.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.Empty.Disable();

        EnableExclusive(Actions.UI_Confirm);
    }

    public void SwitchToInventory()
    {
        Actions.Player.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.Empty.Disable();
        Actions.Global.Disable();

        EnableExclusive(Actions.UI_Inventory);
        UIStateManager.SetUIActive(true);
        Debug.Log("switched to Inventory UI input mode");
    }

    public void SwitchToItemDetail()
    {
        Actions.Player.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();
        Actions.Global.Disable();

        EnableExclusive(Actions.UI_ItemDetail);
    }

    public void SwitchToQuickUseBar()
    {
        Actions.Player.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();
        Actions.Global.Disable();

        EnableExclusive(Actions.UI_QuickUseBar);
    }

    public void SwitchToPauseMenu()
    {
        Actions.Player.Disable();
        Actions.Global.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_BonfireMenu.Disable();
        Actions.Empty.Disable();

        EnableExclusive(Actions.UI_PauseMenu);
        UIStateManager.SetUIActive(true);
        Debug.Log("switched to Pause Menu input mode.");
    }

    public void SwitchToBonfireMenu()
    {
        Actions.Player.Disable();
        Actions.Global.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.Empty.Disable();

        EnableExclusive(Actions.UI_BonfireMenu);
        UIStateManager.SetUIActive(true);
    }

    public void SwitchToEmpty()
    {
        Actions.Player.Disable();
        Actions.Global.Disable();
        Actions.UI_MainMenu.Disable();
        Actions.UI_ArchiveMenu.Disable();
        Actions.UI_Confirm.Disable();
        Actions.UI_Inventory.Disable();
        Actions.UI_ItemDetail.Disable();
        Actions.UI_QuickUseBar.Disable();
        Actions.UI_PauseMenu.Disable();
        Actions.UI_BonfireMenu.Disable();

        EnableExclusive(Actions.Empty);
        UIStateManager.SetUIActive(true);
    }
}
