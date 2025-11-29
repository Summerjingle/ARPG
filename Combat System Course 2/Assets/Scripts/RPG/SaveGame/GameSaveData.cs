using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class GameSaveData
{
    public string saveId; // 存档唯一标识
    public string saveName; // 存档名称
    public int saveSlot; // 存档槽位

    // 场景信息
    public string currentScene;
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;

    // 玩家属性
    public int level;
    public int currEXP;
    public int hpValue;
    public int maxHealth;
    public float energyValue;
    public int armorValue;

    // 装备信息
    public string equippedWeapon;
    public string equippedHelmet;
    public string equippedChestplate;
    public string equippedGauntlets;
    public string equippedLeggings;
    public string equippedBoots;

    // 背包内容
    public List<InventoryItemData> inventoryItems;

    // 任务进度
    public List<QuestSaveData> questProgress;
    public bool showCompletedQuests = false;
    public bool autoTrackNewQuests = true;
    public string currentlyTrackedQuestID;

    // 保存时间
    public DateTime saveTime;

    // 构造函数
    public GameSaveData()
    {
        saveId = Guid.NewGuid().ToString();
        saveTime = DateTime.Now;
        inventoryItems = new List<InventoryItemData>();
        questProgress = new List<QuestSaveData>();
        scenePickedItems = new Dictionary<string, HashSet<string>>();
        sceneMechanismStates = new Dictionary<string, HashSet<string>>(); 
    }

    public GameSaveData(int slot) : this()
    {
        saveSlot = slot;
        saveName = $"存档 {slot + 1}";
    }

    #region 单个存档内一次性物品拾取逻辑
    // 拾取物品字典
    public Dictionary<string, HashSet<string>> scenePickedItems = new Dictionary<string, HashSet<string>>();
    // 检查场景中的物品是否已被拾取
    public bool IsSceneItemPicked(string sceneName, string itemId)
    {
        if (scenePickedItems.TryGetValue(sceneName, out HashSet<string> pickedItems))
        {
            return pickedItems.Contains(itemId);
        }
        return false;
    }

    // 标记场景中的物品为已拾取
    public void MarkSceneItemAsPicked(string sceneName, string itemId)
    {
        if (!scenePickedItems.ContainsKey(sceneName))
        {
            scenePickedItems[sceneName] = new HashSet<string>();
        }
        scenePickedItems[sceneName].Add(itemId);
    }
    #endregion

    #region 单个存档内一次性机关触发逻辑
    // 机关激活状态字典
    public Dictionary<string, HashSet<string>> sceneMechanismStates = new Dictionary<string, HashSet<string>>();
    public bool IsMechanismActivated(string sceneName, string mechanismId)
    {
        if (sceneMechanismStates.TryGetValue(sceneName, out HashSet<string> activatedMechanisms))
        {
            return activatedMechanisms.Contains(mechanismId);
        }
        return false;
    }

    public void MarkMechanismAsActivated(string sceneName, string mechanismId)
    {
        if (!sceneMechanismStates.ContainsKey(sceneName))
        {
            sceneMechanismStates[sceneName] = new HashSet<string>();
        }
        sceneMechanismStates[sceneName].Add(mechanismId);
    }
    #endregion
}
[System.Serializable]
public class InventoryItemData
{
    public string itemId;        // 物品ID或名称
    public int quantity;         // 数量
    public int maxStack;         // 最大堆叠数

    public InventoryItemData(string id, int count, int max = 1)
    {
        itemId = id;
        quantity = count;
        maxStack = max;
    }

    // 默认构造函数用于序列化
    public InventoryItemData() { }
}

[System.Serializable]
public class QuestSaveData
{
    public string questID;        // 改为使用ID而不是名称
    public QuestState questState;
    public List<ObjectiveProgress> objectiveProgress; // 保存目标进度

    public QuestSaveData(string id, QuestState state)
    {
        questID = id;
        questState = state;
        objectiveProgress = new List<ObjectiveProgress>();
    }
}

[System.Serializable]
public class ObjectiveProgress
{
    public int objectiveIndex;    // 目标索引
    public int currentAmount;     // 当前进度
    public bool isCompleted;      // 是否完成
}

// 可序列化的Vector3替代结构
[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }

    public static implicit operator SerializableVector3(Vector3 vector)
    {
        return new SerializableVector3(vector);
    }

    public static implicit operator Vector3(SerializableVector3 serializableVector)
    {
        return serializableVector.ToVector3();
    }
}

// 可序列化的Quaternion替代结构
[System.Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }

    public static implicit operator SerializableQuaternion(Quaternion quaternion)
    {
        return new SerializableQuaternion(quaternion);
    }

    public static implicit operator Quaternion(SerializableQuaternion serializableQuaternion)
    {
        return serializableQuaternion.ToQuaternion();
    }
}

// 可序列化的Color替代结构
[System.Serializable]
public struct SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SerializableColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }

    public static implicit operator SerializableColor(Color color)
    {
        return new SerializableColor(color);
    }

    public static implicit operator Color(SerializableColor serializableColor)
    {
        return serializableColor.ToColor();
    }
}