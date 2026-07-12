using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("装备类型")]
    public ItemType itemType;
    public ArmorType armorType;

    [Header("UI组件")]
    public Image iconImage;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>();
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
}
