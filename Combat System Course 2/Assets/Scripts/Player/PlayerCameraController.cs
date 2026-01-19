using TMPro.Examples;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    
    [Header("Camera Settings")]
    public GameObject cinemachineCameraTarget;

    [Tooltip("Max vertical angle up")]
    public float topClamp = 70f;

    [Tooltip("Max vertical angle down")]
    public float bottomClamp = -30f;

    [Tooltip("Additional degrees for fine-tuning when locked")]
    public float cameraAngleOverride = 0f;

    [Tooltip("Lock camera in place (e.g., during lock-on)")]
    public bool lockCameraPosition = false;

    private float yaw;
    private float pitch;

    private Vector2 lookInput; 

    private Transform lockedTarget;
    private bool isLockingOn = false;

    private void Start()
    {
        if (cinemachineCameraTarget != null)
        {
            yaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
            pitch = 0f;
            cinemachineCameraTarget.transform.rotation = transform.rotation;
        }
    }

    private void LateUpdate()
    {
        
        if (lockCameraPosition || cinemachineCameraTarget == null) return;

        if (isLockingOn && lockedTarget != null)
        {
            HandleLockOnCamera();
        }
        else
        {
            HandleFreeCamera();
        }
    }
    public void SetLookInput(Vector2 look)
    {
        lookInput = look;
    }
    // 自由移动摄像机旋转
    private void HandleFreeCamera()
    {
        float mouseX = lookInput.x;
        float mouseY = lookInput.y;

        if (mouseX != 0f || mouseY != 0f)
        {
            float deltaTimeMultiplier = 1f;
            yaw += mouseX * deltaTimeMultiplier;
            pitch += mouseY * deltaTimeMultiplier;
        }

        pitch = Mathf.Clamp(pitch, bottomClamp, topClamp);

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);
    }

    // 锁敌时摄像机瞬时对齐敌人
    private void HandleLockOnCamera()
    {
        Vector3 dir = lockedTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        yaw = Quaternion.LookRotation(dir).eulerAngles.y;
        pitch = 10f; // 可自定义垂直角度

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);
    }

    // 锁敌目标设置
    public void LockOnTarget(Transform target)
    {
        lockedTarget = target;
        isLockingOn = target != null;
        lockCameraPosition = target != null;
    }

    public void UnlockCamera()
    {
        isLockingOn = false;
        lockedTarget = null;
        lockCameraPosition = false;
    }

    // 外部获取摄像机水平方向旋转（用于移动方向）
    public Quaternion GetPlanarRotation()
    {
        if (cinemachineCameraTarget != null)
        {
            Vector3 forward = cinemachineCameraTarget.transform.forward;
            forward.y = 0f;
            return Quaternion.LookRotation(forward.normalized);
        }
        return Quaternion.identity;
    }

    // 外部获取锁定方向（用于移动方向）
    public Vector3 GetLockedDirection()
    {
        if (isLockingOn && lockedTarget != null)
        {
            Vector3 dir = lockedTarget.position - transform.position;
            dir.y = 0f;
            return dir.normalized;
        }
        return Vector3.zero;
    }
    private void OnEnable()
    {
        UIStateManager.OnUIActiveStateChanged += HandleUIStateChanged;
    }
    private void OnDisable()
    {
        UIStateManager.OnUIActiveStateChanged -= HandleUIStateChanged;
    }

    private void HandleUIStateChanged(bool isUIActive)
    {
        // UI 打开 → 摄像机锁住
        lockCameraPosition = isUIActive;

        // UI 打开时，阻止输入干扰数值
        if (isUIActive)
        {
            Input.ResetInputAxes();
        }
    }
    public void SetCameraHeight(float height, bool smooth = true)
    {
        if (cinemachineCameraTarget == null) return;

        Vector3 pos = cinemachineCameraTarget.transform.localPosition;

        if (smooth)
        {
            // 平滑过渡
            pos.y = Mathf.Lerp(pos.y, height, Time.deltaTime * 10f);
        }
        else
        {
            pos.y = height;
        }

        cinemachineCameraTarget.transform.localPosition = pos;
    }
    public float Yaw => yaw;
    public float Pitch => pitch;
}
