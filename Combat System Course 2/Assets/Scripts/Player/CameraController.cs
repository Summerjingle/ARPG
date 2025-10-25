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

    private float rotationY;
    private float rotationX;
    private float currentDistance;
    private Vector3[] clipPoints = new Vector3[5];
    private int frameCount;
    private float lastCheckTime;
    private Vector3 lastAdjustedPosition;

    public static bool IsAnyUIActive { get; private set; }
    private static int uiActiveCount = 0;

    private void Awake()
    {
        RegisterToManagers();
    }
    private void Start()
    {
        currentDistance = distance;
        UpdateCursorState();
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
        if (IsAnyUIActive) return;

        rotationX += Input.GetAxis("Mouse Y");
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        rotationY += Input.GetAxis("Mouse X");
    }

    private void LateUpdate()
    {
        UpdateCursorState();

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

        // 只检测中心点和两个对角点（减少到3个检测点）
        for (int i = 0; i < 3; i++)
        {
            int index = i == 2 ? 4 : i; // 检测中心(0)、左下(1)、右上(4)
            Vector3 clipPoint = transform.TransformPoint(clipPoints[index]);
            Vector3 rayDirection = clipPoint - focusPosition;

            if (Physics.SphereCast(
                focusPosition,
                cameraRadius,
                rayDirection.normalized,
                out RaycastHit hit,
                rayDirection.magnitude + collisionOffset,
                collisionMask))
            {
                float hitDistance = hit.distance - collisionOffset;
                if (hitDistance < nearestDistance)
                {
                    nearestDistance = hitDistance;
                    collisionDetected = true;
                }
            }
        }

        // 使用更平滑的距离过渡
        float targetDistance = collisionDetected ? Mathf.Max(nearestDistance, minCameraDistance) : distance;
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * 5f);

        return focusPosition - rotation * new Vector3(0, 0, currentDistance);
    }

    // 保留原有UI状态管理方法...
    public void SetUIActive(bool uiActive)
    {
        uiActiveCount += uiActive ? 1 : -1;
        uiActiveCount = Mathf.Max(0, uiActiveCount);
        IsAnyUIActive = uiActiveCount > 0;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        Cursor.visible = IsAnyUIActive;
        Cursor.lockState = IsAnyUIActive ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
    private void RegisterToManagers()
    {
        // 注册到InventoryUI
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.RegisterCameraController(this);
        }

        // 注册到DialogueManager
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.RegisterCameraController(this);
        }

        // 注册到其他需要CameraController的管理器...
    }

    private void OnDestroy()
    {
        // 取消注册
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.UnregisterCameraController();
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.UnregisterCameraController();
        }
    }
}