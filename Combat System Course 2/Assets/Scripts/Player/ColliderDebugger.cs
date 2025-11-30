using UnityEngine;

public class ColliderDebugger : MonoBehaviour
{
    void Start()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true); // 包括禁用的
        Debug.Log($"玩家身上总共有 {colliders.Length} 个 Collider：");
        foreach (var col in colliders)
        {
            Debug.Log($"Collider: {col.GetType().Name}, GameObject: {col.gameObject.name}, Enabled: {col.enabled}, IsTrigger: {col.isTrigger}");
        }
    }
}
