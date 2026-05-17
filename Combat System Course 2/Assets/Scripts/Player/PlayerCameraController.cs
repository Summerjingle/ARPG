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
    // �����ƶ��������ת
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

    // ����ʱ�����˲ʱ�������
    private void HandleLockOnCamera()
    {
        Vector3 dir = lockedTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        yaw = Quaternion.LookRotation(dir).eulerAngles.y;
        pitch = 10f; // ���Զ��崹ֱ�Ƕ�

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);
    }

    // ����Ŀ������
    public void LockOnTarget(Transform target)
    {
        lockedTarget = target;
        isLockingOn = target != null;
        lockCameraPosition = target != null;
    }

    public void UnlockCamera()
    {
        // 同步 yaw/pitch 到当前相机朝向，避免解锁瞬间大幅度摆动
        if (cinemachineCameraTarget != null)
        {
            yaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
            pitch = cinemachineCameraTarget.transform.rotation.eulerAngles.x;
        }

        isLockingOn = false;
        lockedTarget = null;
        lockCameraPosition = false;
    }

    // �ⲿ��ȡ�����ˮƽ������ת�������ƶ�����
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

    // �ⲿ��ȡ�������������ƶ�����
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
        // UI �� �� �������ס
        lockCameraPosition = isUIActive;

        // UI ��ʱ����ֹ���������ֵ
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
            // ƽ������
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
