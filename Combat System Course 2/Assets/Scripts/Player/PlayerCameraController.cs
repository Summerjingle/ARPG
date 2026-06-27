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
    private float lockYawVelocity;
    private float lockPitchVelocity;

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
        if (cinemachineCameraTarget == null) return;

        // Lock-on: smooth follow to avoid jitter from root motion
        if (isLockingOn && lockedTarget != null)
        {
            HandleLockOnCamera();
        }
        else if (!lockCameraPosition)
        {
            HandleFreeCamera();
        }
    }
    public void SetLookInput(Vector2 look)
    {
        lookInput = look;
    }

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

    private float lockCameraTargetZ;

    // Smooth follow during lock-on; avoids jitter from per-frame position changes
    private void HandleLockOnCamera()
    {
        Vector3 dir = lockedTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y;
        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref lockYawVelocity, 0.08f);
        pitch = Mathf.SmoothDamp(pitch, 10f, ref lockPitchVelocity, 0.08f);

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);
    }

    // Called externally (e.g. from PlayerController.LateUpdate) to push camera target
    // away from enemy when close, preventing Cinemachine from going overhead
    public void UpdateLockCameraDistance()
    {
        if (!isLockingOn || lockedTarget == null) return;

        Vector3 toEnemy = lockedTarget.position - transform.position;
        toEnemy.y = 0f;
        float dist = toEnemy.magnitude;

        float minDist = 3f;
        float pushback = 0.3f;
        float desiredZ = dist < minDist ? -(minDist - dist) * pushback : 0f;

        lockCameraTargetZ = Mathf.Lerp(lockCameraTargetZ, desiredZ, Time.deltaTime * 8f);

        Vector3 localPos = cinemachineCameraTarget.transform.localPosition;
        localPos.z = lockCameraTargetZ;
        cinemachineCameraTarget.transform.localPosition = localPos;
    }

    // Snap to enemy direction on first lock, then smooth follow
    public void LockOnTarget(Transform target)
    {
        bool wasLockingOn = isLockingOn;
        lockedTarget = target;
        isLockingOn = target != null;
        lockCameraPosition = target != null;

        if (!wasLockingOn && target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                yaw = Quaternion.LookRotation(dir).eulerAngles.y;
                pitch = 10f;
            }
            lockYawVelocity = 0f;
            lockPitchVelocity = 0f;
        }
    }

    public void UnlockCamera()
    {
        if (cinemachineCameraTarget != null)
        {
            yaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
            pitch = cinemachineCameraTarget.transform.rotation.eulerAngles.x;
        }

        isLockingOn = false;
        lockedTarget = null;
        lockCameraPosition = false;
    }

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
        lockCameraPosition = isUIActive;

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
