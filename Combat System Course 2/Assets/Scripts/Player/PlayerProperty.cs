using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可升级的6种属性类型
/// </summary>
public enum AttributeType
{
    Vitality,    // 体力 → 最大生命值
    Endurance,   // 耐力 → 最大精力值
    Strength,    // 力量 → 攻击力
    Agility,     // 敏捷 → 暴击率 + 暴击伤害
    Defense,     // 防御 → 护甲值
    Luck         // 幸运 → 高品质掉落概率
}

public class PlayerProperty : MonoBehaviour
{
    public static PlayerProperty Instance;

    #region ==================== 属性等级 ====================
    [Header("Attribute Levels (1-99)")]
    [SerializeField] private int vitalityLevel = 1;
    [SerializeField] private int enduranceLevel = 1;
    [SerializeField] private int strengthLevel = 1;
    [SerializeField] private int agilityLevel = 1;
    [SerializeField] private int defenseLevel = 1;
    [SerializeField] private int luckLevel = 1;

    public int VitalityLevel => vitalityLevel;
    public int EnduranceLevel => enduranceLevel;
    public int StrengthLevel => strengthLevel;
    public int AgilityLevel => agilityLevel;
    public int DefenseLevel => defenseLevel;
    public int LuckLevel => luckLevel;

    /// <summary> 总等级 = 各属性等级之和 - 5（全1级时总等级=1）</summary>
    public int Level => vitalityLevel + enduranceLevel + strengthLevel
                      + agilityLevel + defenseLevel + luckLevel - 5;
    #endregion

    #region ==================== 灵魂货币 ====================
    public int currSoulAmount = 0;

    /// <summary> 升级消耗公式：cost = 10x³ + 100x² + 500x，x = 当前总等级 </summary>
    public int GetNextUpgradeCost()
    {
        int x = Level;
        return 10 * x * x * x + 100 * x * x + 500 * x;
    }

    /// <summary> 尝试升级某个属性，成功返回 true </summary>
    public bool UpgradeAttribute(AttributeType type)
    {
        int cost = GetNextUpgradeCost();
        if (currSoulAmount < cost) return false;

        currSoulAmount -= cost;

        switch (type)
        {
            case AttributeType.Vitality:  vitalityLevel++;  break;
            case AttributeType.Endurance: enduranceLevel++; break;
            case AttributeType.Strength:  strengthLevel++;  break;
            case AttributeType.Agility:   agilityLevel++;   break;
            case AttributeType.Defense:   defenseLevel++;   break;
            case AttributeType.Luck:      luckLevel++;      break;
        }

        ApplyBaseStatsToSystems();
        PlayerHUDUI.Instance?.UpdateSoulAmount();
        Debug.Log($"[PlayerProperty] {type} → Lv.{GetAttributeLevel(type)}, Total Lv.{Level}, cost: {cost}, remaining souls: {currSoulAmount}");
        return true;
    }

    public int GetAttributeLevel(AttributeType type)
    {
        return type switch
        {
            AttributeType.Vitality  => vitalityLevel,
            AttributeType.Endurance => enduranceLevel,
            AttributeType.Strength  => strengthLevel,
            AttributeType.Agility   => agilityLevel,
            AttributeType.Defense   => defenseLevel,
            AttributeType.Luck      => luckLevel,
            _ => 1,
        };
    }

    /// <summary> 设置属性等级（用于读档），不消耗灵魂 </summary>
    public void SetAttributeLevel(AttributeType type, int level)
    {
        level = Mathf.Clamp(level, 1, 99);
        switch (type)
        {
            case AttributeType.Vitality:  vitalityLevel  = level; lastAppliedVitalityLevel = level; break;
            case AttributeType.Endurance: enduranceLevel = level; break;
            case AttributeType.Strength:  strengthLevel  = level; break;
            case AttributeType.Agility:   agilityLevel   = level; break;
            case AttributeType.Defense:   defenseLevel   = level; break;
            case AttributeType.Luck:      luckLevel      = level; break;
        }
    }
    #endregion

    #region ==================== 属性计算公式 ====================
    // 公式：y = base + A * (1 - e^(-k * (x - 1)))

    /// <summary>体力 → 最大生命值：100 + 1900 * (1 - e^(-0.05*(x-1)))</summary>
    public int GetMaxHPFromVitality()
    {
        return Mathf.RoundToInt(100f + 1900f * (1f - Mathf.Exp(-0.05f * (vitalityLevel - 1))));
    }

    /// <summary>耐力 → 最大精力值：50 + 150 * (1 - e^(-0.05*(x-1)))</summary>
    public int GetMaxEnergyFromEndurance()
    {
        return Mathf.RoundToInt(50f + 150f * (1f - Mathf.Exp(-0.05f * (enduranceLevel - 1))));
    }

