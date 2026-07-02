using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu()]
public class ItemDBSO : ScriptableObject
{
    public List<ItemSO> itemList;

#if UNITY_EDITOR
    [ContextMenu("自动填充所有 ItemSO")]
    private void AutoFill()
    {
        itemList = new List<ItemSO>();
        string[] guids = AssetDatabase.FindAssets("t:ItemSO");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (item != null)
                itemList.Add(item);
        }
        EditorUtility.SetDirty(this);
        Debug.Log($"ItemDB 已填充 {itemList.Count} 个物品");
    }
#endif
}
