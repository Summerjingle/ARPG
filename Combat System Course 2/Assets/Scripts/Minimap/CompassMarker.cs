using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompassMarker : MonoBehaviour
{

    public Transform bindGO;//绑定的物体

    public RectTransform compassBar;
    public RectTransform markerRect;

    public float barHalfWidth=500f;//纬度条长度/2
    public float maxAngle=90f;//最大角度，超出则停靠在两侧

    [SerializeField]private float maxScale=1.1f;
    [SerializeField]private float minScale=0.5f;
    [SerializeField]private float maxDistance=20f;
    [SerializeField]private float minDistance=2f;


    public Transform camTransform;
    private Vector3 originalScale;
    
    // ========== 调试变量 ==========
    private float debugDistance = 0f;
    private float debugAngle = 0f;
    private float debugXPos = 0f;
    private float debugScale = 0f;

    void Start()
    {
        camTransform=Camera.main.transform;
        if (markerRect != null)
            originalScale = markerRect.localScale;
        if (!bindGO)
            Debug.LogError("指示器未绑定物品");
        if(!compassBar)
            Debug.LogError("纬度条未赋值");
        if(!markerRect)
            Debug.LogError("指示器未赋值");
    }

    void Update()
    {
        if (bindGO == null || camTransform == null)
            return;
        
        UpdateMarkerPosition();
        UpdateMarkerScale();
    }

   private void UpdateMarkerPosition()
    {
        // 1. 获取方向并消除Y轴影响（投影到XZ平面）
        Vector3 camForward = camTransform.forward;
        camForward.y = 0; // 忽略摄像机的俯仰角
        
        Vector3 toDoor = bindGO.position - camTransform.position;
        toDoor.y = 0;     // 忽略目标与摄像机的高度差

        // 防止向量长度为0（比如摄像机完全垂直朝上/朝下时）导致计算报错
        if (camForward.sqrMagnitude < 0.001f || toDoor.sqrMagnitude < 0.001f)
            return;

        // 2. 计算纯水平面上的夹角
        float angle = Vector3.SignedAngle(camForward, toDoor, Vector3.up);
        debugAngle = angle;  // 记录角度

        // 3. 将角度转换为UI上的位置
        float xPos = (angle / maxAngle) * barHalfWidth;
        debugXPos = xPos;  // 记录位置

        // 4. 限制位置范围，防止超出罗盘边界
        xPos = Mathf.Clamp(xPos, -barHalfWidth, barHalfWidth);

        // 5. 应用位置
        markerRect.anchoredPosition = new Vector2(xPos, markerRect.anchoredPosition.y);

        // 当门在身后时淡化（由于去除了Y轴，这里的angle更准确了）
        UnityEngine.UI.Image markerImage = markerRect.GetComponent<UnityEngine.UI.Image>();
        if (markerImage != null)
        {
            Color color = markerImage.color;
            // 超过最大角度意味着在罗盘视野外或身后
            color.a = Mathf.Abs(angle) > maxAngle ? 0.5f : 1f; 
            markerImage.color = color;
        }
    }

    private void UpdateMarkerScale()
    {
        // 计算玩家与门的距离
        float distance = Vector3.Distance(camTransform.position, bindGO.position);
        debugDistance = distance;  // 记录距离
        
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
        // 使用 SmoothStep 让 t 的变化呈现 S 型曲线
        t = t * t * (3f - 2f * t); 
        float scaleValue = Mathf.Lerp(maxScale, minScale, t);
        debugScale = scaleValue;  // 记录缩放值
        
        // 应用缩放
        markerRect.localScale = originalScale * scaleValue;
    }
    
    // ========== 调试面板 ==========
    void OnGUI()
    {
        // 获取当前实际缩放
        float currentScale = markerRect != null ? markerRect.localScale.x / originalScale.x : 0f;
        
        string debugText = 
            "=== 罗盘调试面板 ===\n" +
            "\n--- 距离信息 ---\n" +
            $"目标距离: {debugDistance:F2} 米\n" +
            $"距离范围: {minDistance}米 → {maxDistance}米\n" +
            $"映射t值: {Mathf.InverseLerp(minDistance, maxDistance, debugDistance):F3}\n" +
            $"\n--- 缩放信息 ---\n" +
            $"缩放范围: {maxScale} → {minScale}\n" +
            $"计算缩放: {debugScale:F3}\n" +
            $"实际缩放: {currentScale:F3}\n" +
            $"\n--- 角度信息 ---\n" +
            $"水平角度: {debugAngle:F1}°\n" +
            $"角度范围: -{maxAngle}° → +{maxAngle}°\n" +
            $"计算位置: {debugXPos:F1}\n" +
            $"位置限制: -{barHalfWidth} → +{barHalfWidth}";
        
        // 添加状态提示
        if (Mathf.Abs(debugAngle) > maxAngle)
            debugText += "\n\n⚠️ 门在视野外（淡化中）";
        else
            debugText += "\n\n✓ 门在视野内";
        
        GUI.Box(new Rect(10, 10, 400, 380), debugText);
        
        // 添加操作提示
        GUI.Label(new Rect(10, 400, 400, 60), 
            "提示：\n" +
            "- 观察距离和缩放的变化\n" +
            "- 角度超过90°时会淡化");
    }
}