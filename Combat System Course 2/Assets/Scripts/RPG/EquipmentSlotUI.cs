// EquipmentSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("装备类型")]
    public ItemType itemType;
    public ArmorType armorType;
    
    [Header("UI组件")]
    public Image iconImage;
    public Selectable selectable;
    
    [Header("高亮设置")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    private void Awake()
    {
        if (selectable == null)
            selectable = GetComponent<Selectable>();
        
        if (iconImage == null)
            iconImage = GetComponent<Image>();
        
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
        if (iconImage != null)
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
        if (itemType == ItemType.Weapon)
        {
            WeaponEquipmentManager.Instance?.UnequipWeapon();
        }
        else if (itemType == ItemType.Armor)
        {
            ArmorEquipmentManager.Instance?.UnequipArmor(armorType);
        }
    }
}