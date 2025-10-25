using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public ItemSO itemSO;
    private void Start()
    {
        Debug.Log($"物品生成: {name}, Tag: {tag}, Layer: {LayerMask.LayerToName(gameObject.layer)}");

        // 检查碰撞器
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"碰撞器: {col.GetType().Name}, IsTrigger: {col.isTrigger}");
        }
        else
        {
            Debug.LogWarning("没有找到碰撞器！如果是装备防具请忽略");
        }
    }
    protected virtual void Interact()
    {

    }
}
