using UnityEngine;
using Cinemachine;

public class CinemachineCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 45f;
    [SerializeField] private Vector2 framingOffset;

    [Header("Input Settings")]
    [SerializeField] private float mouseSensitivity = 2f;

    private float rotationX;
    private float rotationY;
    private CinemachineTransposer transposer;

    private void Start()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineVirtualCamera>();

        transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();

        // 初始化虚拟摄像机设置
        virtualCamera.Follow = followTarget;

        if (transposer != null)
        {
            transposer.m_FollowOffset = new Vector3(0, 0, -distance);
        }
    }

    private void Update()
    {
        if (UIStateManager.IsAnyUIActive) return;

        // 和原来一样的输入处理
        rotationX += Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;

        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        if (followTarget == null || transposer == null) return;

        // 计算目标旋转（和原来一样）
        Quaternion targetRotation = Quaternion.Euler(rotationX, rotationY, 0);

        // 应用偏移和距离
        Vector3 followOffset = targetRotation * new Vector3(framingOffset.x, framingOffset.y, -distance);
        transposer.m_FollowOffset = followOffset;

       
    }

    // 提供和原来一样的PlanarRotation属性
    public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);
}