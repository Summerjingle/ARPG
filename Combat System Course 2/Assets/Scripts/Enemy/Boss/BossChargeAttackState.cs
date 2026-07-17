using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossChargeAttackState : State<BossController>
{
    [Header("Attack Data")]
    public AttackData chargeAttackData;

    [Header("Charge Config")]
    [Tooltip("最大冲刺距离（米）")]
    public float dashDistance = 8f;
    [Tooltip("冲刺速度（米/秒）")]
    public float dashSpeed = 15f;
    [Tooltip("障碍物检测层级")]
    public LayerMask obstacleLayer = ~0;
    [Tooltip("前方射线起始高度（避免打到地面）")]
    public float rayStartY = 1f;
    [Tooltip("射线检测距离余量（提前停下避免穿模）")]
    public float rayPadding = 0.5f;
    [Tooltip("开始冲刺的动画归一化时间（0~1），到达前只转向追踪不位移")]
    [Range(0f, 1f)]
    public float dashStartTime = 0f;
    [Tooltip("结束冲刺的动画归一化时间（0~1），超过后停止位移，等动画播完")]
    [Range(0f, 1f)]
    public float dashEndTime = 1f;

    private bool isCharging;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);

        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
            owner.agent.updatePosition = false;
        }

        owner.anim?.SetFloat("Speed", 0);
        owner.fighter.InCounter = false;
        isCharging = false;
    }

    public override void Execute()
    {
        if (isCharging) return;
        if (owner.playerTarget == null) return;
        if (chargeAttackData == null)
        {
            Debug.LogError("[BossChargeAttack] chargeAttackData 未设置！");
            owner.ChangeState(owner.chaseState);
            return;
        }

        owner.lastRangedAttackTime = Time.time;

        owner.fighter.OverrideAttacks(new List<AttackData> { chargeAttackData });
        StartCoroutine(DoChargeAttack());
    }

    private Vector3 GetDirectionToPlayer()
    {
        if (owner.playerTarget == null) return owner.transform.forward;
        Vector3 dir = owner.playerTarget.position - owner.transform.position;
        dir.y = 0;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : owner.transform.forward;
    }

    private IEnumerator DoChargeAttack()
    {
        isCharging = true;

        owner.anim.applyRootMotion = false;

        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        owner.fighter.TryToAttack(target);

        int layer = owner.fighter.AttackAnimLayer;

        // --- 等待动画真正开始播放 ---
        int oldHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            int curHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (curHash != oldHash && curHash != 0)
                break;
            yield return null;
        }

        if (owner.fighter.Attackstate == AttackStates.Idle)
        {
            // 动画未能播放，退出
            isCharging = false;
            yield break;
        }

        // --- 冲刺阶段 ---
        Vector3 dashDirection = GetDirectionToPlayer();
        float dashTimer = 0f;
        float maxDashTime = dashDistance / dashSpeed;
        bool impactStarted = false;

        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            float normalizedTime = owner.anim.GetCurrentAnimatorStateInfo(layer).normalizedTime;

            // 超过冲刺结束时间：停止位移，交给收尾循环等动画播完
            if (normalizedTime >= dashEndTime)
            {
                Debug.Log($"[BossChargeAttack] Reached dashEndTime: {dashEndTime:F2}");
                break;
            }

            // Impact 前：追踪玩家方向
            if (!impactStarted && normalizedTime < chargeAttackData.ImpactStartTime)
            {
                owner.FacePlayer();
                dashDirection = GetDirectionToPlayer();
            }
            else if (!impactStarted)
            {
                // Impact 开始：锁定方向，不再追踪
                impactStarted = true;
            }

            // 未到冲刺开始时间：只追踪不位移
            if (normalizedTime < dashStartTime)
            {
                yield return null;
                continue;
            }

            // 计算本帧移动量
            float moveDist = dashSpeed * Time.deltaTime;

            // 前方障碍物检测（防穿模）
            Vector3 rayOrigin = owner.transform.position + Vector3.up * rayStartY;
            Debug.DrawRay(rayOrigin, dashDirection * (moveDist + rayPadding), Color.red, 0.1f);
            if (Physics.Raycast(rayOrigin, dashDirection, out RaycastHit hit, moveDist + rayPadding, obstacleLayer))
            {
                float safeDist = hit.distance - rayPadding;
                if (safeDist > 0f)
                    owner.transform.position += dashDirection * safeDist;
                Debug.Log($"[BossChargeAttack] Hit wall at {hit.distance:F2}m, stopped");
                break;
            }

            // 向前移动
            owner.transform.position += dashDirection * moveDist;
            dashTimer += Time.deltaTime;

            // 达到最大冲刺距离
            if (dashTimer >= maxDashTime)
            {
                Debug.Log($"[BossChargeAttack] Reached max dash distance: {dashDistance}m");
                break;
            }

            yield return null;
        }

        // --- 收尾：等待动画播完 ---
        while (owner.fighter.Attackstate != AttackStates.Idle)
            yield return null;

        isCharging = false;
        owner.ChangeState(owner.GetNextStateAfterAttack());
    }

    public override void Exit()
    {
        StopAllCoroutines();
        isCharging = false;
        owner.fighter.DisableHitboxes();
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.Warp(owner.transform.position);  // 同步位置，防止回弹
            owner.agent.isStopped = false;
            owner.agent.updatePosition = true;
            owner.agent.ResetPath();
        }
    }
}
