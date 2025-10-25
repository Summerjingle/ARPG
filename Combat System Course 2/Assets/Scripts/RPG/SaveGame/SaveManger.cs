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
    private GameSaveData currentSaveData;
    private GameObject playerObject;
    private bool isApplyingSaveData = false; // 新增：标记是否正在应用存档

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

    public void SaveGame()
    {
        // 使用缓存的玩家对象或重新查找
        GameObject player = playerObject != null ? playerObject : GameObject.FindGameObjectWithTag("Player");
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

        // 确保获取的是最新的属性值
        Debug.Log($"保存玩家属性 - 等级: {playerProperty.level}, 经验: {playerProperty.currEXP}, 血量: {playerProperty.hpValue}");

        // 填充保存数据
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
            if (item != null)
            {
                currentSaveData.inventoryItems.Add(item.name);
            }
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

    public void ApplySaveData()
    {
        if (isApplyingSaveData) return; // 防止重复应用

        isApplyingSaveData = true;
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

        int maxRetries = 15; // 增加重试次数
        float retryInterval = 0.2f; // 增加重试间隔

        for (int i = 0; i < maxRetries; i++)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerObject = player;

                // 检查玩家组件是否已初始化
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
        // 先应用基础属性，再应用装备（装备可能会影响属性）
        ApplyPlayerProperties(player);
        ApplyInventory();
        ApplyQuests();
        ApplyEquipment(); // 最后应用装备

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

        // 应用属性前先记录当前值
        Debug.Log($"应用前属性 - 等级: {playerProperty.level} -> {currentSaveData.level}, 经验: {playerProperty.currEXP} -> {currentSaveData.currEXP}");

        // 设置属性
        playerProperty.level = currentSaveData.level;
        playerProperty.currEXP = currentSaveData.currEXP;
        playerProperty.hpValue = currentSaveData.hpValue;
        playerProperty.energyValue = currentSaveData.energyValue;
        playerProperty.SetBaseArmor(currentSaveData.armorValue);

        meleeFighter.MaxHealth = currentSaveData.maxHealth;
        meleeFighter.Health = currentSaveData.hpValue; // 确保血量被正确设置

        // 验证属性是否应用成功
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

        // 确保库存列表已初始化
        if (InventoryManager.Instance.itemList == null)
        {
            InventoryManager.Instance.itemList = new List<ItemSO>();
        }
        else
        {
            InventoryManager.Instance.itemList.Clear();
        }

        foreach (string itemName in currentSaveData.inventoryItems)
        {
            ItemSO item = ItemDBManager.Instance?.itemDB?.itemList?.Find(i => i != null && i.name == itemName);
            if (item != null)
            {
                InventoryManager.Instance.AddItem(item);
            }
        }

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UpdateInventoryUI();
        }
    }

    private void ApplyEquipment()
    {
        // 延迟一帧应用装备，确保属性已设置
        StartCoroutine(ApplyEquipmentCoroutine());
    }

    private IEnumerator ApplyEquipmentCoroutine()
    {
        yield return null; // 等待一帧，确保属性已应用

        Debug.Log("=== 开始装备流程 ===");

        // 使用标签查找玩家，而不是 FindObjectOfType（可能找到敌人）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("找不到玩家对象！");
            yield break;
        }
        Debug.Log($"找到玩家: {player.name}");

        ArmorEquipmentManager equipmentManager = player.GetComponent<ArmorEquipmentManager>();
        MeleeFighter meleeFighter = player.GetComponent<MeleeFighter>();
        ItemUsageHandler itemUsageHandler = player.GetComponent<ItemUsageHandler>();

        Debug.Log($"ArmorEquipmentManager: {equipmentManager != null}");
        Debug.Log($"MeleeFighter: {meleeFighter != null}");
        Debug.Log($"ItemUsageHandler: {itemUsageHandler != null}");

        if (itemUsageHandler != null)
        {
            Debug.Log($"Weapon1Socket: {itemUsageHandler.weapon1Socket != null}");
            if (itemUsageHandler.weapon1Socket != null)
            {
                Debug.Log($"Weapon1Socket 名称: {itemUsageHandler.weapon1Socket.name}");
                Debug.Log($"Weapon1Socket 子物体数量: {itemUsageHandler.weapon1Socket.childCount}");
            }
        }

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
            Debug.Log($"找到武器物品: {weaponItem != null}");

            if (weaponItem != null)
            {
                Debug.Log($"武器物品名称: {weaponItem.name}");

                // 优先使用玩家自身的 ItemUsageHandler
                if (itemUsageHandler != null)
                {
                    Debug.Log("使用玩家自身的 ItemUsageHandler 装备武器");
                    itemUsageHandler.UseItem(weaponItem);
                }
                // 备用：使用单例
                else if (ItemUsageHandler.Instance != null)
                {
                    Debug.Log("使用 ItemUsageHandler 单例装备武器");
                    ItemUsageHandler.Instance.UseItem(weaponItem);
                }
                else
                {
                    Debug.LogError("找不到可用的 ItemUsageHandler");
                }

                // 等待一帧让装备完成
                yield return null;

                // 检查装备结果
                Debug.Log($"装备后 currentWeapon: {meleeFighter.currentWeapon != null}");
                if (meleeFighter.currentWeapon != null)
                {
                    Debug.Log($"当前武器名称: {meleeFighter.currentWeapon.name}");
                    if (meleeFighter.currentWeapon.transform.parent != null)
                    {
                        Debug.Log($"武器父物体: {meleeFighter.currentWeapon.transform.parent.name}");
                    }
                    else
                    {
                        Debug.LogWarning("武器没有父物体！");
                    }
                }
            }
            else
            {
                Debug.LogError($"找不到武器物品: {currentSaveData.equippedWeapon}");
            }
        }
        else
        {
            Debug.Log($"不需要装备武器或MeleeFighter为null: weapon={currentSaveData.equippedWeapon}, meleeFighter={meleeFighter != null}");
        }

        Debug.Log("=== 装备流程结束 ===");
    }

    private void EquipArmorItem(string itemName, ArmorType armorType, ArmorEquipmentManager equipmentManager)
    {
        if (!string.IsNullOrEmpty(itemName) && ItemDBManager.Instance != null)
        {
            ItemSO armorItem = ItemDBManager.Instance.itemDB.itemList.Find(i => i != null && i.name == itemName);
            if (armorItem != null && armorItem.itemType == ItemType.Armor)
            {
                equipmentManager.EquipArmor(armorItem);
            }
        }
    }

    private void ApplyQuests()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("游戏管理器未初始化，无法加载任务");
            return;
        }

        foreach (QuestSaveData questData in currentSaveData.questProgress)
        {
            Quest quest = GameManager.Instance.allQuests.Find(q => q != null && q.questName == questData.questName);
            if (quest != null)
            {
                GameManager.Instance.SetQuestState(quest, questData.questState);
            }
        }

        if (QuestPanelController.Instance != null)
        {
            QuestPanelController.Instance.UpdateAllPanels();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shouldLoadFromSave)
        {
            // 等待更长时间确保场景完全加载
            StartCoroutine(ApplySaveDataAfterFrame());
        }
    }

    private IEnumerator ApplySaveDataAfterFrame()
    {
        yield return new WaitForSeconds(1f); // 增加等待时间到1秒
        ApplySaveData();
        shouldLoadFromSave = false;
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            Debug.LogWarning("找不到PlayerHUDUI实例，尝试查找...");
            PlayerHUDUI hudUI = FindObjectOfType<PlayerHUDUI>();
            if (hudUI != null)
            {
                hudUI.RefreshUI();
                Debug.Log("通过查找刷新HUDUI");
            }
            else
            {
                Debug.LogError("无法找到PlayerHUDUI");
            }
        }
    }
}