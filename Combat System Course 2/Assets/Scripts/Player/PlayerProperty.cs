using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerProperty : MonoBehaviour

{
    [Header("Energy System")]
    [SerializeField] private int maxEnergy ;                   // �������
    [SerializeField] public float idleRegenRate = 200f;              // ÿ��ָ���վ��������
    [SerializeField] public float walkRegenRate = 150f;              // ÿ��ָ�����·��
    [SerializeField] private float sprintCostPerSecond = 15f;        // ���ÿ������
    [SerializeField] private int rollEnergyCost = 20;                // ÿ�η�������

    public float EnergyValue => energyValue;                           // ֻ�����Ⱪ¶
    public int MaxEnergy => maxEnergy;
    public float EnergyNormalized => (float)energyValue / maxEnergy;

    public System.Action<float> OnEnergyChanged; // ���� 0~1 �İٷֱȣ�����UI

    // ��������ȡ����ֵ�ķ������� PlayerController ����
    public float GetSprintCostPerSecond() => sprintCostPerSecond;
    public int GetRollEnergyCost() => rollEnergyCost;
    public static PlayerProperty Instance;
    public Dictionary<PropertyType, List<Property>> propertyDict;
    public float energyValue ;
    public int armorValue => baseArmorValue + equipmentArmorBonus; 
    public int level = 1;
    public int currEXP = 0;
    public int currSoulAmount=0;

    
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
        propertyDict = new Dictionary<PropertyType, List<Property>>();
        propertyDict.Add(PropertyType.HPValue, new List<Property>());
        propertyDict.Add(PropertyType.EnergyValue, new List<Property>());
        HideAllDrugModels();
        healthSystem = GetComponent<HealthSystem>();    
        
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
                enemyHealth.OnDeath -= HandleEnemyDeath; // ��ȡ��
                enemyHealth.OnDeath += HandleEnemyDeath; // �ٶ���
                Debug.Log($"�Ѷ��ĵ���: {enemy.gameObject.name}");
            }
        }

        Debug.Log($"�ܹ������� {allEnemies.Length} ������");
    }

    // �������������¼�
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
        pendingItem = itemSO;                // ��¼���Ҫ�õ�ҩ
        anim.Play("UsePotion");            // ����ҩ�������� Animator ��� trigger��
    }

    public void AddProperty(PropertyType pt, int value)
    {
        switch (pt)
        {
            case PropertyType.HPValue:
                if (healthSystem != null)
                {
                    healthSystem.RestoreHealth(value);
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
                 Debug.LogWarning("HPValue 不应通过 RemoveProperty 修改，请使用战斗系统造成伤害");
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

    // ��ȡ��������ֵ�����ڴ浵��
    public int GetBaseArmor()
    {
        return baseArmorValue;
    }

    // ���û�������ֵ�����ڼ��ش浵��
    public void SetBaseArmor(int value)
    {
        baseArmorValue = value;
        Debug.Log($"���û�������ֵ: {baseArmorValue}, �ܻ���ֵ: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    //����װ�����׼ӳɣ�ר������װ��ϵͳ��
    public void AddArmorValue(int value)
    {
        equipmentArmorBonus += value;
        Debug.Log($"����װ�����׼ӳ�: {value}, ��ǰ�ܻ���ֵ: {armorValue}");
        OnArmorChanged?.Invoke();
    }

    // �Ƴ�װ�����׼ӳɣ�ר������װ��ϵͳ��
    public void RemoveArmorValue(int value)
    {
        equipmentArmorBonus -= value;
        Debug.Log($"�Ƴ�װ�����׼ӳ�: {value}, ��ǰ�ܻ���ֵ: {armorValue}");
        OnArmorChanged?.Invoke();
    }
    // �����������ɱ��ⲿ���ã�
    public bool ConsumeEnergy(float amount)
    {
        if (energyValue >= amount)
        {
            energyValue -= amount;
            energyValue = Mathf.Clamp(energyValue, 0, maxEnergy);
            OnEnergyChanged?.Invoke(EnergyNormalized);
            return true;
        }
        return false;
    }

    // �ָ��������ڲ��ã�
    public void RestoreEnergy(float amount)
    {
        energyValue += amount;
        energyValue = Mathf.Clamp(energyValue, 0, maxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }

    // ǿ���������������������
    public void SetEnergy(int value)
    {
        energyValue = Mathf.Clamp(value, 0, maxEnergy);
        OnEnergyChanged?.Invoke(EnergyNormalized);
    }
    // ����;����ʾģ��
    public void OnDrinkShowModel()
    {
        HideAllDrugModels();

        if (pendingItem == null) return;

        foreach (Property p in pendingItem.propertyList)
        {
            if (p.propertyType == PropertyType.HPValue)
                hpPotionModel.SetActive(true);

            else if (p.propertyType == PropertyType.EnergyValue)
                eyPotionModel.SetActive(true);
        }
    }

    // �����еĹؼ�֡���ã�����������
    public void OnDrinkApply()
    {
        if (pendingItem == null) return;
        AudioSource.PlayClipAtPoint(DrinkSound,transform.position);
        foreach (Property p in pendingItem.propertyList)
        {
            AddProperty(p.propertyType, p.value);
        }

        pendingItem = null;
    }

    // ������������ģ��
    public void HideAllDrugModels()
    {
        
           hpPotionModel.SetActive(false);
           eyPotionModel.SetActive(false);

    }
}