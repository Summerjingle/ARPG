using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class GameSaveData
{
    public string saveId; // �浵Ψһ��ʶ
    public string saveName; // �浵����
    public int saveSlot; // �浵��λ

    // ������Ϣ
    public string currentScene;
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;

    // �������
    public int level;
    public int currEXP;
    public int currSoulAmount;
    public int hpValue;
    public int maxHealth;
    public float energyValue;
    public int armorValue;
    public int currCoins;


    // װ����Ϣ
    public string equippedWeapon;
    public string equippedHelmet;
    public string equippedChestplate;
    public string equippedGauntlets;
    public string equippedLeggings;
    public string equippedBoots;

    // ��������
    public List<InventoryItemData> inventoryItems;

    // �������
    public List<QuestSaveData> questProgress;
    public bool showCompletedQuests = false;
    public bool autoTrackNewQuests = true;
    public string currentlyTrackedQuestID;

    // ����ʱ��
    public DateTime saveTime;

    // ���캯��
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
        saveName = $"�浵 {slot + 1}";
    }

    #region 静态物品
    // ʰȡ��Ʒ�ֵ�
    public Dictionary<string, HashSet<string>> scenePickedItems = new Dictionary<string, HashSet<string>>();
    // ��鳡���е���Ʒ�Ƿ��ѱ�ʰȡ
    public bool IsSceneItemPicked(string sceneName, string itemId)
    {
        if (scenePickedItems.TryGetValue(sceneName, out HashSet<string> pickedItems))
        {
            return pickedItems.Contains(itemId);
        }
        return false;
    }

    // ��ǳ����е���ƷΪ��ʰȡ
    public void MarkSceneItemAsPicked(string sceneName, string itemId)
    {
        if (!scenePickedItems.ContainsKey(sceneName))
        {
            scenePickedItems[sceneName] = new HashSet<string>();
        }
        scenePickedItems[sceneName].Add(itemId);
    }
    #endregion

    #region 静态机关
    // ���ؼ���״̬�ֵ�
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
    public string itemId;        // ��ƷID������
    public int quantity;         // ����
    public int maxStack;         // ���ѵ���

    public InventoryItemData(string id, int count, int max = 1)
    {
        itemId = id;
        quantity = count;
        maxStack = max;
    }

    // Ĭ�Ϲ��캯���������л�
    public InventoryItemData() { }
}

[System.Serializable]
public class QuestSaveData
{
    public string questID;        // ��Ϊʹ��ID����������
    public QuestState questState;
    public List<ObjectiveProgress> objectiveProgress; // ����Ŀ�����

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
    public int objectiveIndex;    // Ŀ������
    public int currentAmount;     // ��ǰ����
    public bool isCompleted;      // �Ƿ����
}

// �����л���Vector3����ṹ
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

// �����л���Quaternion����ṹ
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

// �����л���Color����ṹ
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