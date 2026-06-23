using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static bool shouldLoadFromSave = false;
    public static bool isNewGame = true;
    public static string currentSaveId;
    public static bool shouldLoadPosition = false;

    public event System.Action OnAutoSaveStart;
    public event System.Action OnAutoSaveComplete;

    private string savePath;
    private string savesDirectory;
    private const int MAX_SAVE_SLOTS = 10;

    [Header("Auto Save")]
    [SerializeField] private float autoSaveInterval = 300f; // 5分钟自动存档间隔
    private float autoSaveTimer = 0f;
    private float lastAutoSaveTime = -999f;
    [SerializeField] private float autoSaveCooldown = 120f; // 最小间隔2分钟

    private GameObject registeredPlayer;
    public GameSaveData currentSaveData;
    private bool isApplyingSaveData = false;
    
    private bool isApplySaveDataAfterFrameRunning = false;
      private bool hasSaveDataBeenAppliedInCurrentScene = false;

    private void Awake()
    {
        Debug.Log($"SaveManager Awake �����ã���ǰʵ��: {GetInstanceID()}");

        // ����Ƿ�������ʵ��
        var allInstances = FindObjectsOfType<SaveManager>();
        Debug.Log($"��ǰ������ SaveManager ʵ������: {allInstances.Length}");

        foreach (var instance in allInstances)
        {
            Debug.Log($"ʵ��ID: {instance.GetInstanceID()}, ��Ϸ����: {instance.gameObject.name}");
        }
        if (Instance != null && Instance != this)
        {
            // ��ȡ���¼�ע��������
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savesDirectory = Path.Combine(Application.persistentDataPath, "saves");
        if (!Directory.Exists(savesDirectory))
        {
            Directory.CreateDirectory(savesDirectory);
        }

        // ȷ��ֻע��һ��
        SceneManager.sceneLoaded -= OnSceneLoaded; // ���Ƴ����ܴ��ڵ��ظ�ע��
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // ȷ������ȡ��ע��
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region �浵����

    public void ResetSaveManagerState()
    {
        shouldLoadFromSave = false;
        isNewGame = true;
        currentSaveId = null;
        shouldLoadPosition = false;

        currentSaveData = null;
        registeredPlayer = null;
        isApplyingSaveData = false;
        isApplySaveDataAfterFrameRunning = false;
        hasSaveDataBeenAppliedInCurrentScene = false;

        // ���ϵͳ
        InventoryManager.Instance?.ClearInventory();
        QuestManager.Instance?.ResetAllQuests();
        WeaponEquipmentManager.Instance?.UnequipWeapon();
        ArmorEquipmentManager.Instance?.UnequipAll();
        CurrencySystem.Instance?.SetCurrentCoins(0);

        Debug.Log("SaveManager ״̬�����е�������������");
    }
    public void CreateNewGame(int slot)
    {
        ResetSaveManagerState();
        isNewGame = true;
        shouldLoadFromSave = false;
        shouldLoadPosition = false; // ����Ϸ������λ��

        // ���ҿ��õĿղ�λ�����������д浵��
        int availableSlot = FindEmptySaveSlot();

        // ����ȫ�µĴ浵�����������д浵��
        currentSaveData = new GameSaveData(availableSlot);
        currentSaveId = currentSaveData.saveId;

        Debug.Log($"��������Ϸ��ʹ�ÿղ�λ: {availableSlot}, �浵ID: {currentSaveId}");
    }

    // �������������ҿղ�λ
    private int FindEmptySaveSlot()
    {
        var existingSaves = GetAllSaves();

        // ���ҵ�һ���ղ�λ��0-9��
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            if (!existingSaves.Any(save => save.saveSlot == i))
            {
                return i; // �ҵ��ղ�λ
            }
        }

        // ���û�пղ�λ��ʹ���µĲ�λ��ţ����������д浵��
        return existingSaves.Count;
    }

    public void LoadGame(string saveId)
    {
        isNewGame = false;
        shouldLoadFromSave = true;
        shouldLoadPosition = true; // �����˵����ش浵ʱ��Ҫ����λ��
        currentSaveId = saveId;
        LoadGameData(saveId);
        Debug.Log($"׼�����ش浵: {saveId}");
    }

    public void DeleteSave(string saveId)
    {
        string savePath = GetSavePath(saveId);
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"��ɾ���浵: {saveId}");
        }

        // ���ɾ�����ǵ�ǰ�浵������״̬
        if (currentSaveId == saveId)
        {
            currentSaveId = null;
            currentSaveData = null;
        }
    }

    public List<GameSaveData> GetAllSaves()
    {
        List<GameSaveData> saves = new List<GameSaveData>();

        if (!Directory.Exists(savesDirectory))
            return saves;

        string[] saveFiles = Directory.GetFiles(savesDirectory, "*.json");
        foreach (string filePath in saveFiles)
        {
            try
            {
                string jsonData = File.ReadAllText(filePath);
                GameSaveData saveData = JsonConvert.DeserializeObject<GameSaveData>(jsonData);
                if (saveData != null)
                {
                    saves.Add(saveData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"���ش浵�ļ�ʧ�� {filePath}: {e.Message}");
            }
        }

        // ������ʱ���������µ���ǰ
        return saves.OrderByDescending(s => s.saveTime).ToList();
    }

    public GameSaveData GetSaveData(string saveId)
    {
        string savePath = GetSavePath(saveId);
        if (File.Exists(savePath))
        {
            try
            {
                string jsonData = File.ReadAllText(savePath);
                return JsonConvert.DeserializeObject<GameSaveData>(jsonData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"���ش浵ʧ��: {e.Message}");
            }
        }
        return null;
    }

    private void LoadGameData(string saveId)
    {
        currentSaveData = GetSaveData(saveId);
        if (currentSaveData == null)
        {
            Debug.LogWarning($"�浵������: {saveId}");
            currentSaveData = new GameSaveData();
        }
        else
        {
            Debug.Log("��Ϸ�����Ѽ���");
        }
    }

    private string GetSavePath(string saveId)
    {
        return Path.Combine(savesDirectory, $"{saveId}.json");
    }
    #endregion

    #region 保存游戏
    public void SaveGame(bool updatePosition = true)
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("No current save data, cannot save");
            return;
        }

        OnAutoSaveStart?.Invoke();

        // Check if new game and hasn't been saved yet
        if (isNewGame && !HasBeenSavedBefore())
        {
            int availableSlot = FindEmptySaveSlot();
            currentSaveData.saveSlot = availableSlot;
            currentSaveData.saveName = $"Save {availableSlot + 1}";
        }

        GameObject player = registeredPlayer ?? GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found, cannot save");
            return;
        }

        PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        ArmorEquipmentManager equipmentManager = player.GetComponent<ArmorEquipmentManager>();

        if (playerProperty == null || healthSystem == null)
        {
            Debug.LogWarning("Player missing required components, cannot save");
            return;
        }

        if (healthSystem.IsDead)
        {
            Debug.LogWarning("Player is dead, cannot save");
            return;
        }

        // Only update position on manual/checkpoint saves, not auto-save
        if (updatePosition)
        {
            currentSaveData.currentScene = SceneManager.GetActiveScene().name;
            currentSaveData.playerPosition = player.transform.position;
            currentSaveData.playerRotation = player.transform.rotation;
        }

        currentSaveData.level = playerProperty.level;//等级
        currentSaveData.currEXP = playerProperty.currEXP;//经验值（即将弃用）
        currentSaveData.currSoulAmount=playerProperty.currSoulAmount;//灵魂值
        currentSaveData.hpValue = Mathf.RoundToInt(healthSystem.Health);//当前血量
        currentSaveData.maxHealth = Mathf.RoundToInt(healthSystem.MaxHealth);//最大血量
        currentSaveData.energyValue = playerProperty.energyValue;//当前精力
        currentSaveData.armorValue = playerProperty.GetBaseArmor();//当前护甲
        currentSaveData.currCoins=CurrencySystem.Instance?.GetCurrentCoins() ?? 0;//当前钱币
        currentSaveData.saveTime = System.DateTime.Now;//保存事件

        currentSaveData.inventoryItems.Clear();
        currentSaveData.questProgress.Clear();

        currentSaveData.isWeaponDrawn = WeaponEquipmentManager.Instance?.isWeaponDrawn ?? false;

        SaveEquipment(equipmentManager);
        SaveInventory();
        SaveQuickSlots();
        SaveQuests();

        try
        {
            string savePath = GetSavePath(currentSaveData.saveId);
            string jsonData = JsonConvert.SerializeObject(currentSaveData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(savePath, jsonData);
            isNewGame = false;
            Debug.Log($"Game saved to slot: {currentSaveData.saveSlot}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }

        OnAutoSaveComplete?.Invoke();
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        if (currentSaveData == null) return;

        currentSaveData.currentScene = SceneManager.GetActiveScene().name;
        currentSaveData.playerPosition = position;
        currentSaveData.playerRotation = rotation;
    }

    private bool HasBeenSavedBefore()
    {
        string savePath = GetSavePath(currentSaveData.saveId);
        return File.Exists(savePath);
    }

    private void SaveEquipment(ArmorEquipmentManager equipmentManager)
    {
        if (equipmentManager != null)
        {
            currentSaveData.equippedHelmet = GetEquippedItemName(equipmentManager, ArmorType.Helmet);
            currentSaveData.equippedChestplate = GetEquippedItemName(equipmentManager, ArmorType.Chestplate);
            currentSaveData.equippedGauntlets = GetEquippedItemName(equipmentManager, ArmorType.Gauntlets);
            currentSaveData.equippedLeggings = GetEquippedItemName(equipmentManager, ArmorType.Leggings);
            currentSaveData.equippedBoots = GetEquippedItemName(equipmentManager, ArmorType.Boots);
        }
        var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
        if (currentWeapon != null)  
        {
            PickableObject weaponPickable = currentWeapon.GetComponent<PickableObject>();
            if (weaponPickable != null && weaponPickable.itemSO != null)
            {
                currentSaveData.equippedWeapon = weaponPickable.itemSO.name;
            }
        }
    }

    private string GetEquippedItemName(ArmorEquipmentManager equipmentManager, ArmorType armorType)
    {
        if (equipmentManager == null) return "";
        ItemSO equippedItem = equipmentManager.GetEquippedItem(armorType);
        return equippedItem != null ? equippedItem.name : "";
    }

    private void SaveInventory()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("��������δ��ʼ�����޷�������");
            return;
        }

        currentSaveData.inventoryItems.Clear();

       
        var itemStacks = InventoryManager.Instance.GetAllItemStacks();
        currentSaveData.inventoryItems.AddRange(itemStacks);

        Debug.Log($"�ѱ��� {currentSaveData.inventoryItems.Count} ����Ʒ�ѵ�");
    }

    private void SaveQuickSlots()
    {
        currentSaveData.quickSlots.Clear();

        if (QuickItemBar.Instance == null)
        {
            Debug.LogWarning("QuickItemBar δ��ʼ�����޷�����");
            return;
        }

        for (int i = 0; i < 7; i++)
        {
            var slot = QuickItemBar.Instance.GetSlot(i);
            string itemName = slot.item != null ? slot.item.nameOfItem ?? slot.item.name : "";
            currentSaveData.quickSlots.Add(new QuickSlotSaveData(itemName, slot.count));
        }
    }

    private void SaveQuests()
    {
        if (QuestManager.Instance == null || QuestDBManager.Instance == null)
        {
            Debug.LogWarning("���������δ��ʼ�����޷���������");
            return;
        }

        currentSaveData.questProgress = new List<QuestSaveData>();

        // �������������״̬�ͽ���
        foreach (Quest quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            if (quest != null)
            {
                QuestState state = QuestManager.Instance.GetQuestState(quest);
                QuestSaveData questSaveData = new QuestSaveData(quest.questID, state);
                Debug.Log($"��������: {quest.questName}, ״̬: {state}");
                // ��������Ŀ�����
                if (quest.objectives != null)
                {
                    for (int i = 0; i < quest.objectives.Count; i++)
                    {
                        var objective = quest.objectives[i];
                        questSaveData.objectiveProgress.Add(new ObjectiveProgress
                        {
                            objectiveIndex = i,
                            currentAmount = objective.currentAmount,
                            isCompleted = objective.isCompleted
                        });
                        Debug.Log($"����Ŀ��{i}: ����={objective.objectiveType}, Ŀ��ID={objective.targetID}, ���={objective.isCompleted}");
                    }
                }

                currentSaveData.questProgress.Add(questSaveData);
            }
        }

        Debug.Log($"�ѱ��� {currentSaveData.questProgress.Count} ���������");
    }
    #endregion

    #region ���ش浵
    public void ApplySaveData()
    {
        Debug.Log($"ApplySaveData �����ã����ö�ջ: {System.Environment.StackTrace}");
        // ��ֹ�ظ�Ӧ��
        if (hasSaveDataBeenAppliedInCurrentScene)
        {
            Debug.LogWarning("�浵�����Ѿ��ڵ�ǰ����Ӧ�ù�������");
            return;
        }

        // ���ñ�־
        hasSaveDataBeenAppliedInCurrentScene = true;
        if (isApplyingSaveData) return;
        StartCoroutine(ApplySaveDataWithRetry());
    }

    private IEnumerator ApplySaveDataWithRetry()
    {
        if (isNewGame)
        {
            Debug.Log("����Ϸ��ʼ��ʹ�ó���Ĭ������");
            isApplyingSaveData = false;
            yield break;
        }

        if (currentSaveData == null)
        {
            Debug.LogWarning("û�п�Ӧ�õĴ浵����");
            isApplyingSaveData = false;
            yield break;
        }
        isApplyingSaveData = true;

        int maxRetries = 15;
        float retryInterval = 0.2f;

        for (int i = 0; i < maxRetries; i++)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
                HealthSystem healthSystem = player.GetComponent<HealthSystem>();

                if (playerProperty != null && healthSystem != null)
                {
                    ApplySaveDataToPlayer(player);
                    Debug.Log("�浵������Ӧ��");
                    isApplyingSaveData = false;
                    yield break;
                }
                else
                {
                    Debug.Log($"�ҵ���Ҷ��󣬵����δ��ȫ��ʼ�������Ե� {i + 1} ��");
                }
            }

            Debug.Log($"�� {i + 1} �γ���Ѱ����Ҷ���...");
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogError($"�� {maxRetries} �����Ժ���Ȼ�Ҳ�����Ҷ����޷�Ӧ�ô浵����");
        isApplyingSaveData = false;
    }

    private void ApplySaveDataToPlayer(GameObject player)
    {
        // ֻ������Ҫʱ��Ӧ��λ������
        if (shouldLoadPosition)
        {
            ApplyPlayerPosition(player);
        }

        ApplyPlayerProperties(player);
        ApplyInventory();       // 内部会 UpdateInventoryUI，此时 QuickSlots 还没恢复
        ApplyQuickSlots();
        InventoryUI.Instance?.RefreshAllQuickLights(); // 补刷新：QuickSlots 恢复后才设 QuickLight
        ApplyQuests();
        ApplyEquipment();

        Debug.Log($"Applied save - Level: {currentSaveData.level}, EXP: {currentSaveData.currEXP}, HP: {currentSaveData.hpValue}");
        RefreshHUDUI();
    }

    // ����������Ӧ�����λ��
    private void ApplyPlayerPosition(GameObject player)
    {
        if (currentSaveData.playerPosition.ToVector3() != Vector3.zero)
        {
            player.transform.position = currentSaveData.playerPosition.ToVector3();
            player.transform.rotation = currentSaveData.playerRotation.ToQuaternion();
            Debug.Log($"Ӧ�����λ��: {player.transform.position}, ��ת: {player.transform.rotation}");
        }
        else
        {
            Debug.Log("�浵��û��λ�����ݣ�ʹ��Ĭ�ϳ�����");
        }
    }

    private void ApplyPlayerProperties(GameObject player)
    {
        PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();

        if (playerProperty == null || healthSystem == null)
        {
            Debug.LogWarning("���ȱ�ٱ�Ҫ���������");
            return;
        }

        Debug.Log($"Ӧ��ǰ���� - �ȼ�: {playerProperty.level} -> {currentSaveData.level}, ����: {playerProperty.currEXP} -> {currentSaveData.currEXP}");

        playerProperty.level = currentSaveData.level;
        playerProperty.currEXP = currentSaveData.currEXP;
        playerProperty.currSoulAmount=currentSaveData.currSoulAmount;
        playerProperty.energyValue = currentSaveData.energyValue;
        playerProperty.SetBaseArmor(currentSaveData.armorValue);
        
        healthSystem.MaxHealth = currentSaveData.maxHealth;
        healthSystem.Health = currentSaveData.hpValue;
        CurrencySystem.Instance.SetCurrentCoins(currentSaveData.currCoins);
        Debug.Log($"Ӧ�ú����� - �ȼ�: {playerProperty.level}, ����: {playerProperty.currEXP}, Ѫ��: {healthSystem.Health}");
        RefreshHUDUI();
    }

    private void ApplyInventory()
    {
        if (InventoryManager.Instance == null || ItemDBManager.Instance == null)
        {
            Debug.LogWarning("��������δ��ʼ�����޷����ؿ��");
            return;
        }

        // ���ǰ�ȼ�¼��ǰ״̬
        Debug.Log($"Ӧ�ô浵ǰ - �ڴ�����Ʒ����: {InventoryManager.Instance.itemList.Count}");
        foreach (var item in InventoryManager.Instance.itemList)
        {
            Debug.Log($"Ӧ��ǰ���ڵ���Ʒ: {item.nameOfItem}, ����: {item.amount}");
        }

        // ʹ����շ���
        InventoryManager.Instance.ClearInventory();

        // ��¼�浵�е�����
        Debug.Log($"�浵�е���Ʒ����: {currentSaveData.inventoryItems.Count}");
        foreach (var itemData in currentSaveData.inventoryItems)
        {
            Debug.Log($"�浵��Ʒ: {itemData.itemId}, ����: {itemData.quantity}");
        }

        foreach (InventoryItemData itemData in currentSaveData.inventoryItems)
        {
            ItemSO itemTemplate = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.nameOfItem == itemData.itemId);
            if (itemTemplate != null)
            {
                ItemSO newItem = Instantiate(itemTemplate);
                newItem.amount = itemData.quantity;
                InventoryManager.Instance.ReAddItem(newItem);
                Debug.Log($"�Ӵ浵������Ʒ: {newItem.nameOfItem}, ����: {newItem.amount}");
            }
        }

        InventoryUI.Instance?.UpdateInventoryUI();
        Debug.Log($"Ӧ�ô浵�� - �ڴ�����Ʒ����: {InventoryManager.Instance.itemList.Count}");
    }

    private void ApplyQuickSlots()
    {
        if (QuickItemBar.Instance == null) return;
        if (currentSaveData.quickSlots == null || currentSaveData.quickSlots.Count == 0) return;
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < Mathf.Min(currentSaveData.quickSlots.Count, 7); i++)
        {
            var data = currentSaveData.quickSlots[i];
            if (string.IsNullOrEmpty(data.itemName))
                continue;

            // 必须从背包 itemList 查——背包内是 Instantiate 副本，QuickItemBar 用 == 引用比较
            ItemSO item = InventoryManager.Instance.itemList?.Find(it => it != null && it.nameOfItem == data.itemName);
            if (item != null)
                QuickItemBar.Instance.SetSlot(i, item, data.count);
        }
    }

    private void ApplyQuests()
    {
        if (QuestManager.Instance == null || QuestDBManager.Instance == null) return;

        // ������������״̬
        QuestManager.Instance.ResetAllQuests();

        foreach (QuestSaveData questData in currentSaveData.questProgress)
        {
            // ͨ��ID��������
            Quest quest = QuestDBManager.Instance.questDatabase.GetQuestByID(questData.questID);
            if (quest != null)
            {
                Debug.Log($"��������: {quest.questName}, ״̬: {questData.questState}");
                // �ָ�����״̬
                QuestManager.Instance.SetQuestState(quest, questData.questState);

                // �ָ�����Ŀ�����
                if (questData.objectiveProgress != null && quest.objectives != null)
                {
                    foreach (var objectiveProgress in questData.objectiveProgress)
                    {
                        if (objectiveProgress.objectiveIndex < quest.objectives.Count)
                        {
                            var objective = quest.objectives[objectiveProgress.objectiveIndex];
                            Debug.Log($"����Ŀ��{objectiveProgress.objectiveIndex}: ���״̬={objectiveProgress.isCompleted}");
                            objective.currentAmount = objectiveProgress.currentAmount;
                            objective.isCompleted = objectiveProgress.isCompleted;
                        }
                    }
                }

                //��Ϊ�������С��򡰿���ɡ�������������ʾ�ڽ�����
                if (questData.questState == QuestState.InProgress || questData.questState == QuestState.CanComplete)
                {
                    if (QuestPanelController.Instance != null)
                    {
                        if (quest.questType == QuestType.Main)
                        {
                            QuestPanelController.Instance.SetMainQuest(quest);
                        }
                        else if (quest.questType == QuestType.Side)
                        {
                            QuestPanelController.Instance.SetSideQuest(quest);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"�Ҳ���IDΪ {questData.questID} ������");
            }
        }

        // ��������UI
        QuestPanelController.Instance?.UpdateAllPanels();
        Debug.Log($"�Ѽ��� {currentSaveData.questProgress.Count} ���������");
    }

    private void ApplyEquipment()
    {
        StartCoroutine(ApplyEquipmentCoroutine());
    }

    private IEnumerator ApplyEquipmentCoroutine()
    {
        yield return null;

        Debug.Log("=== ��ʼװ������ ===");
        GameObject player = registeredPlayer ?? GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("�Ҳ�����Ҷ���");
            yield break;
        }

        Debug.Log($"�ҵ����: {player.name}");

        ArmorEquipmentManager equipmentManager = player.GetComponent<ArmorEquipmentManager>();
        ItemUsageHandler itemUsageHandler = player.GetComponent<ItemUsageHandler>();

        if (equipmentManager != null)
        {
            Debug.Log("��ʼװ������...");
            EquipArmorItem(currentSaveData.equippedHelmet, ArmorType.Helmet, equipmentManager);
            EquipArmorItem(currentSaveData.equippedChestplate, ArmorType.Chestplate, equipmentManager);
            EquipArmorItem(currentSaveData.equippedGauntlets, ArmorType.Gauntlets, equipmentManager);
            EquipArmorItem(currentSaveData.equippedLeggings, ArmorType.Leggings, equipmentManager);
            EquipArmorItem(currentSaveData.equippedBoots, ArmorType.Boots, equipmentManager);
            Debug.Log("����װ�����");
        }

        if (!string.IsNullOrEmpty(currentSaveData.equippedWeapon))
        {
            Debug.Log($"��ʼװ������: {currentSaveData.equippedWeapon}");
            ItemSO weaponItem = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.name == currentSaveData.equippedWeapon);

            if (weaponItem != null)
            {
                if (itemUsageHandler != null)
                    itemUsageHandler.UseItem(weaponItem);
                else if (ItemUsageHandler.Instance != null)
                    ItemUsageHandler.Instance.UseItem(weaponItem);
                else
                    Debug.LogError("�Ҳ������õ� ItemUsageHandler");
            }
        }


        // 恢复武器拔出状态：走 drawWeapon 动画，让动画事件调 DrawWeapon() 和 SetWeaponDrawState()
        if (currentSaveData.isWeaponDrawn && WeaponEquipmentManager.Instance != null)
        {
            yield return null;
            var anim = player.GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("drawWeapon");
            Debug.Log("已触发武器拔出动画恢复");
        }
        Debug.Log("=== װ�����̽��� ===");
    }

    private void EquipArmorItem(string itemName, ArmorType armorType, ArmorEquipmentManager equipmentManager)
    {
        if (!string.IsNullOrEmpty(itemName) && ItemDBManager.Instance != null)
        {
            ItemSO armorItem = ItemDBManager.Instance.itemDB.itemList.Find(i => i != null && i.name == itemName);
            if (armorItem != null && armorItem.itemType == ItemType.Armor)
                equipmentManager.EquipArmor(armorItem);
        }
    }
    #endregion

    #region ������UI����
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasSaveDataBeenAppliedInCurrentScene = false;

        // �ؼ��޸���ֹͣ�����������е�Э��
        StopAllCoroutines();
        isApplySaveDataAfterFrameRunning = false;
        isApplyingSaveData = false;

        Debug.Log($"=== OnSceneLoaded ������ === Scene: {scene.name}, Mode: {mode}");

        // ֻ�����ض������²Ŵ�����������
        if (shouldLoadFromSave && !string.IsNullOrEmpty(currentSaveId))
        {
            // ��ֹ�ظ�����Э��
            if (!isApplySaveDataAfterFrameRunning && !hasSaveDataBeenAppliedInCurrentScene)
            {
                Debug.Log("��ʼ ApplySaveDataAfterFrame Э��");
                StartCoroutine(ApplySaveDataAfterFrame());
            }
            else
            {
                Debug.LogWarning($"�����浵Ӧ�ã�isApplySaveDataAfterFrameRunning={isApplySaveDataAfterFrameRunning}, hasSaveDataBeenAppliedInCurrentScene={hasSaveDataBeenAppliedInCurrentScene}");
            }
        }
        else
        {
            Debug.Log($"�����浵Ӧ�ã�shouldLoadFromSave={shouldLoadFromSave}, currentSaveId={currentSaveId}");
        }
    }

    private IEnumerator ApplySaveDataAfterFrame()
    {
        // �������б�־
        if (isApplySaveDataAfterFrameRunning)
        {
            Debug.LogWarning("ApplySaveDataAfterFrame �Ѿ��������У�����");
            yield break;
        }

        isApplySaveDataAfterFrameRunning = true;
        Debug.Log("ApplySaveDataAfterFrame Э�̿�ʼ");

        yield return new WaitForSeconds(0.5f);

        Debug.Log("ApplySaveDataAfterFrame Э�̽��������� ApplySaveData");
        ApplySaveData();

        // ���ñ�־
        isApplySaveDataAfterFrameRunning = false;
    }


    private void RefreshHUDUI()
    {
        if (PlayerHUDUI.Instance != null)
        {
            PlayerHUDUI.Instance.RefreshUI();
            Debug.Log("HUDUI��ˢ��");
        }
        else
        {
            PlayerHUDUI hudUI = FindObjectOfType<PlayerHUDUI>();
            if (hudUI != null)
            {
                hudUI.RefreshUI();
                Debug.Log("ͨ������ˢ��HUDUI");
            }
        }
    }
    #endregion

    #region 自动存档
    private void Update()
    {
        autoSaveTimer += Time.deltaTime;
        if (autoSaveTimer >= autoSaveInterval)
        {
            autoSaveTimer = 0f;
            if (!IsPlayerInCombat() && Time.time - lastAutoSaveTime >= autoSaveCooldown)
            {
                StartCoroutine(AutoSaveCoroutine());
            }
            else
            {
                Debug.Log("自动存档跳过：玩家在战斗中或冷却中");
            }
        }
    }

    private IEnumerator AutoSaveCoroutine()
    {
        OnAutoSaveStart?.Invoke();
        yield return new WaitForSeconds(1.5f);
        SaveGame(updatePosition: false); // 自动存档不更新位置，只用篝火点
        lastAutoSaveTime = Time.time;
        OnAutoSaveComplete?.Invoke();
    }

    private bool IsPlayerInCombat()
    {
        if (EnemyManager.i == null) return false;
        var enemies = EnemyManager.i.GetEnemiesInRange();
        if (enemies == null || enemies.Count == 0) return false;

        return enemies.Any(e =>
            e.IsInState(EnemyStates.CombatMovement) ||
            e.IsInState(EnemyStates.Attack));
    }
    #endregion

    #region 应用退出
    private void OnApplicationQuit()
    {
        // 不再自动存档
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // 不再自动存档
    }
    #endregion
}