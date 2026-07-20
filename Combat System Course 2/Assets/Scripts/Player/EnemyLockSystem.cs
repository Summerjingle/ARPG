using Cinemachine;
using UnityEngine;

public class EnemyLockSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] float noticeRadius = 14f;
    [SerializeField] float noticeAngle = 60f;
    [SerializeField] LayerMask enemyLayer;

    [SerializeField] Transform enemyTarget_Locator;

    [Header("UI Indicator")]
    [SerializeField] RectTransform lockUICanvas;
    [SerializeField] RectTransform lockOnIcon;
    [SerializeField] float uiScaleFactor = 0.1f;

    [Header("Scale")]
    [SerializeField] float lockScale = 0.8f;
    [SerializeField] float scaleLerpDuration = 0.15f;

    public CinemachineVirtualCamera followCam;
    public CinemachineVirtualCamera lockCam;

    public Transform currentTarget { get; private set; }
    public bool IsLocked { get; private set; }

    private Transform cam;
    private PlayerController player;
    private Animator animator;
    private float yOffset = 1f;
    private Vector3 locatorVelocity;
    private Vector3 dirVelocity;
    private Transform currentLockOnPoint;
    private Coroutine scaleCoroutine;

    void OnEnable()
    {
        InputManager.Instance.OnLock += ToggleLock;
    }

    void OnDisable()
    {
        InputManager.Instance.OnLock -= ToggleLock;
    }

    void Start()
    {
        cam = Camera.main.transform;
        player = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!IsLocked) return;

        if (!TargetStillValid())
        {
            Unlock();
            return;
        }

        UpdateUI();
        RotatePlayer();
    }

    void ToggleLock()
    {
        if (IsLocked)
            Unlock();
        else
            LockOn();
    }

    void LockOn()
    {
        Transform target = ScanForTargets();
        if (target == null) return;

        currentTarget = target;
        IsLocked = true;

        // 缓存敌人身上的 lockOnPoint 挂点
        var ec = target.GetComponentInParent<EnemyController>();
        var bc = target.GetComponentInParent<BossController>();
        if (ec != null && ec.lockOnPoint != null)
            currentLockOnPoint = ec.lockOnPoint;
        else if (bc != null && bc.lockOnPoint != null)
            currentLockOnPoint = bc.lockOnPoint;
        else
            currentLockOnPoint = null; // TODO: 给所有小怪配 lockOnPoint！为 null 时相机回退锁骨骼碰撞体，会跟着动画摆

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(true);

        // 相机锁定用稳定的 lockOnPoint（根节点子物体），不能用骨骼碰撞体，
        // 否则 HandleLockOnCamera 每帧算 yaw 会跟着骨骼动画摆动
        player.cameraController.LockOnTarget(currentLockOnPoint != null ? currentLockOnPoint : target);
        player.ResetMovementBase();

        followCam.Priority = 10;
        lockCam.Priority = 20;

        player.isLockedOn = true;
        animator.SetBool("IsLocked", true);
        StartScaleTo(lockScale);
    }

    public void Unlock()
    {
        IsLocked = false;
        currentTarget = null;
        currentLockOnPoint = null;

        player.LockRotation = false;
        player.isLockedOn = false;
        player.lockedTargetDir = Vector3.zero;
        player.cameraController.UnlockCamera();
        animator.SetBool("IsLocked", false);
        StartScaleTo(1.25f);

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(false);

        followCam.Priority = 20;
        lockCam.Priority = 10;
    }

    void StartScaleTo(float targetScale)
    {
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(targetScale));
    }

    System.Collections.IEnumerator ScaleTo(float target)
    {
        if (lockOnIcon == null) yield break;

        Vector3 start = lockOnIcon.localScale;
        Vector3 end = Vector3.one * target;
        float elapsed = 0f;

        while (elapsed < scaleLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleLerpDuration;
            lockOnIcon.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }

        lockOnIcon.localScale = end;
    }

    Transform ScanForTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, noticeRadius, enemyLayer);
        if (hits.Length == 0) {
            Debug.Log("视角内没有捕捉到敌人");
            return null;}

        Transform best = null;
        float bestAngle = noticeAngle;

        foreach (var h in hits)
        {
            Vector3 dir = h.transform.position - cam.position;
            dir.y = 0;
            float ang = Vector3.Angle(cam.forward, dir);

            if (ang < bestAngle)
            {
                bestAngle = ang;
                best = h.transform;
            }
        }

        if (best == null) return null;

        CapsuleCollider col = best.GetComponent<CapsuleCollider>();
        if (col)
            yOffset = (col.height * best.localScale.y) * 0.66f;

        return best;
    }

    Vector3 GetEnemyLockPosition()
    {
        if (currentLockOnPoint != null)
            return currentLockOnPoint.position;
        return currentTarget.position + Vector3.up * yOffset;
    }

    void UpdateUI()
    {
        if (currentTarget == null || lockOnIcon == null || lockUICanvas == null) return;

        Vector3 worldPos = GetEnemyLockPosition();
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lockUICanvas,
            new Vector2(screenPos.x, screenPos.y),
            null,
            out Vector2 localPoint);
        lockOnIcon.anchoredPosition = localPoint;
    }

    void RotatePlayer()
    {
        if (currentTarget == null) return;

        Vector3 lockPos = GetEnemyLockPosition();
        Vector3 dir = lockPos - player.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            player.lockedTargetDir = Vector3.SmoothDamp(
                player.lockedTargetDir, dir.normalized, ref dirVelocity, 0.08f);

        if (enemyTarget_Locator != null)
            enemyTarget_Locator.position = Vector3.SmoothDamp(
                enemyTarget_Locator.position, lockPos, ref locatorVelocity, 0.08f);
    }

    bool TargetStillValid()
    {
        if (!currentTarget) return false;

        var enemyController = currentTarget.GetComponent<EnemyController>();
        if (enemyController != null &&
            (enemyController.Fighter.HealthSystem.IsDead || !enemyController.gameObject.activeInHierarchy))
            return false;

        float dis = Vector3.Distance(transform.position, currentTarget.position);
        return dis <= noticeRadius * 1.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }
}
