using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] Transform followTarget;
    [SerializeField] float distance = 5;
    [SerializeField] float minVerticalAngle = -20f;
    [SerializeField] float maxVerticalAngle = 45;
    [SerializeField] Vector2 framingOffset;

    [Header("Collision Settings")]
    [SerializeField] LayerMask collisionMask = -1;
    [SerializeField] float collisionOffset = 0.3f;
    [SerializeField] float minCameraDistance = 0.5f;
    [SerializeField] float cameraRadius = 0.2f;
    [SerializeField] int collisionCheckFrequency = 3; // 每3帧检测一次

    [Header("Anti-Jitter Settings")]
    [SerializeField] float distanceChangeThreshold = 0.1f; // 距离变化阈值
    [SerializeField] float stableTimeThreshold = 0.2f; // 稳定时间阈值

    private float rotationY;
    private float rotationX;
    private float currentDistance;
    private Vector3[] clipPoints = new Vector3[5];
    private int frameCount;
    private float lastCheckTime;
    private Vector3 lastAdjustedPosition;

    // 防抖动变量声明
    private float lastStableDistance;
    private float distanceStableTimer;
    private bool isDistanceLocked = false;



    private void Awake()
    {
        
        UIStateManager.OnUIActiveStateChanged += HandleUIActiveStateChanged;
    }

    private void Start()
    {
        currentDistance = distance;
        lastStableDistance = distance; // 初始化
        distanceStableTimer = 0f; // 初始化
        PrecalculateClipPoints();
    }

    private void PrecalculateClipPoints()
    {
        if (Camera.main == null) return;

        float z = Camera.main.nearClipPlane;
        float fovRad = Camera.main.fieldOfView * Mathf.Deg2Rad;
        float aspect = Camera.main.aspect;

        float halfHeight = Mathf.Tan(fovRad / 2f) * z;
        float halfWidth = halfHeight * aspect;

        clipPoints = new Vector3[5]
        {
            new Vector3(0, 0, z),           // 中心
            new Vector3(-halfWidth, -halfHeight, z),
            new Vector3(halfWidth, -halfHeight, z),
            new Vector3(halfWidth, halfHeight, z),
            new Vector3(-halfWidth, halfHeight, z)
        };
    }

    private void Update()
    {
        if (UIStateManager.IsAnyUIActive) return;

        rotationX += Input.GetAxis("Mouse Y");
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        rotationY += Input.GetAxis("Mouse X");
    }

    private void LateUpdate()
    {

        if (followTarget == null) return;

        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 focusPosition = followTarget.position + new Vector3(framingOffset.x, framingOffset.y);

        // 减少碰撞检测频率
        frameCount++;
        bool shouldCheckCollision = frameCount % collisionCheckFrequency == 0 ||
                                  Vector3.Distance(focusPosition, lastAdjustedPosition) > 0.5f;

        Vector3 adjustedPosition;
        if (shouldCheckCollision)
        {
            adjustedPosition = CheckCameraCollision(focusPosition, targetRotation);
            lastAdjustedPosition = adjustedPosition;
            lastCheckTime = Time.time;
        }
        else
        {
            // 使用上次检测结果进行插值
            float t = (Time.time - lastCheckTime) / (Time.deltaTime * collisionCheckFrequency);
            adjustedPosition = Vector3.Lerp(
                lastAdjustedPosition,
                focusPosition - targetRotation * new Vector3(0, 0, currentDistance),
                t);
        }

        transform.position = adjustedPosition;
        transform.rotation = targetRotation;
    }

    private Vector3 CheckCameraCollision(Vector3 focusPosition, Quaternion rotation)
    {
        Vector3 desiredPosition = focusPosition - rotation * new Vector3(0, 0, distance);
        float nearestDistance = distance;
        bool collisionDetected = false;

        // 使用更稳定的碰撞检测方法
        Vector3[] checkDirections = new Vector3[]
        {
            rotation * Vector3.forward * -1, // 主方向
            rotation * (Vector3.forward * -1 + Vector3.up * 0.3f), // 稍微向上偏移
            rotation * (Vector3.forward * -1 + Vector3.down * 0.3f) // 稍微向下偏移
        };

        foreach (Vector3 direction in checkDirections)
        {
            if (Physics.SphereCast(
                focusPosition,
                cameraRadius,
                direction.normalized,
                out RaycastHit hit,
                distance + collisionOffset,
                collisionMask))
            {
                float hitDistance = Mathf.Max(hit.distance - collisionOffset, minCameraDistance);
                if (hitDistance < nearestDistance)
                {
                    nearestDistance = hitDistance;
                    collisionDetected = true;
                }
            }
        }

        float targetDistance = collisionDetected ? nearestDistance : distance;

        // 防抖动处理
        if (Mathf.Abs(targetDistance - lastStableDistance) < distanceChangeThreshold)
        {
            distanceStableTimer += Time.deltaTime;
            if (distanceStableTimer >= stableTimeThreshold && !isDistanceLocked)
            {
                isDistanceLocked = true;
                currentDistance = lastStableDistance;
            }
        }
        else
        {
            distanceStableTimer = 0f;
            isDistanceLocked = false;
            lastStableDistance = targetDistance;
        }

        if (!isDistanceLocked)
        {
            // 自适应平滑过渡
            float distanceDiff = Mathf.Abs(currentDistance - targetDistance);
            float smoothSpeed = distanceDiff > 0.5f ? 8f : 4f;

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * smoothSpeed);
            lastStableDistance = currentDistance;
        }

        return focusPosition - rotation * new Vector3(0, 0, currentDistance);
    }

    private void HandleUIActiveStateChanged(bool isActive)
    {
        // 可以在这里添加相机特定的UI响应逻辑
        // 例如：UI打开时停止相机旋转等
        Debug.Log($"CameraController: UI状态变为 {(isActive ? "活跃" : "非活跃")}");
    }

   

    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);

    

    private void OnDestroy()
    {
        UIStateManager.OnUIActiveStateChanged -= HandleUIActiveStateChanged;
     
    }
}