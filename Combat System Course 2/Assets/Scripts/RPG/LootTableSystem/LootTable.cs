using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemSO item;
        [Range(0f, 1f)] public float probability = 0.5f;//物品掉落概率
        public int minCount = 1;
        public int maxCount = 1;

        // 掉落位置偏移（可选）
        public Vector2 spawnOffsetRange = new Vector2(0.5f, 1.5f);
    }

    [Header("掉落设置")]
    public List<LootEntry> lootEntries = new List<LootEntry>();

    [Header("全局设置")]
    public bool useRandomSpawnOffset = true;
    public float spawnRadius = 1f;

    public List<ItemDropData> GetRandomLoot()
    {
        List<ItemDropData> result = new List<ItemDropData>();

        foreach (var entry in lootEntries)
        {
            if (entry.item == null) continue;

            if (Random.value <= entry.probability)
            {
                int count = Random.Range(entry.minCount, entry.maxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    Vector3 spawnOffset = Vector3.zero;
                    if (useRandomSpawnOffset)
                    {
                        float offset = Random.Range(entry.spawnOffsetRange.x, entry.spawnOffsetRange.y);
                        Vector2 randomCircle = Random.insideUnitCircle * offset;
                        spawnOffset = new Vector3(randomCircle.x, 0.5f, randomCircle.y);
                    }

                    result.Add(new ItemDropData
                    {
                        item = entry.item,
                        spawnOffset = spawnOffset
                    });
                }
            }
        }

        return result;
    }
}

[System.Serializable]
public class ItemDropData
{
    public ItemSO item;
    public Vector3 spawnOffset;
}