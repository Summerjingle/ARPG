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
    [SerializeField] Animator cinemachineAnimator; // 控制摄像机切换的 Animator
    [SerializeField] Transform enemyTarget_Locator;

    [Header("UI Indicator")]
    [SerializeField] Transform lockUICanvas;
    [SerializeField] float uiScaleFactor = 0.1f;

    private Transform currentTarget;
    private Transform cam;
    private PlayerController player;
    private CombatController combatController;

    private bool isLocked = false;
    private float yOffset = 1f;
    private int frameCount = 0;


    void Start()
    {
     
        combatController = GetComponent<CombatController>();
        cam = Camera.main.transform;
        player = GetComponent<PlayerController>();
        

        if (lockUICanvas != null)
            lockUICanvas.gameObject.SetActive(false);

        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!isLocked) TryLockTarget();
            else Unlock();
        }

        if (isLocked)
        {
            if (!TargetStillValid())
            {
                Unlock();
                return;
            }

            UpdateUI();
            RotatePlayer();
        }
        frameCount++;
#if UNITY_EDITOR
        if (frameCount % 60 == 0)
        {
            Debug.Log($"第{frameCount}帧 -玩家没动的 位置: {player.transform.position}");
        }
#endif
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
        Transform t = ScanForTargets();
        if (!t) return;

        currentTarget = t;
        isLocked = true;
        player.LockRotation = true;

        if (lockUICanvas) lockUICanvas.gameObject.SetActive(true);

        // 禁止玩家鼠标控制摄像机
        player.LockCameraPosition = true;
        player.animator.SetBool("combatMode",true);
        
        player.LookAtTargetInstant(t);
        player.ResetMovementBase(); // 保持原来的逻辑

        // 切换到锁定镜头
        cinemachineAnimator.Play("TargetCamera");

        // 设置锁定状态
        player.isLockedOn = true;
    }

    void Unlock()
    {
        isLocked = false;
        currentTarget = null;
        player.LockRotation = false;

        // 设置玩家解锁后的默认朝向
        Vector3 dir = transform.forward; // 保持当前朝向
        dir.y = 0;
        player.targetRotation = Quaternion.LookRotation(dir);
        player.animator.SetBool("combatMode", false);
        if (lockUICanvas) lockUICanvas.gameObject.SetActive(false);
        player.isLockedOn = false;
        player.lockedTargetDir = Vector3.zero;
        player.UnlockCamera();
        player.targetRotation = player.transform.rotation;



        // 切换回跟随镜头
        cinemachineAnimator.Play("FollowCamera");

        Debug.Log("Unlock");
    }

    void UpdateUI()
    {
        Vector3 toCamera = (cam.position - currentTarget.position).normalized;
        Vector3 pos = currentTarget.position + Vector3.up * yOffset+ toCamera * 0.3f; 
        lockUICanvas.position = pos;

        float dis = Vector3.Distance(cam.position, pos);
        lockUICanvas.localScale = Vector3.one * (dis * uiScaleFactor);
    }

    void RotatePlayer()
    {
        if (currentTarget == null) return;

        // --- 1. 更新锁定方向，每帧动态计算 ---
        Vector3 dir = currentTarget.position - player.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            player.lockedTargetDir = dir.normalized;

        // --- 2. 更新敌人 UI/Locator，保持锁定显示 ---
        Vector3 targetPos = currentTarget.position + Vector3.up * yOffset;
        if (enemyTarget_Locator != null)
            enemyTarget_Locator.position = targetPos;
    }


    bool TargetStillValid()
    {
        if (!currentTarget) return false;

        float dis = Vector3.Distance(transform.position, currentTarget.position);

        return dis <= noticeRadius * 1.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, noticeRadius);
    }
}
