// EquipmentSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    [Header("装备类型")]
    public ItemType itemType;
    public ArmorType armorType;
    
    [Header("UI组件")]
    public Image iconImage;
    public Sprite selectionSprite;
    public Selectable selectable;

    [Header("高亮设置")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private Sprite originalIconSprite;
    public void OnSubmit(BaseEventData eventData) => Unequip();
    private void Awake()
    {
        if (selectable == null)
            selectable = GetComponentInChildren<Selectable>();

        if (selectable == null)
            selectable = gameObject.AddComponent<Selectable>();

        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.Automatic;
        selectable.navigation = nav;

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            selectable.targetGraphic = iconImage;
            originalIconSprite = iconImage.sprite;
        }

        UpdateIcon(null);
    }
    
    public void UpdateIcon(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            originalIconSprite = icon;
        }
    }
    
    public void SetHighlight(bool highlight)
    {
        if (iconImage == null) return;

        if (highlight && selectionSprite != null)
        {
            iconImage.sprite = selectionSprite;
        }
        else
        {
            iconImage.sprite = originalIconSprite;
        }

        iconImage.color = highlight ? selectedColor : normalColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlight(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlight(false);
    }
    
    public void OnSubmit()
    {
        Unequip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Unequip();
    }

    private void Unequip()
    {
        bool hasUnequipped = false;
        if (itemType == ItemType.Weapon)
        {
            WeaponEquipmentManager.Instance?.UnequipWeapon();
            hasUnequipped = true;
        }
        else if (itemType == ItemType.Armor)
        {
            ArmorEquipmentManager.Instance?.UnequipArmor(armorType);
            hasUnequipped = true;
        }

        if (hasUnequipped)
        {
            UpdateIcon(null);
            // 触发 InventoryUI 刷新，确保卸下的物品回到背包第一格
            InventoryUI.Instance.UpdateInventoryUI();
        }
    }
}