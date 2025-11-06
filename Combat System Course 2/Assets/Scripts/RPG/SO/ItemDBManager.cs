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

}
