using System.Collections.Generic;
using UnityEngine;

public static class LootSpawner
{
    public static void SpawnLootItems(Vector3 deathPosition, LootTable lootTable, Transform parent = null)
    {
        if (lootTable == null)
        {
            Debug.LogWarning("LootTable为空，无法生成物品");
            return;
        }

        var lootItems = lootTable.GetRandomLoot();

        foreach (var dropData in lootItems)
        {
            if (dropData.item?.interactablePrefab == null)
            {
                Debug.LogWarning("物品或预制体为空，跳过生成");
                continue;
            }

            Vector3 spawnPosition = deathPosition + dropData.spawnOffset;

            // 确保生成位置在地面上
            if (Physics.Raycast(spawnPosition + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 3f,
                LayerMask.GetMask("Ground", "Default")))
            {
                spawnPosition = hit.point + Vector3.up * 0.1f;
            }

            GameObject droppedItem = Object.Instantiate(dropData.item.interactablePrefab, spawnPosition, Quaternion.identity);

            // 设置父级（可选，用于组织层次结构）
            if (parent != null)
            {
                droppedItem.transform.SetParent(parent);
            }

            // 确保有PickableObject组件
            PickableObject po = droppedItem.GetComponent<PickableObject>();
            if (po == null)
            {
                po = droppedItem.AddComponent<PickableObject>();
            }
            po.itemSO = dropData.item;

            // 添加随机旋转（视觉效果）
            droppedItem.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Debug.Log($"生成掉落物: {dropData.item.nameOfItem} 在位置 {spawnPosition}");
        }

        Debug.Log($"从 {lootTable.name} 生成了 {lootItems.Count} 个物品");
    }
}