    /// <summary>力量 → 攻击力：10 + 490 * (1 - e^(-0.05*(x-1)))</summary>
    public int GetAttackPowerFromStrength()
    {
        return Mathf.RoundToInt(10f + 490f * (1f - Mathf.Exp(-0.05f * (strengthLevel - 1))));
    }

    /// <summary>敏捷 → 暴击率（分段函数，20级分界），返回值单位%</summary>
    public float GetCritRateFromAgility()
    {
        if (agilityLevel <= 20)
        {
            // k=0.08，快速增长段
            return 5f + 25f * (1f - Mathf.Exp(-0.08f * (agilityLevel - 1)));
        }
        else
        {
            // k=0.02，缓慢增长段
            float critAt20 = 5f + 25f * (1f - Mathf.Exp(-0.08f * 19));
            return critAt20 + 25f * (1f - Mathf.Exp(-0.02f * (agilityLevel - 20)));
        }
    }

    /// <summary>敏捷 → 暴击伤害（分段函数，20级分界），返回值单位%</summary>
    public float GetCritDamageFromAgility()
    {
        if (agilityLevel <= 20)
        {
            // k=0.02，缓慢增长段
            return 150f + 50f * (1f - Mathf.Exp(-0.02f * (agilityLevel - 1)));
        }
        else
        {
            // k=0.08，快速增长段
            float dmgAt20 = 150f + 50f * (1f - Mathf.Exp(-0.02f * 19));
            return dmgAt20 + 50f * (1f - Mathf.Exp(-0.08f * (agilityLevel - 20)));
        }
    }

    /// <summary>防御 → 护甲值：5 + 295 * (1 - e^(-0.05*(x-1)))</summary>
    public int GetDefenseFromDefenseLevel()
    {
        return Mathf.RoundToInt(5f + 295f * (1f - Mathf.Exp(-0.05f * (defenseLevel - 1))));
    }

    /// <summary>幸运 → 掉率加成：0 + 50 * (1 - e^(-0.05*(x-1)))</summary>
    public int GetLuckFromLuckLevel()
    {
        return Mathf.RoundToInt(0f + 50f * (1f - Mathf.Exp(-0.05f * (luckLevel - 1))));
    }
    #endregion

    #region ==================== 派生属性（base + equipment bonus）====================

    // --- HP ---
    public int MaxHealth => GetMaxHPFromVitality() + equipmentMaxHPBonus;

    // --- Energy ---
    public int MaxEnergy => GetMaxEnergyFromEndurance() + equipmentMaxEnergyBonus;
    public float EnergyValue => energyValue;
    public float EnergyNormalized => MaxEnergy > 0 ? (float)energyValue / MaxEnergy : 0f;

    // --- Attack ---
    public int AttackPower => GetAttackPowerFromStrength();

    // --- Crit ---
    /// <summary>总暴击率（%），base 来自敏捷 + equipment bonus</summary>
    public float TotalCritRate => GetCritRateFromAgility() + bonusCritRate;
    /// <summary>总暴击伤害（倍率），base 来自敏捷 + equipment bonus</summary>
    public float TotalCritDamage => (GetCritDamageFromAgility() / 100f) + bonusCritDamage;

    // --- Armor ---
    public int ArmorValue => GetDefenseFromDefenseLevel() + equipmentArmorBonus;

    // --- Luck ---
    public int LuckValue => GetLuckFromLuckLevel();

    #endregion

    #region ==================== Energy 行为参数 ====================
    [Header("Energy System")]
    [SerializeField] public float idleRegenRate = 200f;
    [SerializeField] public float walkRegenRate = 150f;
    [SerializeField] private float sprintCostPerSecond = 15f;
    [SerializeField] private int rollEnergyCost = 20;

    public float energyValue;
    public float GetSprintCostPerSecond() => sprintCostPerSecond;
    public int GetRollEnergyCost() => rollEnergyCost;
    #endregion

    #region ==================== Equipment Bonuses（装备加成，叠加在公式值之上）====================
    private int equipmentMaxHPBonus;
    private int equipmentMaxEnergyBonus;
    private int equipmentArmorBonus;
    private float bonusCritRate;
    private float bonusCritDamage;
    #endregion

    #region ==================== 事件 ====================
    public System.Action<float> OnEnergyChanged;
    public System.Action OnArmorChanged;
    #endregion

    #region ==================== 组件引用 & 内部状态 ====================
    public Dictionary<StatType, List<Property>> propertyDict;
    private HealthSystem healthSystem;
    private Animator anim;
    private ItemSO pendingItem;
    private int lastAppliedVitalityLevel;  // 上次同步到HealthSystem的体力等级

