using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerProperty : MonoBehaviour

{
    [Header("Energy System")]
    [SerializeField] private int baseMaxEnergy = 50;                 // 基础最大能量（Inspector设）
    [SerializeField] public float idleRegenRate = 200f;              // 每秒站立时能量恢复
    [SerializeField] public float walkRegenRate = 150f;              // 每秒走路时能量恢复
    [SerializeField] private float sprintCostPerSecond = 15f;        // 冲刺每秒消耗
    [SerializeField] private int rollEnergyCost = 20;                // 每次翻滚消耗

    private int equipmentMaxEnergyBonus;
    public int MaxEnergy => baseMaxEnergy + equipmentMaxEnergyBonus;
    public float EnergyValue => energyValue;
    public float EnergyNormalized => MaxEnergy > 0 ? (float)energyValue / MaxEnergy : 0f;

    public System.Action<float> OnEnergyChanged; // 返回 0~1 的百分比，用于UI

    // 提供获取消耗值的方法，供 PlayerController 调用
    public float GetSprintCostPerSecond() => sprintCostPerSecond;
    public int GetRollEnergyCost() => rollEnergyCost;
    public static PlayerProperty Instance;
    public Dictionary<StatType, List<Property>> propertyDict;
    public float energyValue;
    public int armorValue => baseArmorValue + equipmentArmorBonus;
    public int level = 1;
    public int currEXP = 0;
    public int currSoulAmount = 0;

    [Header("HP System")]
    private int baseMaxHealth;
    private int equipmentMaxHPBonus;

    [Header("Crit System")]//暴击系统
    //暴击率
    [SerializeField] private float baseCritRate = 5f;
    [SerializeField] private float bonusCritRate = 0f;
    public float TotalCritRate => baseCritRate + bonusCritRate;
    //暴击效果
    [SerializeField] private float baseCritDamage = 1.5f;
    [SerializeField] private float bonusCritDamage = 0f;
    public float TotalCritDamage => baseCritDamage + bonusCritDamage;

    [SerializeField] private int baseArmorValue = 0;
    private int equipmentArmorBonus = 0;


    private HealthSystem healthSystem;
    private Animator anim;
    private ItemSO pendingItem;
    [SerializeField] private GameObject hpPotionModel;
    [SerializeField] private GameObject eyPotionModel;
    public AudioClip DrinkSound;


    public event System.Action OnArmorChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        anim=GetComponent<Animator>();

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

        // 记录基础值（baseMaxEnergy 由 Inspector 序列化，无需记录）
        if (healthSystem != null)
            baseMaxHealth = (int)healthSystem.MaxHealth;

        SubscribeToAllEnemies();
    }

    private void SubscribeToAllEnemies()
    {
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();

        foreach (EnemyController enemy in allEnemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= HandleEnemyDeath; // 先取消
                enemyHealth.OnDeath += HandleEnemyDeath; // 再订阅
                Debug.Log($"已订阅敌人: {enemy.gameObject.name}");
            }
        }

        Debug.Log($"总共订阅了 {allEnemies.Length} 个敌人");
    }

    // 敌人死亡事件处理
    private void HandleEnemyDeath(HealthSystem healthSystem)
    {
        EnemyController enemyController = healthSystem.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            OnEnemyDie(enemyController);
        }
    }


    private void OnEnemyDie(EnemyController enemy)//敌人死亡获得SoulAmount
    {
        Debug.Log($"OnEnemyDie触发，死亡敌人ID: {enemy.GetInstanceID()}, 死亡帧：{Time.frameCount}");
        int gainedSoulAmount = enemy.provideSoulAmount;
        currSoulAmount += gainedSoulAmount;
        PlayerHUDUI.Instance.UpdateSoulAmount();
    }

    public void UseDrag(ItemSO itemSO)
    {
        pendingItem = itemSO;                // 记录要使用的药
        anim.Play("UsePotion");            // 播放喝药动画，由 Animator 的 trigger触发
    }

    public void AddProperty(StatType pt, int value)
    {
        switch (pt)
        {
            // === Curr 类：直接加当前值 ===
            case StatType.CurrHP:
                if (healthSystem != null) healthSystem.RestoreHealth(value);
                return;
            case StatType.CurrEnergy:
                energyValue += value;
                energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
                return;

            // === Bonus 类：加 base+bonus ===
            case StatType.MaxHP:
                equipmentMaxHPBonus += value;
                if (healthSystem != null)
                {
                    float newMax = baseMaxHealth + equipmentMaxHPBonus;
                    healthSystem.SetMaxHealth(newMax);
                    healthSystem.RestoreHealth(value); // 当前HP也增加
                }
                break;
            case StatType.MaxEnergy:
                equipmentMaxEnergyBonus += value;
                // 当前能量同比增加
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

        // 记录到字典，方便后续追踪
        if (propertyDict.TryGetValue(pt, out List<Property> list))
        {
            list.Add(new Property(pt, value));
        }
    }

    public void RemoveProperty(StatType pt, int value)
    {
        switch (pt)
        {
            case StatType.CurrHP:
                Debug.LogWarning("CurrHP 不应通过 RemoveProperty 修改，请使用战斗系统造成伤害");
                return;
            case StatType.CurrEnergy:
                energyValue = Mathf.Clamp(energyValue - value, 0, MaxEnergy);
                return;

            case StatType.MaxHP:
                equipmentMaxHPBonus -= value;
                if (healthSystem != null)
                {
                    healthSystem.SetMaxHealth(baseMaxHealth + equipmentMaxHPBonus);
                    healthSystem.RestoreHealth(0); // clamp 当前HP到新上限
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
        {
            list.Remove(list.Find(x => x.value == value));
        }
    }

    /// <summary> 获取某属性的总值（用于装备条件检查）</summary>
    public int GetStatValue(StatType statType)
    {
        return statType switch
        {
            StatType.MaxHP => healthSystem != null ? (int)healthSystem.MaxHealth : baseMaxHealth,
            StatType.MaxEnergy => MaxEnergy,
            StatType.Defense => armorValue,
            StatType.CritRate => (int)TotalCritRate,
            StatType.CritDamage => (int)(TotalCritDamage * 100f), // 如1.5→150
            StatType.Strength => 0,    // TODO: 未实现
            StatType.Luck => 0,        // TODO: 未实现
            StatType.CurrHP => healthSystem != null ? (int)healthSystem.Health : 0,
            StatType.CurrEnergy => (int)energyValue,
            _ => 0,
        };
    }

    private void OnDestroy()
    {
        OnArmorChanged = null;
        EnemyController[] allEnemies = FindObjectsOfType<EnemyController>();
        foreach (EnemyController enemy in allEnemies)
        {
            HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath -= HandleEnemyDeath;
            }
        }
    }

    // 获取基础护甲值，用于存档
    public int GetBaseArmor()
    {
        return baseArmorValue;
    }

    // 设置基础护甲值，用于加载存档
    public void SetBaseArmor(int value)
    {
        baseArmorValue = value;
        Debug.Log($"设置基础护甲值: {baseArmorValue}, 总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    //添加装备护甲加成，专供装备系统调用
    public void AddArmorValue(int value)
    {
        equipmentArmorBonus += value;
        Debug.Log($"增加装备护甲加成: {value}, 当前总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    // 移除装备护甲加成，专供装备系统调用
    public void RemoveArmorValue(int value)
    {
        equipmentArmorBonus -= value;
        Debug.Log($"移除装备护甲加成: {value}, 当前总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }
    // 消耗能量，供外部调用
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

    // 恢复能量，内部调用
    public void RestoreEnergy(float amount)
    {
        energyValue += amount;
        energyValue = Mathf.Clamp(energyValue, 0, MaxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }

    // 强制设置能量值，用于存档读取
    public void SetEnergy(int value)
    {
        energyValue = Mathf.Clamp(value, 0, MaxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }
    // 在动画中显示模型
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

    // 在特定的关键帧调用，用于实际应用效果
    public void OnDrinkApply()
    {
        if (pendingItem == null) return;

        ConsumableSO consumable = pendingItem as ConsumableSO;
        if (consumable == null || consumable.effects == null) return;

        AudioSource.PlayClipAtPoint(DrinkSound, transform.position);
        foreach (Property p in consumable.effects)
        {
            AddProperty(p.statType, p.value);
        }

        pendingItem = null;
    }

    // 隐藏所有药品模型
    public void HideAllDrugModels()
    {

           hpPotionModel.SetActive(false);
           eyPotionModel.SetActive(false);

    }
}
