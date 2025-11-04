using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public List<ItemSO> itemList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); return;
        }
        Instance = this;
    }

    public void AddItem(ItemSO item)
    {
        if (item.IsStackable())
        {
            // 查找背包中是否已有相同物品
            ItemSO existingItem = FindStackableItem(item);

            if (existingItem != null)
            {
                // 可以堆叠，增加数量
                int remainingSpace = existingItem.maxStackSize - existingItem.amount;
                int amountToAdd = Mathf.Min(item.amount, remainingSpace);

                existingItem.amount += amountToAdd;
                MessageUI.Instance.Show($"{item.nameOfItem} 数量增加至 {existingItem.amount}");

                // 如果还有剩余物品，递归添加
                if (item.amount > amountToAdd)
                {
                    item.amount -= amountToAdd;
                    AddItem(item); // 递归处理剩余物品
                }

                InventoryUI.Instance.UpdateItemAmountDisplay(existingItem);
            }
            else
            {
                // 没有找到可堆叠的物品，添加新物品
                ItemSO newItem = Instantiate(item); // 创建副本以避免修改原始SO
                newItem.amount = item.amount;
                itemList.Add(newItem);
                InventoryUI.Instance.AddItem(newItem);
                MessageUI.Instance.Show($"{item.nameOfItem} 被放入了背包");
            }
        }
        else
        {
            // 非堆叠物品直接添加
            ItemSO newItem = Instantiate(item);
            newItem.amount = 1;
            itemList.Add(newItem);
            InventoryUI.Instance.AddItem(newItem);
            MessageUI.Instance.Show($"{item.nameOfItem} 被放入了背包");
        }
    }

    // 查找可堆叠的物品
    private ItemSO FindStackableItem(ItemSO targetItem)
    {
        foreach (ItemSO item in itemList)
        {
            if (item.CanStackWith(targetItem) && item.amount < item.maxStackSize)
            {
                return item;
            }
        }
        return null;
    }



    // 移除指定数量的物品
    public void RemoveItem(ItemSO itemSO, int amountToRemove = 1)
    {
        if (itemSO.IsStackable())
        {
            // 堆叠物品：减少数量
            itemSO.amount -= amountToRemove;

            if (itemSO.amount <= 0)
            {
                // 数量为0或负数时完全移除
                itemList.Remove(itemSO);
                MessageUI.Instance.Show($"{itemSO.nameOfItem} 已从背包移除");
            }
            else
            {
                MessageUI.Instance.Show($"{itemSO.nameOfItem} 数量减少至 {itemSO.amount}");
            }
        }
        else
        {
            // 非堆叠物品：直接移除
            itemList.Remove(itemSO);
            MessageUI.Instance.Show($"{itemSO.nameOfItem} 已从背包移除");
        }

        InventoryUI.Instance.UpdateInventoryUI();
    }


    // 检查物品数量
    public int GetItemCount(string itemName)
    {
        int totalCount = 0;
        foreach (ItemSO item in itemList)
        {
            if (item.nameOfItem == itemName)
            {
                totalCount += item.amount;
            }
        }
        return totalCount;
    }

    // 不带提示消息的物品增加（已存在）
    public void ReAddItem(ItemSO item)
    {
        if (item.IsStackable())
        {
            // 查找背包中是否已有相同物品
            ItemSO existingItem = FindStackableItem(item);

            if (existingItem != null)
            {
                // 可以堆叠，增加数量
                int remainingSpace = existingItem.maxStackSize - existingItem.amount;
                int amountToAdd = Mathf.Min(item.amount, remainingSpace);

                existingItem.amount += amountToAdd;
               

                // 如果还有剩余物品，递归添加
                if (item.amount > amountToAdd)
                {
                    item.amount -= amountToAdd;
                    ReAddItem(item); // 递归处理剩余物品
                }

                InventoryUI.Instance.UpdateItemAmountDisplay(existingItem);
            }
            else
            {
                // 没有找到可堆叠的物品，添加新物品
                ItemSO newItem = Instantiate(item); // 创建副本以避免修改原始SO
                newItem.amount = item.amount;
                itemList.Add(newItem);
                InventoryUI.Instance.AddItem(newItem);
                
            }
        }
        else
        {
            // 非堆叠物品直接添加
            ItemSO newItem = Instantiate(item);
            newItem.amount = 1;
            itemList.Add(newItem);
            InventoryUI.Instance.AddItem(newItem);
            
        }
    }

    public bool HasItem(ItemSO targetItem)
    {
        return itemList.Contains(targetItem);
    }

    // 新增：检查是否有指定数量的物品
    public bool HasEnoughItems(ItemSO targetItem, int requiredAmount)
    {
        return GetItemCount(targetItem.nameOfItem) >= requiredAmount;
    }

    public void ClearInventory()
    {
        if (itemList != null)
        {
            itemList.Clear();
            Debug.Log("库存已清空");
        }
    }

    // 添加获取所有物品堆叠数据的方法（用于存档）
    public List<InventoryItemData> GetAllItemStacks()
    {
        List<InventoryItemData> stacks = new List<InventoryItemData>();

        foreach (ItemSO item in itemList)
        {
            if (item != null)
            {
                stacks.Add(new InventoryItemData(item.nameOfItem, item.amount, item.maxStackSize));
            }
        }

        return stacks;
    }
}