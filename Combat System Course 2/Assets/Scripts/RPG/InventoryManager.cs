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
    public void RemoveItem(ItemSO itemSO) 
    { 
        itemList.Remove(itemSO);
        
    }
}
