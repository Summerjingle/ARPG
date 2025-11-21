using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLockSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] float noticeRadius = 14f;
    [SerializeField] float noticeAngle = 60f;
    [SerializeField] LayerMask enemyLayer;

    [Header("Lock Camera Animator")]
    [SerializeField] Animator cinemachineAnimator;
    [SerializeField] Transform enemyTarget_Locator;

    [Header("UI Indicator")]
    [SerializeField] Transform lockUICanvas;
    [SerializeField] float uiScaleFactor = 0.1f;

    public Transform currentTarget { get; private set; }
    private Transform cam;
    private PlayerController player;
    private Animator animator;

    public bool IsLocked { get; private set; }
    private float yOffset = 1f;

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
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!IsLocked)
                TryLockTarget();
            else
                Unlock();
        }

        if (IsLocked)
        {
            if (!TargetStillValid())
            {
                Unlock();
                return;
            }

            UpdateUI();
            RotatePlayer();
        }
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

        if (!best) return null;

        CapsuleCollider col = best.GetComponent<CapsuleCollider>();
        if (col)
        {
            yOffset = (col.height * best.localScale.y) * 0.66f;
        }

        return best;
    }

    void TryLockTarget()
    {
        Transform target = ScanForTargets();
        if (target == null) return;

        currentTarget = target;
        IsLocked = true;
        player.LockRotation = true;

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(true);

        player.LockCameraPosition = true;
        animator.SetBool("combatMode", true); 

        player.LookAtTargetInstant(target);
        player.ResetMovementBase();

        if (cinemachineAnimator != null)
            cinemachineAnimator.Play("TargetCamera");

        player.isLockedOn = true;
    }

    public void Unlock()
    {
        IsLocked = false;
        currentTarget = null;
        player.LockRotation = false;

        Vector3 dir = transform.forward;
        dir.y = 0;
        player.targetRotation = Quaternion.LookRotation(dir);
        animator.SetBool("combatMode", false); 

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(false);

        player.isLockedOn = false;
        player.lockedTargetDir = Vector3.zero;
        player.UnlockCamera();
        player.targetRotation = player.transform.rotation;

        if (cinemachineAnimator != null)
            cinemachineAnimator.Play("FollowCamera");
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
        {
            return false;
        }

        float dis = Vector3.Distance(transform.position, currentTarget.position);
        return dis <= noticeRadius * 1.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }
}