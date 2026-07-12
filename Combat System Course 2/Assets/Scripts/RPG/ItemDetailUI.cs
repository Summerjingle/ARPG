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
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI equipConditionText;
    public GameObject propertyGrid;
    public GameObject propertyTempate;

    public Button useButton;
    public Button cancelButton;
    public Button setQuickSlotButton;
    public SetQuickUseUI setQuickUseUI;
    public TextMeshProUGUI unusableText;

    [Header("导航")]
    [SerializeField] private VerticalMenuNavigator navigator;

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

        // 功能性回调（navigator 会额外注入 selection-sync 监听，互不干扰）
        useButton.onClick.AddListener(OnUseButtonClick);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        if (setQuickSlotButton != null)
            setQuickSlotButton.onClick.AddListener(OnSetQuickSlotButtonClick);
    }

    private void OnEnable()
    {
        InputManager.Instance.OnItemDetailNavigate += OnNavigate;
        InputManager.Instance.OnItemDetailUse += OnSubmit;
        InputManager.Instance.OnItemDetailCancel += OnCancel;
        InputManager.Instance.SwitchToItemDetail();

        // 阻止 EventSystem 导航穿透到背后的背包/装备区
        SetBackgroundInteractable(false);
    }

    private void OnDisable()
    {
        InputManager.Instance.OnItemDetailNavigate -= OnNavigate;
        InputManager.Instance.OnItemDetailUse -= OnSubmit;
        InputManager.Instance.OnItemDetailCancel -= OnCancel;
        InputManager.Instance.SwitchToInventory();

        // 恢复背后面板的交互
        SetBackgroundInteractable(true);
    }

    private void Start()
    {
        this.gameObject.SetActive(false);
    }

    public void UpdateDetailUI(ItemSO itemSO, ItemUI itemUI)
    {
        this.itemSO = itemSO;
        this.itemUI = itemUI;

        // 重置按钮可见性（默认全部显示，任务道具分支会覆盖）
        useButton.gameObject.SetActive(true);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
        if (unusableText != null) unusableText.gameObject.SetActive(false);

        string type = "";
        switch (itemSO.itemType)
        {
            case ItemType.Weapon:
                string category = "";
                if (itemSO is WeaponSO weapon)
                    category = weapon.weaponCategory.ToString();
                type = $"武器[{category}]";
                {
                    var cur = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
                    bool isEquipped = cur != null && cur.itemSO != null
                        && cur.itemSO.nameOfItem == itemSO.nameOfItem;
                    buttonText.text = isEquipped ? "卸载" : "装备";
                }
                break;
            case ItemType.Consumable:
                type = "消耗品";
                buttonText.text = "使用";
                break;
            case ItemType.Armor:
                type = "防具";
                {
                    bool armorEquipped = false;
                    if (itemSO is ArmorSO armor)
                    {
                        var equipped = ArmorEquipmentManager.Instance?.GetEquippedItem(armor.armorType);
                        armorEquipped = equipped != null && equipped.nameOfItem == itemSO.nameOfItem;
                    }
                    buttonText.text = armorEquipped ? "卸载" : "装备";
                }
                break;
            case ItemType.QuestRelated:
                type = "任务道具";
                // 任务道具不显示任何按钮，只显示提示文字
                useButton.gameObject.SetActive(false);
                if (setQuickSlotButton != null) setQuickSlotButton.gameObject.SetActive(false);
                if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                if (unusableText != null) unusableText.gameObject.SetActive(true);
                break;
        }

        // 快捷槽按钮：只有 Consumable 可用
        canQuickSlot = itemSO.itemType == ItemType.Consumable;
        if (setQuickSlotButton != null)
            setQuickSlotButton.gameObject.SetActive(canQuickSlot);

        if (canQuickSlot)
        {
            isItemInQuickSlot = QuickItemBar.Instance != null && QuickItemBar.Instance.HasItem(itemSO);
            if (quickSlotButtonText != null)
                quickSlotButtonText.text = isItemInQuickSlot ? "取消快捷道具" : "设为快捷道具";
        }

        iconImage.sprite = itemSO.icon;
        nameText.text = itemSO.nameOfItem;
        typeText.text = type;
        SetRarityDisplay(itemSO.rarity);
        descriptionText.text = itemSO.description;

        foreach (Transform child in propertyGrid.transform)
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
        if (itemSO is EquipmentSO eq)
            BuildEquipConditionText(eq);

        // 建立导航按钮列表后激活（保证 OnEnable 时 canQuickSlot 已确定）
        BuildButtonList();
        this.gameObject.SetActive(true);
    }

    /// <summary> 阻止/恢复物品区交互，防止导航穿透 </summary>
    private void SetBackgroundInteractable(bool interactable)
    {
        if (InventoryUI.Instance != null)
        {
            if (InventoryUI.Instance.itemsCanvasGroup != null)
                InventoryUI.Instance.itemsCanvasGroup.interactable = interactable;
        }
    }


    /// <summary> 根据 itemType 构建按钮列表并交给 navigator </summary>
    private void BuildButtonList()
    {
        var btns = new List<Button>();
        if (useButton.gameObject.activeSelf)
            btns.Add(useButton);
        if (canQuickSlot && setQuickSlotButton != null && setQuickSlotButton.gameObject.activeSelf)
            btns.Add(setQuickSlotButton);
        if (cancelButton != null && cancelButton.gameObject.activeSelf)
            btns.Add(cancelButton);

        navigator.SetButtons(btns);
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
            StatType.CurrHP => "生命值+",
            StatType.CurrEnergy => "精力值+",
            StatType.Strength => "力量",
            StatType.Luck => "幸运",
            _ => statType.ToString()
        };
    }

    private void SetRarityDisplay(Rarity rarity)
    {
        if (rarityText == null) return;

        var (name, color) = rarity switch
        {
            Rarity.Garbage   => ("垃圾",   new Color(0.5f, 0.5f, 0.5f)),   // 灰
            Rarity.Common    => ("普通",   Color.white),                    // 白
            Rarity.Uncommon  => ("精良",   new Color(0.3f, 0.85f, 0.3f)),  // 绿
            Rarity.Rare      => ("稀有",   new Color(0.3f, 0.5f, 1f)),     // 蓝
            Rarity.Epic      => ("史诗",   new Color(0.7f, 0.3f, 1f)),     // 紫
            Rarity.Legendary => ("传说",   new Color(1f, 0.85f, 0.1f)),    // 黄
            _                => ("",       Color.white)
        };

        rarityText.text = name;
        rarityText.color = color;
    }

    private void BuildEquipConditionText(EquipmentSO eq)
    {
        if (equipConditionText == null) return;

        if (eq.equipConditions == null || eq.equipConditions.Count == 0)
        {
            equipConditionText.gameObject.SetActive(false);
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        var pp = player != null ? player.GetComponent<PlayerProperty>() : null;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < eq.equipConditions.Count; i++)
        {
            var cond = eq.equipConditions[i];
            string statName = GetStatDisplayName(cond.statType).TrimEnd('+');
            bool met = pp != null && pp.GetStatValue(cond.statType) >= cond.requiredValue;
            string color = met ? "green" : "red";

            sb.Append($"需要{statName}: <color={color}>{cond.requiredValue}</color>");
            if (i < eq.equipConditions.Count - 1)
                sb.Append("\n");
        }

        equipConditionText.text = sb.ToString();
        equipConditionText.gameObject.SetActive(true);
    }

    // === 输入回调 ===

    private void OnNavigate(int direction)
    {
        navigator.Navigate(direction);
    }

    private void OnSubmit()
    {
        navigator.Submit();
    }

    private void OnCancel()
    {
        OnCancelButtonClick();
    }

    // === 按钮功能 ===

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
        if (itemSO.itemType == ItemType.QuestRelated)
        {
            MessageUI.Instance.Show("任务道具无法被使用");
            this.gameObject.SetActive(false);
            return;
        }

        // 已装备的武器/防具 → 卸载
        if (itemSO.itemType == ItemType.Weapon || itemSO.itemType == ItemType.Armor)
        {
            if (TryUnequip())
            {
                this.gameObject.SetActive(false);
                return;
            }
        }

        InventoryUI.Instance.OnItemUse(itemSO, itemUI);
        this.gameObject.SetActive(false);
    }

    private bool TryUnequip()
    {
        if (itemSO.itemType == ItemType.Weapon)
        {
            var cur = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
            if (cur != null && cur.itemSO != null && cur.itemSO.nameOfItem == itemSO.nameOfItem)
            {
                WeaponEquipmentManager.Instance.UnequipWeapon();
                itemUI?.UpdateQuickLight();
                BackpackCharacterDisplay.Instance?.ClearWeaponHighlight();
                InventoryUI.Instance?.RefreshQuickLightForItem(itemSO);
                InventoryUI.Instance?.RefreshStatusPanel();
                return true;
            }
        }
        else if (itemSO.itemType == ItemType.Armor && itemSO is ArmorSO armor)
        {
            var equipped = ArmorEquipmentManager.Instance?.GetEquippedItem(armor.armorType);
            if (equipped != null && equipped.nameOfItem == itemSO.nameOfItem)
            {
                ArmorEquipmentManager.Instance.UnequipArmor(armor.armorType);
                itemUI?.UpdateQuickLight();
                BackpackCharacterDisplay.Instance?.ClearArmorHighlight(armor.armorType);
                InventoryUI.Instance?.RefreshQuickLightForItem(itemSO);
                InventoryUI.Instance?.RefreshStatusPanel();
                return true;
            }
        }
        return false;
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
            QuickItemBar.Instance?.ClearSlotByItem(itemSO);
            isItemInQuickSlot = false;
            if (quickSlotButtonText != null)
                quickSlotButtonText.text = "设为快捷道具";
            InventoryUI.Instance?.RefreshQuickLightForItem(itemSO);
        }
        else
        {
            if (setQuickUseUI != null)
                setQuickUseUI.Open(itemSO);
        }
    }
}
