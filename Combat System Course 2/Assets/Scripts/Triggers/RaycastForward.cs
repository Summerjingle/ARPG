using UnityEngine;

public class RaycastForward : MonoBehaviour
{
    [Header("射线参数")]
    [SerializeField] private float rayDistance = 10f;      // 射线长度
    [SerializeField] private LayerMask hitLayers = -1;     // -1表示检测所有层

    void Update()
    {
        // 从角色位置出发，沿正前方打射线
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // 画一条可视化射线（Scene视图可见）
        Debug.DrawRay(ray.origin, ray.direction * rayDistance, Color.red);

        if (Physics.Raycast(ray, out hit, rayDistance, hitLayers))
        {
            // 命中物体，输出名字
            Debug.Log($"打到物体：{hit.collider.gameObject.name}");
        }
    }
}