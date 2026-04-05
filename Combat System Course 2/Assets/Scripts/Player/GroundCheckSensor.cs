using UnityEngine;

public class GroundCheckSensor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayOffset = 0.3f;    // 射线偏离中心的距离
    [SerializeField] private float probeDistance = 0.8f; // 探测深度（略大于台阶高度）
    [SerializeField] private float originOffset = 0.1f; // 射线起点上移，防止没入地面

    public struct SnapInfo
    {
        public bool shouldSnap;      // 是否建议执行吸附
        public float distanceToGround; // 距离地面的精确距离
        public Vector3 groundNormal;   // 地面法线（可传给 IK）
    }

    public SnapInfo GetSnapInfo()
    {
        SnapInfo info = new SnapInfo();
        
        // 定义 5 个探测点：中心、前、后、左、右
        Vector3[] rayOrigins = new Vector3[]
        {
            transform.position + Vector3.up * originOffset,
            transform.position + Vector3.up * originOffset + transform.forward * rayOffset,
            transform.position + Vector3.up * originOffset - transform.forward * rayOffset,
            transform.position + Vector3.up * originOffset + transform.right * rayOffset,
            transform.position + Vector3.up * originOffset - transform.right * rayOffset
        };

        float minDistance = float.MaxValue;
        int hitCount = 0;
        Vector3 averageNormal = Vector3.up;

        foreach (Vector3 origin in rayOrigins)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, probeDistance, groundLayer))
            {
                hitCount++;
                float dist = hit.distance - originOffset;
                if (dist < minDistance) minDistance = dist;
                averageNormal += hit.normal;
            }
        }

        // 判定逻辑：只要有一根射线扫到地，且距离在合理范围内（如 0.6m）
        info.shouldSnap = hitCount > 0 && minDistance > 0.01f && minDistance < 0.6f;
        info.distanceToGround = (hitCount > 0) ? minDistance : 0f;
        info.groundNormal = (averageNormal / (hitCount + 1)).normalized;

        return info;
    }

    // 在编辑器里画出射线，方便调试
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3[] origins = { transform.position, transform.position + transform.forward * rayOffset, /*...*/ };
        // 此处可以循环画出 5 条线
    }
}