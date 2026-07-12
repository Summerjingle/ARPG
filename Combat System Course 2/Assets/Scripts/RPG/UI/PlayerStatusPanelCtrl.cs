using TMPro;
using UnityEngine;

/// <summary>
/// 背包内的角色状态面板（只读）。
/// </summary>
public class PlayerStatusPanelCtrl : MonoBehaviour
{
    [Header("即时值")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI defenseText;

    [Header("属性等级")]
    [SerializeField] private TextMeshProUGUI vitalityText;
    [SerializeField] private TextMeshProUGUI enduranceText;
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI agilityText;
    [SerializeField] private TextMeshProUGUI defenseLevelText;
    [SerializeField] private TextMeshProUGUI luckText;

    public void RefreshDisplay()
    {
        var prop = PlayerProperty.Instance;
        if (prop == null) return;

        if (levelText != null)
            levelText.text = prop.Level.ToString();

        var health = prop.GetComponent<HealthSystem>();
        if (hpText != null && health != null)
            hpText.text = $"{health.Health:F0} / {health.MaxHealth:F0}";
        if (energyText != null)
            energyText.text = $"{prop.EnergyValue:F0} / {prop.MaxEnergy:F0}";
        if (defenseText != null)
            defenseText.text = prop.ArmorValue.ToString();

        if (vitalityText != null)
            vitalityText.text = prop.GetAttributeLevel(AttributeType.Vitality).ToString();
        if (enduranceText != null)
            enduranceText.text = prop.GetAttributeLevel(AttributeType.Endurance).ToString();
        if (strengthText != null)
            strengthText.text = prop.GetAttributeLevel(AttributeType.Strength).ToString();
        if (agilityText != null)
            agilityText.text = prop.GetAttributeLevel(AttributeType.Agility).ToString();
        if (defenseLevelText != null)
            defenseLevelText.text = prop.GetAttributeLevel(AttributeType.Defense).ToString();
        if (luckText != null)
            luckText.text = prop.GetAttributeLevel(AttributeType.Luck).ToString();
    }
}
