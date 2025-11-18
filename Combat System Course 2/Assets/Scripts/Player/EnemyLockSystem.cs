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

    private bool isLocked = false;
    private float yOffset = 1f;

    

    void Start()
    {
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

        

        // 玩家瞬间朝向目标
        player.LookAtTargetInstant(t);

        // 切换到锁定镜头
        cinemachineAnimator.Play("TargetCamera");

        Debug.Log("Lock-On: " + t.name);
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
      
        if (lockUICanvas) lockUICanvas.gameObject.SetActive(false);
        player.UnlockCamera();

       

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
        

        // 计算目标位置并更新 enemyTarget_Locator
        Vector3 targetPos = currentTarget.position + Vector3.up * yOffset;

        if (enemyTarget_Locator != null)
            enemyTarget_Locator.position = targetPos;

      
        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0;
        Quaternion rot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rot,
            player.RotationSpeed * 5 * Time.deltaTime  // 放大5倍
        );


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
