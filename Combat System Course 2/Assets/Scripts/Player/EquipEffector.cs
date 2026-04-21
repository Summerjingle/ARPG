using UnityEngine;

public class EquipEffector : MonoBehaviour
{
    public Animator HelmetAnimator;
    public Animator ChestAnimator;
    public Animator ArmAnimator;
    public Animator LegAnimator;
    public Animator FootAnimator;

    // 记录上一次各部位是否装备的状态
    private bool lastHelmetEquipped;
    private bool lastChestEquipped;
    private bool lastArmEquipped;
    private bool lastLegEquipped;
    private bool lastFootEquipped;

    private void OnEnable()
    {
        if (ArmorEquipmentManager.Instance != null)
            ArmorEquipmentManager.Instance.OnEquipmentChanged += UpdateEquipEffect;
    }

    private void OnDisable()
    {
        if (ArmorEquipmentManager.Instance != null)
            ArmorEquipmentManager.Instance.OnEquipmentChanged -= UpdateEquipEffect;
    }

    void Start()
    {
        // 首次初始化记录状态
        CacheCurrentStates();
        UpdateEquipEffect();
    }

    private void CacheCurrentStates()
    {
        lastHelmetEquipped = IsSlotEquipped(ArmorType.Helmet);
        lastChestEquipped  = IsSlotEquipped(ArmorType.Chestplate);
        lastArmEquipped    = IsSlotEquipped(ArmorType.Gauntlets);
        lastLegEquipped    = IsSlotEquipped(ArmorType.Leggings);
        lastFootEquipped   = IsSlotEquipped(ArmorType.Boots);
    }

    private bool IsSlotEquipped(ArmorType type)
    {
        var socket = ArmorEquipmentManager.Instance?.GetSocketByType(type);
        if (socket == null) return false;

        return socket.isSymmetric ? (socket.leftEquipment != null) : (socket.currentEquipment != null);
    }

    public void UpdateEquipEffect()
    {
        if (ArmorEquipmentManager.Instance == null) return;

        CheckSlot(ArmorType.Helmet, HelmetAnimator, ref lastHelmetEquipped);
        CheckSlot(ArmorType.Chestplate, ChestAnimator, ref lastChestEquipped);
        CheckSlot(ArmorType.Gauntlets, ArmAnimator, ref lastArmEquipped);
        CheckSlot(ArmorType.Leggings, LegAnimator, ref lastLegEquipped);
        CheckSlot(ArmorType.Boots, FootAnimator, ref lastFootEquipped);
    }

    private void CheckSlot(ArmorType type, Animator anim, ref bool lastState)
    {
        if (anim == null) return;

        var socket = ArmorEquipmentManager.Instance.GetSocketByType(type);
        if (socket == null) return;

        bool currentEquipped = socket.isSymmetric ? 
            (socket.leftEquipment != null) : (socket.currentEquipment != null);

        // 仅在状态发生变化时才触发动画
        if (currentEquipped != lastState)
        {
            if (currentEquipped)
                anim.SetTrigger("Equiped");
            else
                anim.SetTrigger("UnEquip");

            lastState = currentEquipped;
            Debug.Log($"{type} 状态变化: {(currentEquipped ? "已装备" : "已卸下")}");
        }
    }
}