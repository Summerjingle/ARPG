using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
   public static InventoryManager Instance {  get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); return;
        }
        Instance = this;
    }
    public List<ItemSO> itemList;
    
    private void Start()
    {
        
    }
    public void AddItem(ItemSO item)
    {
        itemList.Add(item);
        InventoryUI.Instance.AddItem(item);
        MessageUI.Instance.Show( item.nameOfItem+" 被放入了背包" );
    }
    public void ReAddItem(ItemSO item)//不带提示消息的物品增加
    {
        itemList.Add(item);
        InventoryUI.Instance.AddItem(item);
    }
    public void RemoveItem(ItemSO itemSO) 
    { 
        itemList.Remove(itemSO);
        InventoryUI.Instance.UpdateInventoryUI();
    }

    public bool HasItem(ItemSO targetItem)
    {
        //检查背包内是否有指定道具（任务道具）
        return itemList.Contains(targetItem);
    }
}
