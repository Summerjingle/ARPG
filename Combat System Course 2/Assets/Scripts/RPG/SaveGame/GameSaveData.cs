using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveData
{
    // 场景信息
    public string currentScene;
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;

    // 玩家属性
    public int level;
    public int currEXP;
    public int hpValue;
    public int maxHealth;
    public int energyValue;
    public int armorValue;

    // 装备信息
    public string equippedWeapon;
    public string equippedHelmet;
    public string equippedChestplate;
    public string equippedGauntlets;
    public string equippedLeggings;
    public string equippedBoots;

    // 背包内容
    public List<string> inventoryItems;

    // 任务进度
    public List<QuestSaveData> questProgress;

    // 保存时间
    public DateTime saveTime;
}

[System.Serializable]
public class QuestSaveData
{
    public string questName;
    public QuestState questState;

    public QuestSaveData(string name, QuestState state)
    {
        questName = name;
        questState = state;
    }
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