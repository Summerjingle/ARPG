using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 单属性行的UI引用容器
/// </summary>
[System.Serializable]
public class PropertyRowUI
{
    public AttributeType attributeType;
    public GameObject selectImage;         // 选中高亮条
    public TextMeshProUGUI currLevelText;  // 当前等级
    public TextMeshProUGUI targetLevelText;// 目标等级
    public GameObject updateImage;         // 升级箭头（+号图标）
}

/// <summary>
/// 能力升级面板控制器
/// 2D导航：上下切换属性行+按钮（MenuListController处理），左右调整目标升级等级
/// </summary>
public class AbilityUpgradePanelCtrl : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private MenuListController menuListController;
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference cancelAction;
    [SerializeField] private float deadZone = 0.5f;

    [Header("Property Rows (6 rows, 顺序: Vitality/Endurance/Strength/Agility/Defense/Luck)")]
    [SerializeField] private PropertyRowUI[] propertyRows;

    [Header("Buttons")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    [Header("Soul Display")]
    [SerializeField] private TextMeshProUGUI needSoulAmountText;
    [SerializeField] private TextMeshProUGUI currSoulAmountText;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Button Select Images")]
    [SerializeField] private GameObject btnConfirmSelectImg;
    [SerializeField] private GameObject btnCancelSelectImg;

    // 每个属性排队的升级次数
    private Dictionary<AttributeType, int> targetDeltas;
    private bool horizontalInputLocked;
    private bool returnToBonfireOnHide;

    private InputAction Navigate => navigateAction?.action;
    private InputAction CancelAction => cancelAction?.action;

    #region Lifecycle

    private void Awake()
    {
        targetDeltas = new Dictionary<AttributeType, int>();
        foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            targetDeltas[type] = 0;
    }

    private void Start()
    {
        // 初始隐藏面板
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // 初始隐藏所有 UpdateImage 和 SelectImage
        if (propertyRows != null)
        {
            foreach (var row in propertyRows)
            {
                if (row.updateImage != null)
                    row.updateImage.SetActive(false);
                if (row.selectImage != null)
                    row.selectImage.SetActive(false);
            }
        }

        if (btnConfirmSelectImg != null)
            btnConfirmSelectImg.SetActive(false);
        if (btnCancelSelectImg != null)
            btnCancelSelectImg.SetActive(false);
    }

    private void OnEnable()
    {
        if (Navigate != null)
        {
            Navigate.performed += OnNavigate;
            Navigate.canceled += OnNavigateCanceled;
        }
        if (CancelAction != null)
            CancelAction.performed += OnCancelPerformed;

        if (menuListController != null)
        {
            menuListController.OnSubmit.AddListener(OnMenuSubmit);
            menuListController.OnSelectionChanged.AddListener(OnSelectionChanged);
        }

        if (btnConfirm != null) btnConfirm.onClick.AddListener(Confirm);
        if (btnCancel != null) btnCancel.onClick.AddListener(Cancel);
    }

    private void OnDisable()
    {
        if (Navigate != null)
        {
            Navigate.performed -= OnNavigate;
            Navigate.canceled -= OnNavigateCanceled;
        }
        if (CancelAction != null)
            CancelAction.performed -= OnCancelPerformed;

        if (menuListController != null)
        {
            menuListController.OnSubmit.RemoveListener(OnMenuSubmit);
            menuListController.OnSelectionChanged.RemoveListener(OnSelectionChanged);
        }

        if (btnConfirm != null) btnConfirm.onClick.RemoveListener(Confirm);
        if (btnCancel != null) btnCancel.onClick.RemoveListener(Cancel);
    }

    #endregion

    #region Show / Hide

    public void Show()
    {
        // 重置所有排队
        foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            targetDeltas[type] = 0;

        if (menuListController != null)
        {
            menuListController.maxIndex = 7; // 6属性 + 确认 + 取消
            menuListController.index = 0;
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        InputManager.Instance?.SwitchToAbilityUpgrade();
        RefreshAllRows();
        UpdateSoulDisplay();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (returnToBonfireOnHide)
        {
            returnToBonfireOnHide = false;
            BonfirePanelCtrl.Instance?.ReopenPanel();
            InputManager.Instance?.SwitchToBonfireMenu();
        }
        else
        {
            InputManager.Instance?.SwitchToPlayer();
        }
    }

    /// <summary> 从篝火面板打开升级面板，取消后回到篝火菜单 </summary>
    public void ShowFromBonfire()
    {
        Debug.Log("[AbilityUpgradePanelCtrl] ShowFromBonfire called");
        returnToBonfireOnHide = true;
        Show();
    }

    #endregion

    #region Input

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (horizontalInputLocked) return;

        Vector2 dir = ctx.ReadValue<Vector2>();
        if (Mathf.Abs(dir.x) < deadZone) return;

        if (menuListController == null) return;
        int idx = menuListController.index;

        // 仅属性行（0-5）响应左右键
        if (idx < 0 || idx >= (propertyRows != null ? propertyRows.Length : 0)) return;

        int delta = dir.x > 0 ? 1 : -1;
        AdjustTargetLevel(propertyRows[idx].attributeType, delta);
        horizontalInputLocked = true;
    }

    private void OnNavigateCanceled(InputAction.CallbackContext ctx)
    {
        horizontalInputLocked = false;
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        Cancel();
    }

    private void OnMenuSubmit(int index)
    {
        if (index == 6) Confirm();
        else if (index == 7) Cancel();
    }

    private void OnSelectionChanged(int index)
    {
        RefreshRowHighlights();
    }

    #endregion

    #region Core Logic

    private void AdjustTargetLevel(AttributeType type, int delta)
    {
        if (PlayerProperty.Instance == null) return;

        int currentLevel = PlayerProperty.Instance.GetAttributeLevel(type);
        int newDelta = targetDeltas[type] + delta;

        if (newDelta < 0) return;                      // Left 到 0 就停
        if (currentLevel + newDelta > 99) return;      // Right 到 99 上限

        targetDeltas[type] = newDelta;
        RefreshRow(type);
        UpdateSoulDisplay();
    }

    private void Confirm()
    {
        PlayerProperty prop = PlayerProperty.Instance;
        if (prop == null) return;

        int totalNeed = CalculateTotalCost();
        if (prop.currSoulAmount < totalNeed) return;

        // 逐级执行升级，每级cost自动按递增后的Level计算
        foreach (var kvp in targetDeltas)
        {
            for (int i = 0; i < kvp.Value; i++)
                prop.UpgradeAttribute(kvp.Key);
        }

        // 重置排队数据
        foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            targetDeltas[type] = 0;

        RefreshAllRows();
        UpdateSoulDisplay();
        PlayerHUDUI.Instance?.UpdateSoulAmount();
    }

    private void Cancel()
    {
        foreach (AttributeType type in Enum.GetValues(typeof(AttributeType)))
            targetDeltas[type] = 0;

        RefreshAllRows();
        UpdateSoulDisplay();
        Hide();
    }

    #endregion

    #region Cost Calculation

    /// <summary>
    /// 计算所有排队升级的总消耗魂量。
    /// 公式：cost = Σ 10*(L+i)³ + 100*(L+i)² + 500*(L+i)
    /// L = 当前总等级，i = 0..totalQueued-1
    /// </summary>
    private int CalculateTotalCost()
    {
        int totalQueued = 0;
        foreach (var kvp in targetDeltas)
            totalQueued += kvp.Value;

        if (totalQueued == 0) return 0;

        int totalCost = 0;
        int startLevel = PlayerProperty.Instance != null ? PlayerProperty.Instance.Level : 1;

        for (int i = 0; i < totalQueued; i++)
        {
            int x = startLevel + i;
            totalCost += 10 * x * x * x + 100 * x * x + 500 * x;
        }

        return totalCost;
    }

    #endregion

    #region UI Refresh

    private void RefreshRow(AttributeType type)
    {
        if (PlayerProperty.Instance == null) return;

        PropertyRowUI row = null;
        if (propertyRows != null)
        {
            foreach (var r in propertyRows)
            {
                if (r.attributeType == type) { row = r; break; }
            }
        }
        if (row == null) return;

        int cur = PlayerProperty.Instance.GetAttributeLevel(type);
        int delta = targetDeltas[type];

        if (row.currLevelText != null)
            row.currLevelText.text = cur.ToString();
        if (row.targetLevelText != null)
            row.targetLevelText.text = (cur + delta).ToString();
        if (row.updateImage != null)
            row.updateImage.SetActive(delta > 0);
    }

    private void RefreshAllRows()
    {
        if (propertyRows == null) return;
        foreach (var row in propertyRows)
            RefreshRow(row.attributeType);
        RefreshRowHighlights();
    }

    private void RefreshRowHighlights()
    {
        if (propertyRows == null) return;
        int sel = menuListController != null ? menuListController.index : -1;

        for (int i = 0; i < propertyRows.Length; i++)
        {
            if (propertyRows[i].selectImage != null)
                propertyRows[i].selectImage.SetActive(i == sel);
        }

        // 按钮高亮：index 6 = 确认, index 7 = 取消
        if (btnConfirmSelectImg != null)
            btnConfirmSelectImg.SetActive(sel == 6);
        if (btnCancelSelectImg != null)
            btnCancelSelectImg.SetActive(sel == 7);
    }

    private void UpdateSoulDisplay()
    {
        int need = CalculateTotalCost();
        int curr = PlayerProperty.Instance != null ? PlayerProperty.Instance.currSoulAmount : 0;

        if (needSoulAmountText != null)
            needSoulAmountText.text = $"消耗魂量：{need}";
        if (currSoulAmountText != null)
            currSoulAmountText.text = $"当前魂量：{curr}";
    }

    #endregion
}