    [Header("Potion Models")]
    [SerializeField] private GameObject hpPotionModel;
    [SerializeField] private GameObject eyPotionModel;
    public AudioClip DrinkSound;

    [Header("Low HP UI")]
    [SerializeField] private GameObject activeLowHPUI;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        propertyDict = new Dictionary<StatType, List<Property>>();
        propertyDict.Add(StatType.MaxHP, new List<Property>());
        propertyDict.Add(StatType.MaxEnergy, new List<Property>());
        propertyDict.Add(StatType.Defense, new List<Property>());
        propertyDict.Add(StatType.CritRate, new List<Property>());
        propertyDict.Add(StatType.CritDamage, new List<Property>());

        HideAllDrugModels();
        healthSystem = GetComponent<HealthSystem>();
        lastAppliedVitalityLevel = vitalityLevel;
        // MaxHealth 由 Inspector 上的 HealthSystem 决定，PlayerProperty 只提供体力加成

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += OnPlayerHealthChanged;
            UpdateLowHPUI(); // 初始同步
        }

        SubscribeToAllEnemies();
    }

    /// <summary> 属性升级后同步到 HealthSystem / UI </summary>
    private void ApplyBaseStatsToSystems()
    {
        // 体力等级变化 → delta 方式增减 MaxHealth，不覆盖 Inspector 基础值
        if (healthSystem != null && vitalityLevel != lastAppliedVitalityLevel)
        {
            int oldHP = GetMaxHPForLevel(lastAppliedVitalityLevel);
            int newHP = GetMaxHPForLevel(vitalityLevel);
            healthSystem.AddMaxHealth(newHP - oldHP);
            lastAppliedVitalityLevel = vitalityLevel;
        }

        // Clamp 当前能量到新上限
        energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);

        OnArmorChanged?.Invoke();
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }

    private int GetMaxHPForLevel(int level)
    {
        return Mathf.RoundToInt(100f + 1900f * (1f - Mathf.Exp(-0.05f * (level - 1))));
    }

    #region ==================== Low HP UI ====================
    private void OnPlayerHealthChanged(HealthSystem hs, HealthChangeInfo info)
    {
        UpdateLowHPUI();
    }

    private void UpdateLowHPUI()
    {
        if (activeLowHPUI == null || healthSystem == null) return;
        activeLowHPUI.SetActive(healthSystem.HealthPercent < 0.2f && !healthSystem.IsDead);
    }
    #endregion

    #region ==================== 敌人击杀 → 获得灵魂 ====================
    private void SubscribeToAllEnemies()
    {
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in allEnemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= HandleEnemyDeath;
                enemyHealth.OnDeath += HandleEnemyDeath;
                Debug.Log($"已订阅敌人: {enemy.gameObject.name}");
            }
        }
        Debug.Log($"总共订阅了 {allEnemies.Length} 个敌人");
    }

    private void HandleEnemyDeath(HealthSystem healthSystem)
    {
        EnemyController enemyController = healthSystem.GetComponent<EnemyController>();
        if (enemyController != null)
            OnEnemyDie(enemyController);
    }

    private void OnEnemyDie(EnemyController enemy)
    {
        Debug.Log($"OnEnemyDie触发，死亡敌人ID: {enemy.GetInstanceID()}, 死亡帧：{Time.frameCount}");
        int gainedSoulAmount = enemy.provideSoulAmount;
        currSoulAmount += gainedSoulAmount;
        PlayerHUDUI.Instance?.UpdateSoulAmount();
    }
    #endregion

    #region ==================== 装备加成（AddProperty / RemoveProperty）====================
    public void AddProperty(StatType pt, int value)
    {
        switch (pt)
        {
            case StatType.CurrHP:
                if (healthSystem != null) healthSystem.RestoreHealth(value);
                return;
            case StatType.CurrEnergy:
                energyValue += value;
                energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
                return;

            case StatType.MaxHP:
                equipmentMaxHPBonus += value;
                if (healthSystem != null)
                {
                    healthSystem.AddMaxHealth(value);
                    healthSystem.RestoreHealth(value);
                }
                break;
            case StatType.MaxEnergy:
                equipmentMaxEnergyBonus += value;
                energyValue = Mathf.Clamp(energyValue + value, 0, MaxEnergy);
                OnEnergyChanged?.Invoke(EnergyNormalized);
                break;
            case StatType.Defense:
                equipmentArmorBonus += value;
                OnArmorChanged?.Invoke();
                break;
            case StatType.CritRate:
                bonusCritRate += value;
                break;
            case StatType.CritDamage:
                bonusCritDamage += value;
                break;
        }

        if (propertyDict.TryGetValue(pt, out List<Property> list))
            list.Add(new Property(pt, value));
    }

    public void RemoveProperty(StatType pt, int value)
    {
        switch (pt)
        {
            case StatType.CurrHP:
                Debug.LogWarning("CurrHP 不应通过 RemoveProperty 修改");
                return;
            case StatType.CurrEnergy:
                energyValue = Mathf.Clamp(energyValue - value, 0, MaxEnergy);
                return;

            case StatType.MaxHP:
                equipmentMaxHPBonus -= value;
                if (healthSystem != null)
                {
                    healthSystem.AddMaxHealth(-value);
                    healthSystem.RestoreHealth(0);
                }
                break;
            case StatType.MaxEnergy:
                equipmentMaxEnergyBonus -= value;
                energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
                OnEnergyChanged?.Invoke(EnergyNormalized);
                break;
            case StatType.Defense:
                equipmentArmorBonus -= value;
                OnArmorChanged?.Invoke();
                break;
            case StatType.CritRate:
                bonusCritRate -= value;
                break;
            case StatType.CritDamage:
                bonusCritDamage -= value;
                break;
        }

        if (propertyDict.TryGetValue(pt, out List<Property> list))
            list.Remove(list.Find(x => x.value == value));
    }

    public int GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHP      => healthSystem != null ? (int)healthSystem.MaxHealth : 0,
            StatType.MaxEnergy  => MaxEnergy,
            StatType.Defense    => ArmorValue,
            StatType.CritRate   => (int)TotalCritRate,
            StatType.CritDamage => (int)(TotalCritDamage * 100f),
            StatType.Strength   => AttackPower,
            StatType.Luck       => LuckValue,
            StatType.CurrHP     => healthSystem != null ? (int)healthSystem.Health : 0,
            StatType.CurrEnergy => (int)energyValue,
            _ => 0,
        };
    }
    #endregion

    #region ==================== 护甲（兼容旧接口）====================
    public int GetBaseArmor() => GetDefenseFromDefenseLevel();

    public void SetBaseArmor(int value)
    {
        // 新系统下护甲由防御等级公式决定，此接口保留兼容
        Debug.LogWarning("SetBaseArmor 在新属性系统中被调用，护甲值应由 defenseLevel 公式决定");
        OnArmorChanged?.Invoke();
    }

    public void AddArmorValue(int value)
    {
        equipmentArmorBonus += value;
        Debug.Log($"增加装备护甲加成: {value}, 当前总护甲值: {ArmorValue}");
        OnArmorChanged?.Invoke();
    }

    public void RemoveArmorValue(int value)
    {
        equipmentArmorBonus -= value;
        Debug.Log($"移除装备护甲加成: {value}, 当前总护甲值: {ArmorValue}");
        OnArmorChanged?.Invoke();
    }
    #endregion

    #region ==================== 能量操作 ====================
    public bool ConsumeEnergy(float amount)
    {
        if (energyValue >= amount)
        {
            energyValue -= amount;
            energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
            OnEnergyChanged?.Invoke(EnergyNormalized);
            return true;
        }
        return false;
    }

    public void RestoreEnergy(float amount)
    {
        energyValue += amount;
        energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }

    public void SetEnergy(int value)
    {
        energyValue = Mathf.Clamp(value, 0, MaxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }
    #endregion

    #region ==================== 药品使用 ====================
    public void UseDrag(ItemSO itemSO)
    {
        pendingItem = itemSO;
        anim.Play("UsePotion");
    }

    public void OnDrinkShowModel()
    {
        HideAllDrugModels();
        if (pendingItem == null) return;

        ConsumableSO consumable = pendingItem as ConsumableSO;
        if (consumable == null || consumable.effects == null) return;

        foreach (Property p in consumable.effects)
        {
            if (p.statType == StatType.CurrHP)
                hpPotionModel.SetActive(true);
            else if (p.statType == StatType.CurrEnergy)
                eyPotionModel.SetActive(true);
        }
    }

    public void OnDrinkApply()
    {
        if (pendingItem == null) return;

        ConsumableSO consumable = pendingItem as ConsumableSO;
        if (consumable == null || consumable.effects == null) return;

        AudioManager.Instance.PlaySFX(DrinkSound, transform.position);
        foreach (Property p in consumable.effects)
            AddProperty(p.statType, p.value);

        pendingItem = null;
    }

    public void HideAllDrugModels()
    {
        hpPotionModel.SetActive(false);
        eyPotionModel.SetActive(false);
    }
    #endregion

    private void OnDestroy()
    {
        if (healthSystem != null)
            healthSystem.OnHealthChanged -= OnPlayerHealthChanged;

        OnArmorChanged = null;
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in allEnemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
                enemyHealth.OnDeath -= HandleEnemyDeath;
        }
    }
}
