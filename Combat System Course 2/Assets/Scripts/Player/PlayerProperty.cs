using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerProperty : MonoBehaviour

{
    [Header("Energy System")]
    [SerializeField] private int maxEnergy = 100;                    // �������
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
    public int hpValue = 100;
    public float energyValue = 100;
    public int armorValue => baseArmorValue + equipmentArmorBonus; // ֻ�����ԣ��ܻ���ֵ
    public int level = 1;
    public int currEXP = 0;

    // ���뻤��ֵ������ֵ + װ���ӳ�
    [SerializeField] private int baseArmorValue = 0; // ��������ֵ������װ��Ӱ�죩
    private int equipmentArmorBonus = 0; // װ���ӳɻ���ֵ

   
    private HealthSystem healthSystem;
    private Animator anim;
    private ItemSO pendingItem;
    [SerializeField] private GameObject hpPotionModel;
    [SerializeField] private GameObject eyPotionModel;
    public AudioClip DrinkSound;


    public event System.Action OnArmorChanged;//����ֵ�ı��¼������ӡ����٣�

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
        if (healthSystem != null)
        {
            hpValue = Mathf.RoundToInt(healthSystem.Health);
        }

        // ��������Ԥ�ȷ��õĵ��˵������¼�
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

    // ����ֵ��������
    private void OnEnemyDie(EnemyController enemy)
    {
        Debug.Log($"OnEnemyDie�����ã�����ID: {enemy.GetInstanceID()}, ��ǰ֡: {Time.frameCount}");

        int gainedExp = enemy.EXP;
        this.currEXP += gainedExp;

        Debug.Log($"������������� {gainedExp} ����ֵ����ǰ����: {currEXP}");

        bool leveledUp = false;
        int levelsGained = 0;

        // �������ܵĶ༶����
        while (currEXP >= (level * 30) && (level * 30) > 0)
        {
            currEXP -= (level * 30);
            level++;
            leveledUp = true;
            levelsGained++;
            baseArmorValue += 5;
            PlayerHUDUI.Instance.UpdateArmorDisplay();

            Debug.Log($"�����ˣ���ǰ�ȼ�: {level}��ʣ�ྭ��: {currEXP}");
        }

        // ����UI
        if (PlayerHUDUI.Instance != null)
        {
            if (leveledUp)
            {
                // ����ʱ�������⶯��
                PlayerHUDUI.Instance.UpdateEXPBar(true);
            }
            else
            {
                // ֻ�Ǿ�������
                PlayerHUDUI.Instance.UpdateEXPBar(false);
            }
        }

        Debug.Log($"����״̬ - �ȼ�: {level}, ����: {currEXP}/{level * 30}, ���� {levelsGained} ��");
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