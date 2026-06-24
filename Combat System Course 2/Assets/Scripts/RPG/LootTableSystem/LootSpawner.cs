using System.Collections.Generic;
using UnityEngine;

public static class LootSpawner
{
    public static void SpawnLootItems(Vector3 deathPosition, LootTable lootTable, Transform parent = null, bool ejectFromChest = false)
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

            // 宝箱弹出：加 Rigidbody 并施加随机弹射力
            if (ejectFromChest)
            {
                // 解除父子关系，否则物品会跟随宝箱移动
                droppedItem.transform.SetParent(null);

                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = droppedItem.AddComponent<Rigidbody>();
                rb.freezeRotation=true;
                // 随机方向：前方扇形 + 上方
                Vector3 forward = parent != null ? parent.forward : Vector3.forward;
                float spreadX = Random.Range(-0.3f, 0.3f);
                float spreadZ = Random.Range(-0.3f, 0.3f);
                Vector3 ejectDir = (forward + new Vector3(spreadX, 0.6f, spreadZ)).normalized;
                float ejectPower = Random.Range(2.5f, 4.5f);
                rb.AddForce(ejectDir * ejectPower, ForceMode.Impulse);
                
               
            }

            Debug.Log($"生成的掉落: {dropData.item.nameOfItem} 在位置 {spawnPosition}");
        }

        Debug.Log($"从 {lootTable.name} 生成了 {lootItems.Count} 个物品");
    }
}
