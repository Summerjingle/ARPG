using System.Collections;
using Cinemachine;
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

    [Header("Lock-on Pitch")]
    [Tooltip("锁定敌人时的俯仰角（正值=俯视）")]
    [SerializeField] float lockOnPitch = 10f;

    private float yaw;
    private float pitch;

    private Vector2 lookInput;

    private Transform lockedTarget;
    private bool isLockingOn = false;
    private float lockYawVelocity;
    private float lockPitchVelocity;

    // 摄像机震动
    private Vector3 shakeOffset;
    private Coroutine shakeCoroutine;

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

        const float sensitivity = 1f;
        yaw   += mouseX * sensitivity;
        pitch += mouseY * sensitivity;

        pitch = Mathf.Clamp(pitch, bottomClamp, topClamp);

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);
    }

    [Header("Lock-on Distance")]
    [Tooltip("玩家与敌人距离小于此值时开始缩短相机距离")]
    public float lockOnMinDistance = 5f;

    [Tooltip("距离小于 minDistance 时，相机缩到的最短距离")]
    public float lockOnMinCameraDistance = 1.5f;

    [Tooltip("锁定状态下默认相机距离")]
    public float lockOnDefaultCameraDistance = 4f;

    // Runtime cached from EnemyLockSystem.lockCam — pipeline body, not a MonoBehaviour
    private Cinemachine3rdPersonFollow lockCamBody;

    // Smooth follow during lock-on; avoids jitter from per-frame position changes.
    // CameraDistance is shrunk separately by UpdateLockCameraDistance at close range.
    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int _lastDebugFrame;

    private void HandleLockOnCamera()
    {
        Vector3 dir = lockedTarget.position - transform.position;
        dir.y = 0f;

        float targetYaw = Quaternion.LookRotation(dir).eulerAngles.y;
        float prevYaw = yaw;
        yaw   = Mathf.SmoothDampAngle(yaw, targetYaw, ref lockYawVelocity, 0.12f);
        pitch = Mathf.SmoothDamp(pitch, lockOnPitch, ref lockPitchVelocity, 0.12f);

        cinemachineCameraTarget.transform.rotation =
            Quaternion.Euler(pitch + cameraAngleOverride, yaw, 0f);

    }

    // Called from PlayerController.LateUpdate — adjusts lock cam distance when close to enemy
    public void UpdateLockCameraDistance()
    {
        if (!isLockingOn || lockedTarget == null) return;

        // Lazy cache the lock cam's 3rdPersonFollow body
        if (lockCamBody == null)
        {
            var lockSystem = GetComponent<EnemyLockSystem>();
            if (lockSystem != null && lockSystem.lockCam != null)
                lockCamBody = lockSystem.lockCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        }
        if (lockCamBody == null) return;

        // 读取敌人身上的近身冻结距离
        var ec = lockedTarget.GetComponentInParent<EnemyController>();
        var bc = lockedTarget.GetComponentInParent<BossController>();
        float minFreeze = lockOnMinCameraDistance;
        if (ec != null) minFreeze = ec.lockOnMinFreezeDistance;
        else if (bc != null) minFreeze = bc.lockOnMinFreezeDistance;

        Vector3 toEnemy = lockedTarget.position - transform.position;
        toEnemy.y = 0f;
        float dist = toEnemy.magnitude;

        float desiredDistance;
        if (dist < lockOnMinDistance)
        {
            float t = Mathf.Clamp01(dist / lockOnMinDistance);
            desiredDistance = Mathf.Lerp(minFreeze, lockOnDefaultCameraDistance, t);
        }
        else
        {
            desiredDistance = lockOnDefaultCameraDistance;
        }

        lockCamBody.CameraDistance = Mathf.Lerp(
            lockCamBody.CameraDistance,
            desiredDistance,
            1f - Mathf.Exp(-8f * Time.deltaTime));
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
            yaw   = cinemachineCameraTarget.transform.rotation.eulerAngles.y;
            pitch = cinemachineCameraTarget.transform.rotation.eulerAngles.x;
        }

        // Reset lock cam distance to default
        if (lockCamBody != null)
            lockCamBody.CameraDistance = lockOnDefaultCameraDistance;

        isLockingOn   = false;
        lockedTarget  = null;
        lockCameraPosition = false;
        lockYawVelocity     = 0f;
        lockPitchVelocity   = 0f;
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
    /// <summary>
    /// 每帧从 PlayerController 调用，让 CCT 世界位置跟随玩家（替代父子关系）。
    /// XZ 瞬时跟随，Y 轴可选平滑（配合蹲下过渡）。
    /// </summary>
    public void SyncCameraTargetPosition(Vector3 playerPosition, float headOffsetY, bool smoothHeight = true)
    {
        if (cinemachineCameraTarget == null) return;

        Vector3 pos = cinemachineCameraTarget.transform.position;
        pos.x = playerPosition.x;
        pos.z = playerPosition.z;

        float targetY = playerPosition.y + headOffsetY;
        if (smoothHeight)
            pos.y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-12f * Time.deltaTime));
        else
            pos.y = targetY;

        // 叠加上摄像机震动偏移
        pos += shakeOffset;

        cinemachineCameraTarget.transform.position = pos;

       
    }

    public void SetCameraHeight(float height, bool smooth = true)
    {
        if (cinemachineCameraTarget == null) return;

        Vector3 pos = cinemachineCameraTarget.transform.position;

        if (smooth)
            pos.y = Mathf.Lerp(pos.y, height, 1f - Mathf.Exp(-10f * Time.deltaTime));
        else
            pos.y = height;

        cinemachineCameraTarget.transform.position = pos;
    }
    public float Yaw => yaw;
    public float Pitch => pitch;

    /// <summary>触发摄像机震动（由 AttackData 驱动）</summary>
    public void ShakeCamera(float intensity, float duration, float frequency)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(DoShake(intensity, duration, frequency));
    }

    private IEnumerator DoShake(float intensity, float duration, float frequency)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / duration);
            float currentIntensity = intensity * decay;

            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) * 2f - 1f) * currentIntensity;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) * 2f - 1f) * currentIntensity;

            shakeOffset = new Vector3(x, y, 0f);
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}
