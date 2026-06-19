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
    public GameObject selectionHighlight;
    public Selectable selectable;
    public void OnSubmit(BaseEventData eventData) => Unequip();
    private void Awake()
    {
        // Selectable必须挂在EquipmentSlotUI自己的GameObject上，不能用子级的
        selectable = GetComponent<Selectable>();
        if (selectable == null)
            selectable = gameObject.AddComponent<Selectable>();

        Navigation nav = selectable.navigation;
        nav.mode = Navigation.Mode.Vertical;
        selectable.navigation = nav;

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
        if (iconImage != null)
        {
            selectable.targetGraphic = iconImage;
        }

        UpdateIcon(null);
    }
    
    public void UpdateIcon(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
    
    public void SetHighlight(bool highlight)
    {
        selectionHighlight?.SetActive(highlight);
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
        EventSystem.current.SetSelectedGameObject(gameObject);
        Unequip();
        // 卸装后维持选中，防止跳回物品栏
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    private int _lastUnequipFrame = -1;

    private void Unequip()
    {
        // 防止同一帧内 Click + Submit 双重触发
        if (Time.frameCount == _lastUnequipFrame) return;
        _lastUnequipFrame = Time.frameCount;

        // 卸装前先拿到 ItemSO，卸装后 Weapon/Armor 实例会被销毁
        ItemSO unequippedItem = null;
        if (itemType == ItemType.Weapon)
        {
            unequippedItem = WeaponEquipmentManager.Instance?.GetCurrentWeapon()?.itemSO;
            WeaponEquipmentManager.Instance?.UnequipWeapon();
        }
        else if (itemType == ItemType.Armor)
        {
            unequippedItem = ArmorEquipmentManager.Instance?.GetEquippedItem(armorType);
            ArmorEquipmentManager.Instance?.UnequipArmor(armorType);
        }

        if (unequippedItem != null)
        {
            UpdateIcon(null);
            // ReAddItem 内部已做增量 UI（AddItem / UpdateItemAmountDisplay），无需再刷新
        }
    }
}