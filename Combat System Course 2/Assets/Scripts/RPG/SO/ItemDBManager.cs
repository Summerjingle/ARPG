using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemDBManager : MonoBehaviour
{
    public static ItemDBManager Instance {  get; private set; }
    public ItemDBSO itemDB;
    private void Start()
    {
        if (Instance!=null && Instance!=this)
        {
            Destroy(this.gameObject);return;
        }
        Instance = this;
    }

    
    public ItemSO GetRandomItem()
    {/*不过滤掉落*/

        int randomIndex=  Random.Range(0,itemDB.itemList.Count);
        return itemDB.itemList[randomIndex];
    }

   
    public ItemSO GetRandomDropAllowedItem()
    { /*过滤掉落*/

        var allowedItems = itemDB.itemList.Where(item => item.canDrop).ToList();

        if (allowedItems.Count > 0)
        {
            int randomIndex = Random.Range(0, allowedItems.Count);
            return allowedItems[randomIndex];
        }
        else
        {
            Debug.LogWarning("没有可掉落的物品！请检查 ItemSO 的 canDrop 设置。");
            return null;
        }
    }
}
