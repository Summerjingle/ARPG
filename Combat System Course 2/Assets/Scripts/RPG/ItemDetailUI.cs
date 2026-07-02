using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemDetailUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI descriptionText;
    public GameObject propertyGrid;
    public GameObject propertyTempate;

    public Button useButton;
    public Button cancelButton;
    public Button setQuickSlotButton;
    public SetQuickUseUI setQuickUseUI;

    private TextMeshProUGUI buttonText;
    private TextMeshProUGUI quickSlotButtonText;
    private ItemSO itemSO;
    private ItemUI itemUI;
    private bool isItemInQuickSlot;
    private bool canQuickSlot;

    private void Awake()
    {
        buttonText = useButton.GetComponentInChildren<TextMeshProUGUI>();
        quickSlotButtonText = setQuickSlotButton?.GetComponentInChildren<TextMeshProUGUI>();
        propertyTempate.SetActive(false);

        // 鼠标点击支持
        useButton.onClick.AddListener(OnUseButtonClick);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        if (setQuickSlotButton != null)
            setQuickSlotButton.onClick.AddListener(OnSetQuickSlotButtonClick);
    }

    private void OnEnable()
    {
        InputManager.Instance.OnItemDetailUse += OnUseButtonClick;
        InputManager.Instance.OnItemDetailCancel += OnCancelButtonClick;
        InputManager.Instance.OnItemDetailSetQuickSlot += OnSetQuickSlotButtonClick;
        InputManager.Instance.SwitchToItemDetail();

        // 默认选中 useButton
        if (useButton != null)
            EventSystem.current.SetSelectedGameObject(useButton.gameObject);
    }

    private void OnDisable()
    {
        InputManager.Instance.OnItemDetailUse -= OnUseButtonClick;
        InputManager.Instance.OnItemDetailCancel -= OnCancelButtonClick;
        InputManager.Instance.OnItemDetailSetQuickSlot -= OnSetQuickSlotButtonClick;
        InputManager.Instance.SwitchToInventory();
    }

    private void Start()
    {
        this.gameObject.SetActive(false);
    }
    public void UpdateDetailUI(ItemSO itemSO, ItemUI itemUI)
    {
        this.itemSO = itemSO;
        this.itemUI = itemUI;
        this.gameObject.SetActive(true);

        string type = "";
        switch (itemSO.itemType)
        {
            case ItemType.Weapon:
                type = "武器";
                buttonText.text="装备";
                break;
            case ItemType.Consumable:
                type = "消耗品";
                buttonText.text="使用";
                break;
            case ItemType.Armor:
                type = "防具";
                buttonText.text="装备";
                break;
            case ItemType.QuestRelated:
                type = "任务道具";
                buttonText.text="使用";
                break;

        }
        // 快捷槽按钮：只有 Weapon 和 Consumable 可用
        canQuickSlot = itemSO.itemType == ItemType.Consumable;
        if (setQuickSlotButton != null)
            setQuickSlotButton.gameObject.SetActive(canQuickSlot);

        if (canQuickSlot)
        {
            isItemInQuickSlot = QuickItemBar.Instance != null && QuickItemBar.Instance.HasItem(itemSO);
            if (quickSlotButtonText != null)
                quickSlotButtonText.text = isItemInQuickSlot ? "取消快捷道具" : "设为快捷道具";
        }

        iconImage.sprite=itemSO.icon;
        nameText.text=itemSO.nameOfItem;
        typeText.text= type;
        descriptionText.text=itemSO.description;

        foreach (Transform child  in propertyGrid.transform)
        {
            if (child.gameObject.activeSelf)
            {
                Destroy(child.gameObject);
            }
        }

        // 获取属性列表：装备用 propertyList，消耗品用 effects
        List<Property> propertiesToShow = null;
        if (itemSO is EquipmentSO equipment)
            propertiesToShow = equipment.propertyList;
        else if (itemSO is ConsumableSO consumable)
            propertiesToShow = consumable.effects;

        if (propertiesToShow != null)
        {
            foreach (Property property in propertiesToShow)
            {
                string propertyName = GetStatDisplayName(property.statType);
                string propertyStr = propertyName + property.value;

                GameObject go = GameObject.Instantiate(propertyTempate);
                go.SetActive(true);
                go.transform.SetParent(propertyGrid.transform);
                go.transform.Find("Property").GetComponent<TextMeshProUGUI>().text = propertyStr;
            }
        }

        // 装备条件显示
        if (itemSO is EquipmentSO eq && eq.equipConditions != null && eq.equipConditions.Count > 0)
        {
            foreach (EquipCondition cond in eq.equipConditions)
            {
                string condStr = $"需要 {GetStatDisplayName(cond.statType)} >= {cond.requiredValue}";
                GameObject go = GameObject.Instantiate(propertyTempate);
                go.SetActive(true);
                go.transform.SetParent(propertyGrid.transform);
                go.transform.Find("Property").GetComponent<TextMeshProUGUI>().text = condStr;
            }
        }
    }

    private string GetStatDisplayName(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHP => "最大生命值+",
            StatType.MaxEnergy => "最大精力值+",
            StatType.Defense => "护甲值",
            StatType.CritRate => "暴击率+",
            StatType.CritDamage => "暴击伤害+",
            StatType.CurrHP => "恢复生命值",
            StatType.CurrEnergy => "恢复精力值",
            StatType.Strength => "力量",
            StatType.Luck => "幸运",
            _ => statType.ToString()
        };
    }

    public void OnCancelButtonClick()
    {
        this.gameObject.SetActive(false);
        if (InventoryUI.Instance.currentSelectedItem != null)
        {
            EventSystem.current.SetSelectedGameObject(InventoryUI.Instance.currentSelectedItem.gameObject);
        }
    }

    public void OnUseButtonClick()
    {
        // 先检查是否为任务道具
        if (itemSO.itemType == ItemType.QuestRelated)
        {
            MessageUI.Instance.Show("任务道具无法被使用");
            this.gameObject.SetActive(false);
            return;
        }

        // 只有符合条件的物品才执行使用逻辑
        InventoryUI.Instance.OnItemUse(itemSO, itemUI);
        this.gameObject.SetActive(false);
    }

    /// <summary> SetQuickUseUI 关闭时回调，刷新按钮状态 </summary>
    public void RefreshQuickSlotState()
    {
        if (itemSO == null) return;
        isItemInQuickSlot = QuickItemBar.Instance != null && QuickItemBar.Instance.HasItem(itemSO);
        if (quickSlotButtonText != null)
            quickSlotButtonText.text = isItemInQuickSlot ? "取消快捷道具" : "设为快捷道具";
    }

    public void OnSetQuickSlotButtonClick()
    {
        if (itemSO == null) return;
        if (!canQuickSlot) return;

        if (isItemInQuickSlot)
        {
            // 已设为快捷道具 → 取消
            QuickItemBar.Instance?.ClearSlotByItem(itemSO);
            isItemInQuickSlot = false;
            if (quickSlotButtonText != null)
                quickSlotButtonText.text = "设为快捷道具";
            InventoryUI.Instance?.RefreshQuickLightForItem(itemSO);
        }
        else
        {
            // 未设置 → 打开选择面板
            if (setQuickUseUI != null)
                setQuickUseUI.Open(itemSO);
        }
    }
}
