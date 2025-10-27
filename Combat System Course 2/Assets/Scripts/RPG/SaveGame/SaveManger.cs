using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static bool shouldLoadFromSave = false;
    public static bool isNewGame = true;

    private string savePath;

    private GameObject registeredPlayer;
    private GameSaveData currentSaveData;
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

        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        LoadGameData();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    #region 游戏数据管理
    public void StartNewGame()
    {
        isNewGame = true;
        shouldLoadFromSave = false;
        currentSaveData = new GameSaveData();

        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("已清除旧存档，开始新游戏");
        }
    }

    public void LoadGame()
    {
        isNewGame = false;
        shouldLoadFromSave = true;
        LoadGameData();
        Debug.Log("准备加载存档");
    }

    private void LoadGameData()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string jsonData = File.ReadAllText(savePath);
                currentSaveData = JsonConvert.DeserializeObject<GameSaveData>(jsonData);
                Debug.Log("游戏数据已加载");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载存档失败: {e.Message}");
                currentSaveData = new GameSaveData();
            }
        }
        else
        {
            currentSaveData = new GameSaveData();
            Debug.Log("创建新的存档数据");
        }
    }
    #endregion

    #region 保存游戏
    public void SaveGame()
    {
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

        currentSaveData = new GameSaveData
        {
            currentScene = SceneManager.GetActiveScene().name,
            level = playerProperty.level,
            currEXP = playerProperty.currEXP,
            hpValue = playerProperty.hpValue,
            maxHealth = Mathf.RoundToInt(meleeFighter.MaxHealth),
            energyValue = playerProperty.energyValue,
            armorValue = playerProperty.GetBaseArmor(),
            inventoryItems = new List<string>(),
            questProgress = new List<QuestSaveData>(),
            saveTime = System.DateTime.Now
        };

        SaveEquipment(equipmentManager, meleeFighter);
        SaveInventory();
        SaveQuests();

        try
        {
            string jsonData = JsonConvert.SerializeObject(currentSaveData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(savePath, jsonData);
            Debug.Log("游戏已保存");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存游戏失败: {e.Message}");
        }
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
        if (InventoryManager.Instance == null || InventoryManager.Instance.itemList == null)
        {
            Debug.LogWarning("库存管理器未初始化，无法保存库存");
            return;
        }

        foreach (ItemSO item in InventoryManager.Instance.itemList)
        {
            if (item != null) currentSaveData.inventoryItems.Add(item.name);
        }
    }

    private void SaveQuests()
    {
        if (GameManager.Instance == null || GameManager.Instance.allQuests == null)
        {
            Debug.LogWarning("游戏管理器未初始化，无法保存任务");
            return;
        }

        foreach (Quest quest in GameManager.Instance.allQuests)
        {
            if (quest != null)
            {
                QuestState state = GameManager.Instance.GetQuestState(quest);
                currentSaveData.questProgress.Add(new QuestSaveData(quest.questName, state));
            }
        }
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

            Debug.Log($"第 {i + 1} 次尝试查找玩家对象...");
            yield return new WaitForSeconds(retryInterval);
        }

        Debug.LogError($"在 {maxRetries} 次重试后仍然找不到玩家对象，无法应用存档数据");
        isApplyingSaveData = false;
    }

    private void ApplySaveDataToPlayer(GameObject player)
    {
        ApplyPlayerProperties(player);
        ApplyInventory();
        ApplyQuests();
        ApplyEquipment();

        Debug.Log($"应用存档完成 - 等级: {currentSaveData.level}, 经验: {currentSaveData.currEXP}, 血量: {currentSaveData.hpValue}");
        RefreshHUDUI();
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

        if (InventoryManager.Instance.itemList == null)
            InventoryManager.Instance.itemList = new List<ItemSO>();
        else
            InventoryManager.Instance.itemList.Clear();

        foreach (string itemName in currentSaveData.inventoryItems)
        {
            ItemSO item = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.name == itemName);
            if (item != null) InventoryManager.Instance.AddItem(item);
        }

        InventoryUI.Instance?.UpdateInventoryUI();
    }

    private void ApplyQuests()
    {
        if (GameManager.Instance == null) return;

        foreach (QuestSaveData questData in currentSaveData.questProgress)
        {
            Quest quest = GameManager.Instance.allQuests.Find(q => q != null && q.questName == questData.questName);
            if (quest != null) GameManager.Instance.SetQuestState(quest, questData.questState);
        }

        QuestPanelController.Instance?.UpdateAllPanels();
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
        if (shouldLoadFromSave)
            StartCoroutine(ApplySaveDataAfterFrame());
    }

    private IEnumerator ApplySaveDataAfterFrame()
    {
        yield return new WaitForSeconds(3f); // 3秒
        ApplySaveData();
        shouldLoadFromSave = false;
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