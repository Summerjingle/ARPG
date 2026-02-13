using UnityEngine;

public class PlayerGroundDetector : MonoBehaviour
{
    [Header("检测设置")]
    [SerializeField] private float debugInterval = 10f; // 检测间隔（秒）
    [SerializeField] private LayerMask groundLayer = ~0; // 默认检测所有层
    [SerializeField] private float rayDistance = 1.2f; // 射线距离

    [Header("角色引用")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator; // 可选，用于检测状态

    [Header("可视化")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private Color rayColor = Color.green;
    [SerializeField] private Color hitColor = Color.red;

    private float timer = 0f;
    private RaycastHit lastHitInfo;
    private GameObject lastGroundObject;
    private bool isGrounded;

    private void Awake()
    {
        // 如果未手动指定，尝试自动获取
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (characterController == null)
        {
            Debug.LogError("PlayerGroundDetector: 未找到CharacterController组件！");
            enabled = false;
        }
    }

    private void Update()
    {
        // 更新接地状态
        isGrounded = characterController.isGrounded;

        // 定时检测逻辑
        timer += Time.deltaTime;
        if (timer >= debugInterval && isGrounded)
        {
            timer = 0f;
            DetectGroundObject();
        }

        // 持续更新地面信息（可选）
        UpdateGroundInfo();
    }

    /// <summary>
    /// 检测玩家脚下的物体
    /// </summary>
    public void DetectGroundObject()
    {
        if (!isGrounded)
        {
            Debug.Log("玩家当前不在地面上");
            return;
        }

        // 计算射线起点（角色底部）
        Vector3 rayOrigin = transform.position;
        float controllerHeight = characterController.height;
        float skinWidth = characterController.skinWidth;
        rayOrigin.y = transform.position.y - controllerHeight / 2f + skinWidth;

        // 发射射线
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            lastHitInfo = hit;
            lastGroundObject = hit.collider.gameObject;

            // 输出信息
            LogGroundInfo(hit);

            // 可视化射线
            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, Vector3.down * hit.distance, rayColor, 2f);
                Debug.DrawRay(hit.point, hit.normal * 0.3f, hitColor, 2f);
            }
        }
        else
        {
            Debug.LogWarning("玩家在地面上，但未检测到脚下物体。可能站在边缘或斜坡上。");
            lastGroundObject = null;
        }
    }

    /// <summary>
    /// 持续更新地面信息（每帧调用）
    /// </summary>
    private void UpdateGroundInfo()
    {
        if (!isGrounded || !showDebugRays) return;

        // 显示持续的检测射线
        Vector3 rayOrigin = transform.position;
        float controllerHeight = characterController.height;
        float skinWidth = characterController.skinWidth;
        rayOrigin.y = transform.position.y - controllerHeight / 2f + skinWidth;

        // 绘制短暂的检测射线
        Debug.DrawRay(rayOrigin, Vector3.down * 0.1f, Color.yellow, 0.1f);
    }

    /// <summary>
    /// 输出地面信息到控制台
    /// </summary>
    private void LogGroundInfo(RaycastHit hit)
    {
        GameObject groundObj = hit.collider.gameObject;

        Debug.Log("=== 地面检测结果 ===");
        Debug.Log($"时间: {System.DateTime.Now:HH:mm:ss}");
        Debug.Log($"玩家位置: {transform.position}");
        Debug.Log($"玩家旋转: {transform.rotation.eulerAngles.y:F1}°");

        // 基本物体信息
        Debug.Log($"脚下物体: {groundObj.name}");
        Debug.Log($"物体标签: {groundObj.tag}");
        Debug.Log($"物体层级: {LayerMask.LayerToName(groundObj.layer)}");

        // 碰撞信息
        Debug.Log($"碰撞点: {hit.point}");
        Debug.Log($"法线角度: {Vector3.Angle(hit.normal, Vector3.up):F1}°");
        Debug.Log($"检测距离: {hit.distance:F3}m");

        // 材质信息
        Renderer renderer = groundObj.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Material mat = renderer.material;
            Debug.Log($"地面材质: {mat.name}");

            Texture mainTex = mat.mainTexture;
            if (mainTex != null)
            {
                Debug.Log($"主纹理: {mainTex.name}");
            }
        }

        // 特殊类型检测
        CheckSpecialGroundTypes(groundObj, hit);

        Debug.Log("==================");
    }

    /// <summary>
    /// 检测特殊地面类型
    /// </summary>
    private void CheckSpecialGroundTypes(GameObject groundObj, RaycastHit hit)
    {
        // 检测是否是地形
        Terrain terrain = groundObj.GetComponent<Terrain>();
        if (terrain != null)
        {
            Debug.Log("地面类型: 地形(Terrain)");

            // 获取地形高度图信息
            Vector3 terrainPos = hit.point - terrain.transform.position;
            Vector3 normalizedPos = new Vector3(
                terrainPos.x / terrain.terrainData.size.x,
                0,
                terrainPos.z / terrain.terrainData.size.z
            );

            // 获取地形纹理
            float[,,] alphamap = terrain.terrainData.GetAlphamaps(
                Mathf.FloorToInt(normalizedPos.x * terrain.terrainData.alphamapWidth),
                Mathf.FloorToInt(normalizedPos.z * terrain.terrainData.alphamapHeight),
                1, 1
            );

            Debug.Log($"地形高度: {terrain.SampleHeight(hit.point):F2}m");
        }

        // 检测是否是网格碰撞器
        MeshCollider meshCollider = groundObj.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            Debug.Log("碰撞器类型: MeshCollider");
        }

        // 检测是否是盒子碰撞器
        BoxCollider boxCollider = groundObj.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Debug.Log("碰撞器类型: BoxCollider");
        }

        // 检测是否是楼梯或斜坡
        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        if (slopeAngle > 10f && slopeAngle < 45f)
        {
            Debug.Log($"地面坡度: {slopeAngle:F1}° (斜坡)");
        }
        else if (slopeAngle >= 45f)
        {
            Debug.Log($"地面坡度: {slopeAngle:F1}° (陡坡/墙壁)");
        }
    }

    /// <summary>
    /// 手动触发地面检测
    /// </summary>
    public void ManualDetect()
    {
        DetectGroundObject();
    }

    /// <summary>
    /// 获取最后检测到的地面物体
    /// </summary>
    public GameObject GetLastGroundObject()
    {
        return lastGroundObject;
    }

    /// <summary>
    /// 获取最后的地面碰撞信息
    /// </summary>
    public RaycastHit GetLastHitInfo()
    {
        return lastHitInfo;
    }

    /// <summary>
    /// 判断是否站在特定类型的物体上
    /// </summary>
    public bool IsStandingOn(string tag)
    {
        return lastGroundObject != null && lastGroundObject.CompareTag(tag);
    }

    /// <summary>
    /// 判断是否站在特定图层的物体上
    /// </summary>
    public bool IsStandingOnLayer(int layer)
    {
        return lastGroundObject != null && lastGroundObject.layer == layer;
    }

    /// <summary>
    /// 在编辑器中可视化
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || characterController == null) return;

        if (isGrounded)
        {
            // 绘制检测范围
            Gizmos.color = Color.cyan;
            Vector3 rayOrigin = transform.position;
            float controllerHeight = characterController.height;
            float skinWidth = characterController.skinWidth;
            rayOrigin.y = transform.position.y - controllerHeight / 2f + skinWidth;

            Gizmos.DrawWireSphere(rayOrigin, 0.05f);
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayDistance);

            // 绘制最后检测到的碰撞点
            if (lastGroundObject != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(lastHitInfo.point, 0.05f);
                Gizmos.DrawLine(lastHitInfo.point, lastHitInfo.point + lastHitInfo.normal * 0.3f);
            }
        }
    }

    /// <summary>
    /// 调试用的快捷键
    /// </summary>
    private void OnGUI()
    {
#if UNITY_EDITOR
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.G)
        {
            ManualDetect();
        }
#endif
    }
}