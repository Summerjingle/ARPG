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
    public static bool shouldLoadPosition = false; // 新增：控制是否加载位置

    private string savePath;
    private string savesDirectory;
    private const int MAX_SAVE_SLOTS = 10;

    private GameObject registeredPlayer;
    public GameSaveData currentSaveData;
    private bool isApplyingSaveData = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region 存档管理
    public void CreateNewGame(int slot)
    {
        isNewGame = true;
        shouldLoadFromSave = false;
        shouldLoadPosition = false; // 新游戏不加载位置

        // 查找可用的空槽位（不覆盖现有存档）
        int availableSlot = FindEmptySaveSlot();

        // 创建全新的存档（不覆盖现有存档）
        currentSaveData = new GameSaveData(availableSlot);
        currentSaveId = currentSaveData.saveId;

        Debug.Log($"创建新游戏，使用空槽位: {availableSlot}, 存档ID: {currentSaveId}");
    }

    // 新增方法：查找空槽位
    private int FindEmptySaveSlot()
    {
        var existingSaves = GetAllSaves();

        // 查找第一个空槽位（0-9）
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            if (!existingSaves.Any(save => save.saveSlot == i))
            {
                return i; // 找到空槽位
            }
        }

        // 如果没有空槽位，使用新的槽位编号（不覆盖现有存档）
        return existingSaves.Count;
    }

    public void LoadGame(string saveId)
    {
        isNewGame = false;
        shouldLoadFromSave = true;
        shouldLoadPosition = true; // 从主菜单加载存档时需要加载位置
        currentSaveId = saveId;
        LoadGameData(saveId);
        Debug.Log($"准备加载存档: {saveId}");
    }

    public void DeleteSave(string saveId)
    {
        string savePath = GetSavePath(saveId);
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"已删除存档: {saveId}");
        }

        // 如果删除的是当前存档，重置状态
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
                Debug.LogError($"加载存档文件失败 {filePath}: {e.Message}");
            }
        }

        // 按保存时间排序，最新的在前
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
                Debug.LogError($"加载存档失败: {e.Message}");
            }
        }
        return null;
    }

    private void LoadGameData(string saveId)
    {
        currentSaveData = GetSaveData(saveId);
        if (currentSaveData == null)
        {
            Debug.LogWarning($"存档不存在: {saveId}");
            currentSaveData = new GameSaveData();
        }
        else
        {
            Debug.Log("游戏数据已加载");
        }
    }

    private string GetSavePath(string saveId)
    {
        return Path.Combine(savesDirectory, $"{saveId}.json");
    }
    #endregion

    #region 保存游戏
    public void SaveGame()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("没有当前存档数据，无法保存");
            return;
        }

        // 检查是否为新游戏且还没有保存过
        if (isNewGame && !HasBeenSavedBefore())
        {
            // 新游戏第一次保存，使用空槽位
            int availableSlot = FindEmptySaveSlot();
            currentSaveData.saveSlot = availableSlot;
            currentSaveData.saveName = $"存档 {availableSlot + 1}";
            Debug.Log($"新游戏第一次保存，使用槽位: {availableSlot}");
        }

        GameObject player = registeredPlayer ?? GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("找不到玩家对象，无法保存游戏");
            return;
        }

        PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
        MeleeFighter meleeFighter = player.GetComponent<MeleeFighter>();
        ArmorEquipmentManager equipmentManager = player.GetComponent<ArmorEquipmentManager>();

        if (playerProperty == null || meleeFighter == null)
        {
            Debug.LogWarning("玩家缺少必要的组件，无法保存游戏");
            return;
        }

        Debug.Log($"保存玩家属性 - 等级: {playerProperty.level}, 经验: {playerProperty.currEXP}, 血量: {playerProperty.hpValue}");

        // 更新存档数据 - 保存位置和场景
        currentSaveData.currentScene = SceneManager.GetActiveScene().name;
        currentSaveData.playerPosition = player.transform.position;
        currentSaveData.playerRotation = player.transform.rotation;

        currentSaveData.level = playerProperty.level;
        currentSaveData.currEXP = playerProperty.currEXP;
        currentSaveData.hpValue = playerProperty.hpValue;
        currentSaveData.maxHealth = Mathf.RoundToInt(meleeFighter.MaxHealth);
        currentSaveData.energyValue = playerProperty.energyValue;
        currentSaveData.armorValue = playerProperty.GetBaseArmor();
        currentSaveData.saveTime = System.DateTime.Now;

        // 清空旧数据
        currentSaveData.inventoryItems.Clear();
        currentSaveData.questProgress.Clear();

        SaveEquipment(equipmentManager, meleeFighter);
        SaveInventory();
        SaveQuests();

        try
        {
            string savePath = GetSavePath(currentSaveData.saveId);
            string jsonData = JsonConvert.SerializeObject(currentSaveData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(savePath, jsonData);
            Debug.Log($"游戏已保存到槽位: {currentSaveData.saveSlot}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存游戏失败: {e.Message}");
        }
    }

    private bool HasBeenSavedBefore()
    {
        string savePath = GetSavePath(currentSaveData.saveId);
        return File.Exists(savePath);
    }

    private void SaveEquipment(ArmorEquipmentManager equipmentManager, MeleeFighter meleeFighter)
    {
        if (equipmentManager != null)
        {
            currentSaveData.equippedHelmet = GetEquippedItemName(equipmentManager, ArmorType.Helmet);
            currentSaveData.equippedChestplate = GetEquippedItemName(equipmentManager, ArmorType.Chestplate);
            currentSaveData.equippedGauntlets = GetEquippedItemName(equipmentManager, ArmorType.Gauntlets);
            currentSaveData.equippedLeggings = GetEquippedItemName(equipmentManager, ArmorType.Leggings);
            currentSaveData.equippedBoots = GetEquippedItemName(equipmentManager, ArmorType.Boots);
        }

        if (meleeFighter != null && meleeFighter.currentWeapon != null)
        {
            PickableObject weaponPickable = meleeFighter.currentWeapon.GetComponent<PickableObject>();
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
            Debug.LogWarning("库存管理器未初始化，无法保存库存");
            return;
        }

        currentSaveData.inventoryItems.Clear();

       
        var itemStacks = InventoryManager.Instance.GetAllItemStacks();
        currentSaveData.inventoryItems.AddRange(itemStacks);

        Debug.Log($"已保存 {currentSaveData.inventoryItems.Count} 个物品堆叠");
    }

    private void SaveQuests()
    {
        if (QuestManager.Instance == null || QuestDBManager.Instance == null)
        {
            Debug.LogWarning("任务管理器未初始化，无法保存任务");
            return;
        }

        currentSaveData.questProgress = new List<QuestSaveData>();

        // 保存所有任务的状态和进度
        foreach (Quest quest in QuestDBManager.Instance.questDatabase.allQuests)
        {
            if (quest != null)
            {
                QuestState state = QuestManager.Instance.GetQuestState(quest);
                QuestSaveData questSaveData = new QuestSaveData(quest.questID, state);

                // 保存任务目标进度
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
                    }
                }

                currentSaveData.questProgress.Add(questSaveData);
            }
        }

        Debug.Log($"已保存 {currentSaveData.questProgress.Count} 个任务进度");
    }
    #endregion

    #region 加载存档
    public void ApplySaveData()
    {
        if (isApplyingSaveData) return;
        StartCoroutine(ApplySaveDataWithRetry());
    }

    private IEnumerator ApplySaveDataWithRetry()
    {
        if (isNewGame)
        {
            Debug.Log("新游戏开始，使用场景默认设置");
            isApplyingSaveData = false;
            yield break;
        }

        if (currentSaveData == null)
        {
            Debug.LogWarning("没有可应用的存档数据");
            isApplyingSaveData = false;
            yield break;
        }

        int maxRetries = 15;
        float retryInterval = 0.2f;

        for (int i = 0; i < maxRetries; i++)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
                MeleeFighter meleeFighter = player.GetComponent<MeleeFighter>();

                if (playerProperty != null && meleeFighter != null)
                {
                    ApplySaveDataToPlayer(player);
                    Debug.Log("存档数据已应用");
                    isApplyingSaveData = false;
                    yield break;
                }
                else
                {
                    Debug.Log($"找到玩家对象，但组件未完全初始化，重试第 {i + 1} 次");
                }
            }

            Debug.Log($"第 {i + 1} 次尝试寻找玩家对象...");
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogError($"在 {maxRetries} 次重试后仍然找不到玩家对象，无法应用存档数据");
        isApplyingSaveData = false;
    }

    private void ApplySaveDataToPlayer(GameObject player)
    {
        // 只有在需要时才应用位置数据
        if (shouldLoadPosition)
        {
            ApplyPlayerPosition(player);
        }

        ApplyPlayerProperties(player);
        ApplyInventory();
        ApplyQuests();
        ApplyEquipment();

        Debug.Log($"应用存档完成 - 等级: {currentSaveData.level}, 经验: {currentSaveData.currEXP}, 血量: {currentSaveData.hpValue}");
        RefreshHUDUI();
    }

    // 新增方法：应用玩家位置
    private void ApplyPlayerPosition(GameObject player)
    {
        if (currentSaveData.playerPosition.ToVector3() != Vector3.zero)
        {
            player.transform.position = currentSaveData.playerPosition.ToVector3();
            player.transform.rotation = currentSaveData.playerRotation.ToQuaternion();
            Debug.Log($"应用玩家位置: {player.transform.position}, 旋转: {player.transform.rotation}");
        }
        else
        {
            Debug.Log("存档中没有位置数据，使用默认出生点");
        }
    }

    private void ApplyPlayerProperties(GameObject player)
    {
        PlayerProperty playerProperty = player.GetComponent<PlayerProperty>();
        MeleeFighter meleeFighter = player.GetComponent<MeleeFighter>();

        if (playerProperty == null || meleeFighter == null)
        {
            Debug.LogWarning("玩家缺少必要的属性组件");
            return;
        }

        Debug.Log($"应用前属性 - 等级: {playerProperty.level} -> {currentSaveData.level}, 经验: {playerProperty.currEXP} -> {currentSaveData.currEXP}");

        playerProperty.level = currentSaveData.level;
        playerProperty.currEXP = currentSaveData.currEXP;
        playerProperty.hpValue = currentSaveData.hpValue;
        playerProperty.energyValue = currentSaveData.energyValue;
        playerProperty.SetBaseArmor(currentSaveData.armorValue);

        meleeFighter.MaxHealth = currentSaveData.maxHealth;
        meleeFighter.Health = currentSaveData.hpValue;

        Debug.Log($"应用后属性 - 等级: {playerProperty.level}, 经验: {playerProperty.currEXP}, 血量: {meleeFighter.Health}");
        RefreshHUDUI();
    }

    private void ApplyInventory()
    {
        if (InventoryManager.Instance == null || ItemDBManager.Instance == null)
        {
            Debug.LogWarning("库存管理器未初始化，无法加载库存");
            return;
        }

        // 使用清空方法
        InventoryManager.Instance.ClearInventory();

        foreach (InventoryItemData itemData in currentSaveData.inventoryItems)
        {
            ItemSO itemTemplate = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.nameOfItem == itemData.itemId);
            if (itemTemplate != null)
            {
                ItemSO newItem = Instantiate(itemTemplate);
                newItem.amount = itemData.quantity;
                InventoryManager.Instance.ReAddItem(newItem);
            }
        }

        InventoryUI.Instance?.UpdateInventoryUI();
        Debug.Log($"内存中物品数量: {InventoryManager.Instance.itemList.Count}");
        foreach (var item in InventoryManager.Instance.itemList)
        {
            Debug.Log($"物品: {item.nameOfItem}, 数量: {item.amount}");
        }
        Debug.Log($"已加载 {currentSaveData.inventoryItems.Count} 个物品堆叠");
        
    }
    private void ApplyQuests()
    {
        if (QuestManager.Instance == null || QuestDBManager.Instance == null) return;

        // 重置所有任务状态
        QuestManager.Instance.ResetAllQuests();

        foreach (QuestSaveData questData in currentSaveData.questProgress)
        {
            // 通过ID查找任务
            Quest quest = QuestDBManager.Instance.questDatabase.GetQuestByID(questData.questID);
            if (quest != null)
            {
                // 恢复任务状态
                QuestManager.Instance.SetQuestState(quest, questData.questState);

                // 恢复任务目标进度
                if (questData.objectiveProgress != null && quest.objectives != null)
                {
                    foreach (var objectiveProgress in questData.objectiveProgress)
                    {
                        if (objectiveProgress.objectiveIndex < quest.objectives.Count)
                        {
                            var objective = quest.objectives[objectiveProgress.objectiveIndex];
                            objective.currentAmount = objectiveProgress.currentAmount;
                            objective.isCompleted = objectiveProgress.isCompleted;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"找不到ID为 {questData.questID} 的任务");
            }
        }

        // 更新任务UI
        QuestPanelController.Instance?.UpdateAllPanels();
        Debug.Log($"已加载 {currentSaveData.questProgress.Count} 个任务进度");
    }

    private void ApplyEquipment()
    {
        StartCoroutine(ApplyEquipmentCoroutine());
    }

    private IEnumerator ApplyEquipmentCoroutine()
    {
        yield return null;

        Debug.Log("=== 开始装备流程 ===");
        GameObject player = registeredPlayer ?? GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("找不到玩家对象！");
            yield break;
        }

        Debug.Log($"找到玩家: {player.name}");

        ArmorEquipmentManager equipmentManager = player.GetComponent<ArmorEquipmentManager>();
        MeleeFighter meleeFighter = player.GetComponent<MeleeFighter>();
        ItemUsageHandler itemUsageHandler = player.GetComponent<ItemUsageHandler>();

        if (equipmentManager != null)
        {
            Debug.Log("开始装备护甲...");
            EquipArmorItem(currentSaveData.equippedHelmet, ArmorType.Helmet, equipmentManager);
            EquipArmorItem(currentSaveData.equippedChestplate, ArmorType.Chestplate, equipmentManager);
            EquipArmorItem(currentSaveData.equippedGauntlets, ArmorType.Gauntlets, equipmentManager);
            EquipArmorItem(currentSaveData.equippedLeggings, ArmorType.Leggings, equipmentManager);
            EquipArmorItem(currentSaveData.equippedBoots, ArmorType.Boots, equipmentManager);
            Debug.Log("护甲装备完成");
        }

        if (!string.IsNullOrEmpty(currentSaveData.equippedWeapon) && meleeFighter != null)
        {
            Debug.Log($"开始装备武器: {currentSaveData.equippedWeapon}");
            ItemSO weaponItem = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.name == currentSaveData.equippedWeapon);

            if (weaponItem != null)
            {
                if (itemUsageHandler != null)
                    itemUsageHandler.UseItem(weaponItem);
                else if (ItemUsageHandler.Instance != null)
                    ItemUsageHandler.Instance.UseItem(weaponItem);
                else
                    Debug.LogError("找不到可用的 ItemUsageHandler");
            }
        }

        Debug.Log("=== 装备流程结束 ===");
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

    #region 场景和UI管理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shouldLoadFromSave && !string.IsNullOrEmpty(currentSaveId))
            StartCoroutine(ApplySaveDataAfterFrame());
    }

    private IEnumerator ApplySaveDataAfterFrame()
    {
        yield return new WaitForSeconds(3f); // 3秒
        ApplySaveData();
        
    }

    private void RefreshHUDUI()
    {
        if (PlayerHUDUI.Instance != null)
        {
            PlayerHUDUI.Instance.RefreshUI();
            Debug.Log("HUDUI已刷新");
        }
        else
        {
            PlayerHUDUI hudUI = FindObjectOfType<PlayerHUDUI>();
            if (hudUI != null)
            {
                hudUI.RefreshUI();
                Debug.Log("通过查找刷新HUDUI");
            }
        }
    }
    #endregion

    #region 应用生命周期
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveGame();
    }
    #endregion
}