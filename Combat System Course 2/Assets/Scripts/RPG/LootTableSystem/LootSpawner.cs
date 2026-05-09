using System.Collections.Generic;
using UnityEngine;

public static class LootSpawner
{
    public static void SpawnLootItems(Vector3 deathPosition, LootTable lootTable, Transform parent = null)
    {
        if (lootTable == null)
        {
            Debug.LogWarning("LootTable为空，无法生成掉落物品");
            return;
        }

        if (lootTable.lootPrefab == null)
        {
            Debug.LogWarning($"LootTable {lootTable.name} 没有设置 lootPrefab");
            return;
        }

        var lootItems = lootTable.GetRandomLoot();

        foreach (var dropData in lootItems)
        {
            Vector3 spawnPosition = deathPosition + dropData.spawnOffset;

            if (Physics.Raycast(spawnPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 3f,
                LayerMask.GetMask("Ground", "Default")))
            {
                spawnPosition = hit.point + Vector3.up * 0.1f;
            }

            GameObject droppedItem = Object.Instantiate(lootTable.lootPrefab, spawnPosition, Quaternion.identity);

            if (parent != null)
                droppedItem.transform.SetParent(parent);

            PickableObject po = droppedItem.GetComponent<PickableObject>();
            if (po == null)
                po = droppedItem.AddComponent<PickableObject>();
            po.itemSO = dropData.item;

            Debug.Log($"生成的掉落: {dropData.item.nameOfItem} 在位置 {spawnPosition}");
        }

        Debug.Log($"从 {lootTable.name} 生成了 {lootItems.Count} 个物品");
    }
}
