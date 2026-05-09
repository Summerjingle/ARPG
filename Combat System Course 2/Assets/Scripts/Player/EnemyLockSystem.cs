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
    [SerializeField] Transform lockUICanvas;
    [SerializeField] float uiScaleFactor = 0.1f;

    public CinemachineVirtualCamera followCam;
    public CinemachineVirtualCamera lockCam;

    public Transform currentTarget { get; private set; }
    public bool IsLocked { get; private set; }

    private Transform cam;
    private PlayerController player;
    private Animator animator;
    private float yOffset = 1f;

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

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(true);

        player.cameraController.LockOnTarget(target);
        player.ResetMovementBase();

        followCam.Priority = 10;
        lockCam.Priority = 20;

        player.isLockedOn = true;
        animator.SetBool("IsLocked", true);
    }

    public void Unlock()
    {
        IsLocked = false;
        currentTarget = null;

        player.LockRotation = false;
        player.isLockedOn = false;
        player.lockedTargetDir = Vector3.zero;
        player.cameraController.UnlockCamera();
        animator.SetBool("IsLocked", false);

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(false);

        followCam.Priority = 20;
        lockCam.Priority = 10;
    }

    Transform ScanForTargets()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, noticeRadius, enemyLayer);
        if (hits.Length == 0) return null;

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

    void UpdateUI()
    {
        if (currentTarget == null) return;

        Vector3 toCamera = (cam.position - currentTarget.position).normalized;
        Vector3 pos = currentTarget.position + Vector3.up * yOffset + toCamera * 0.5f;
        lockUICanvas.position = pos;

        float dis = Vector3.Distance(cam.position, pos);
        lockUICanvas.localScale = Vector3.one * (dis * uiScaleFactor);
    }

    void RotatePlayer()
    {
        if (currentTarget == null) return;

        Vector3 dir = currentTarget.position - player.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            player.lockedTargetDir = dir.normalized;

        Vector3 targetPos = currentTarget.position + Vector3.up * yOffset;
        if (enemyTarget_Locator != null)
            enemyTarget_Locator.position = targetPos;
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
