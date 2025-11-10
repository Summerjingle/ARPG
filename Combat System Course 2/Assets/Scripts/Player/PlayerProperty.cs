using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerProperty : MonoBehaviour

{
    public static PlayerProperty Instance;
    public Dictionary<PropertyType, List<Property>> propertyDict;
    public int hpValue = 100;
    public int energyValue = 100;
    public int armorValue => baseArmorValue + equipmentArmorBonus; // 只读属性：总护甲值
    public int level = 1;
    public int currEXP = 0;

    // 分离护甲值：基础值 + 装备加成
    [SerializeField] private int baseArmorValue = 0; // 基础护甲值（不受装备影响）
    private int equipmentArmorBonus = 0; // 装备加成护甲值

   
    private HealthSystem healthSystem;


    public event System.Action OnArmorChanged;//护甲值改变事件（增加、减少）

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        propertyDict = new Dictionary<PropertyType, List<Property>>();
        propertyDict.Add(PropertyType.HPValue, new List<Property>());
        propertyDict.Add(PropertyType.EnergyValue, new List<Property>());

       
        healthSystem = GetComponent<HealthSystem>();    
        if (healthSystem != null)
        {
            hpValue = Mathf.RoundToInt(healthSystem.Health);
        }

        // 订阅所有预先放置的敌人的死亡事件
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

    // 处理敌人死亡事件
    private void HandleEnemyDeath(HealthSystem healthSystem)
    {
        EnemyController enemyController = healthSystem.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            OnEnemyDie(enemyController);
        }
    }

    // 经验值处理方法
    private void OnEnemyDie(EnemyController enemy)
    {
        Debug.Log($"OnEnemyDie被调用，敌人ID: {enemy.GetInstanceID()}, 当前帧: {Time.frameCount}");

        int gainedExp = enemy.EXP;
        this.currEXP += gainedExp;

        Debug.Log($"敌人死亡，获得 {gainedExp} 经验值，当前经验: {currEXP}");

        bool leveledUp = false;
        int levelsGained = 0;

        // 处理可能的多级连升
        while (currEXP >= (level * 30) && (level * 30) > 0)
        {
            currEXP -= (level * 30);
            level++;
            leveledUp = true;
            levelsGained++;
            baseArmorValue += 5;
            PlayerHUDUI.Instance.UpdateArmorDisplay();

            Debug.Log($"升级了！当前等级: {level}，剩余经验: {currEXP}");
        }

        // 更新UI
        if (PlayerHUDUI.Instance != null)
        {
            if (leveledUp)
            {
                // 升级时播放特殊动画
                PlayerHUDUI.Instance.UpdateEXPBar(true);
            }
            else
            {
                // 只是经验增加
                PlayerHUDUI.Instance.UpdateEXPBar(false);
            }
        }

        Debug.Log($"最终状态 - 等级: {level}, 经验: {currEXP}/{level * 30}, 连升 {levelsGained} 级");
    }

    public void UseDrag(ItemSO itemSO)
    {
        foreach (Property p in itemSO.propertyList)
        {
            AddProperty(p.propertyType, p.value);
        }
    }

    public void AddProperty(PropertyType pt, int value)
    {
        switch (pt)
        {
            case PropertyType.HPValue:
                hpValue = Mathf.Clamp(hpValue + value, 0, 100);
                if (healthSystem != null)
                {
                    healthSystem.RestoreHealth(value);
                    hpValue = Mathf.RoundToInt(healthSystem.Health);
                }
                return;
            case PropertyType.EnergyValue:
                energyValue += value;
                return;
        }

        if (propertyDict.TryGetValue(pt, out List<Property> list))
        {
            list.Add(new Property(pt, value));
        }
    }

    public void RemoveProperty(PropertyType pt, int value)
    {
        switch (pt)
        {
            case PropertyType.HPValue:
                hpValue -= value;
                return;
            case PropertyType.EnergyValue:
                energyValue -= value;
                return;
        }

        if (propertyDict.TryGetValue(pt, out List<Property> list))
        {
            list.Remove(list.Find(x => x.value == value));
        }
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

    // 获取基础护甲值（用于存档）
    public int GetBaseArmor()
    {
        return baseArmorValue;
    }

    // 设置基础护甲值（用于加载存档）
    public void SetBaseArmor(int value)
    {
        baseArmorValue = value;
        Debug.Log($"设置基础护甲值: {baseArmorValue}, 总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    //增加装备护甲加成（专门用于装备系统）
    public void AddArmorValue(int value)
    {
        equipmentArmorBonus += value;
        Debug.Log($"增加装备护甲加成: {value}, 当前总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    // 移除装备护甲加成（专门用于装备系统）
    public void RemoveArmorValue(int value)
    {
        equipmentArmorBonus -= value;
        Debug.Log($"移除装备护甲加成: {value}, 当前总护甲值: {armorValue}");
        OnArmorChanged?.Invoke();
    }
}