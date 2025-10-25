using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image iconImage;
    private ItemSO itemSO;
    public void  InitItem(ItemSO itemSO)
    {
        iconImage.sprite = itemSO.icon;
        this.itemSO = itemSO; 
    }
    public void OnClick()
    {
         InventoryUI.Instance.OnItemClick(itemSO,this);
    }
}
