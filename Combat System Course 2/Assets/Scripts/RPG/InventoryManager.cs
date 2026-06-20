using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public List<ItemSO> itemList;
    public event System.Action<ItemSO> OnItemRemoved;

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
            
            ItemSO existingItem = FindStackableItem(item);

            if (existingItem != null)
            {
                // ���Զѵ�����������
                int remainingSpace = existingItem.maxStackSize - existingItem.amount;
                int amountToAdd = Mathf.Min(item.amount, remainingSpace);

                existingItem.amount += amountToAdd;
                

                // �������ʣ����Ʒ���ݹ�����
                if (item.amount > amountToAdd)
                {
                    item.amount -= amountToAdd;
                    AddItem(item); // �ݹ鴦��ʣ����Ʒ
                }

                InventoryUI.Instance.UpdateItemAmountDisplay(existingItem);
            }
            else
            {
                // û���ҵ��ɶѵ�����Ʒ����������Ʒ
                ItemSO newItem = Instantiate(item); // ���������Ա����޸�ԭʼSO
                newItem.amount = item.amount;
                itemList.Add(newItem);
                InventoryUI.Instance.AddItem(newItem);
                
            }
        }
        else
        {
            // �Ƕѵ���Ʒֱ������
            ItemSO newItem = Instantiate(item);
            newItem.amount = 1;
            itemList.Add(newItem);
            InventoryUI.Instance.AddItem(newItem);
            
        }
    }

    // ���ҿɶѵ�����Ʒ
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


    //�ӱ������Ƴ�ָ����Ʒ
    public void RemoveItem(ItemSO targetItem, int amountToRemove = 1, bool updateUI = true)
    {
        // �ڱ������ҵ���Ӧ����Ʒ��ͨ������ƥ�䣩
        ItemSO inventoryItem = null;
        foreach (ItemSO item in itemList)
        {
            if (item != null && item.nameOfItem == targetItem.nameOfItem)
            {
                inventoryItem = item;
                break;
            }
        }

        if (inventoryItem == null)
        {
            Debug.LogWarning($"�����Ƴ������ڵ���Ʒ: {targetItem.nameOfItem}");
            return;
        }

        // ԭ�е��Ƴ��߼����ֲ���
        bool removed = false;

        if (inventoryItem.IsStackable())
        {
            inventoryItem.amount -= amountToRemove;
            if (inventoryItem.amount <= 0)
            {
                itemList.Remove(inventoryItem);
                removed = true;
                MessageUI.Instance.Show($"{inventoryItem.nameOfItem}");
            }
            else
            {
                MessageUI.Instance.Show($"{inventoryItem.nameOfItem} {inventoryItem.amount}");
            }
        }
        else
        {
            itemList.Remove(inventoryItem);
            removed = true;
            MessageUI.Instance.Show($"{inventoryItem.nameOfItem} �Ѵӱ����Ƴ�");
        }

        if (removed)
            OnItemRemoved?.Invoke(inventoryItem);

        if (updateUI)
            InventoryUI.Instance.UpdateInventoryUI();
    }


    // �����Ʒ����
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

    // ������ʾ��Ϣ����Ʒ���ӣ��Ѵ��ڣ�
    public void ReAddItem(ItemSO item)
    {
        if (item.IsStackable())
        {
            // ���ұ������Ƿ�������ͬ��Ʒ
            ItemSO existingItem = FindStackableItem(item);

            if (existingItem != null)
            {
                // ���Զѵ�����������
                int remainingSpace = existingItem.maxStackSize - existingItem.amount;
                int amountToAdd = Mathf.Min(item.amount, remainingSpace);

                existingItem.amount += amountToAdd;
               

                // �������ʣ����Ʒ���ݹ�����
                if (item.amount > amountToAdd)
                {
                    item.amount -= amountToAdd;
                    ReAddItem(item); // �ݹ鴦��ʣ����Ʒ
                }

                InventoryUI.Instance.UpdateItemAmountDisplay(existingItem);
            }
            else
            {
                // û���ҵ��ɶѵ�����Ʒ����������Ʒ
                ItemSO newItem = Instantiate(item); // ���������Ա����޸�ԭʼSO
                newItem.amount = item.amount;
                itemList.Add(newItem);
                InventoryUI.Instance.AddItem(newItem);
                
            }
        }
        else
        {
            // �Ƕѵ���Ʒֱ������
            ItemSO newItem = Instantiate(item);
            newItem.amount = 1;
            itemList.Add(newItem);
            InventoryUI.Instance.AddItem(newItem);
            
        }
    }

    public bool HasItem(ItemSO targetItem)
    {
        if (targetItem == null) return false;

        
        foreach (ItemSO item in itemList)
        {
            if (item != null && item.nameOfItem == targetItem.nameOfItem)
            {
                return true;
            }
        }
        return false;
    }


    // ����������Ƿ���ָ����������Ʒ
    public bool HasEnoughItems(ItemSO targetItem, int requiredAmount)
    {
        return GetItemCount(targetItem.nameOfItem) >= requiredAmount;
    }

    public void ClearInventory()
    {
        if (itemList != null)
        {
            itemList.Clear();
            Debug.Log("������������");
        }

        // �ؼ���ͬʱ���� UI�������ʾ����Ʒ
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UpdateInventoryUI();
            Debug.Log("���UI�Ѹ���");
        }
    }

    // ���ӻ�ȡ������Ʒ�ѵ����ݵķ��������ڴ浵��
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