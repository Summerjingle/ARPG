using System.Collections;
using UnityEngine;

public class BossJumpAttackState : State<BossController>
{
    [Header("Attack Data")]
    public AttackData jumpAttackData;

    [Header("Jump Config")]
    [Tooltip("动画播到多少%时开始起跳")]
    public float jumpStartNormalizedTime = 0.1f;
    [Tooltip("动画播到多少%时到顶，冻结动画开始悬停")]
    public float riseEndNormalizedTime = 0.5f;
    [Tooltip("跳跃高度（m）")]
    public float jumpHeight = 3f;
    [Tooltip("上升速度（m/s）")]
    public float riseSpeed = 8f;
    [Tooltip("天上悬停时间（秒）")]
    public float hoverDuration = 1.5f;
    [Tooltip("砸下速度（m/s）")]
    public float slamSpeed = 25f;
    [Tooltip("地面检测层级")]
    public LayerMask groundLayer = ~0;
    [Tooltip("地面检测射线起始偏移（避免打到自身）")]
    public float groundRayStartY = 0.5f;

    private bool isJumping;
    private float groundY;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);

        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = true;
            owner.agent.updatePosition = false; // 允许手动控制 Y 轴，不被 NavMesh 拉回地面
        }

        owner.anim?.SetFloat("Speed", 0);
        owner.fighter.InCounter = false;
        isJumping = false;
    }

    public override void Execute()
    {
        if (isJumping) return;
        if (owner.playerTarget == null) return;

        owner.lastRangedAttackTime = Time.time;

        owner.fighter.OverrideAttacks(new System.Collections.Generic.List<AttackData> { jumpAttackData });
        StartCoroutine(DoJumpAttack());
    }

    private IEnumerator DoJumpAttack()
    {
        isJumping = true;

        owner.anim.applyRootMotion = false;

        int layer = owner.fighter.AttackAnimLayer;
        int oldHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;

        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        owner.fighter.TryToAttack(target);

        Debug.Log($"[JumpAttack] START | jumpStart={jumpStartNormalizedTime:F2} | riseEnd={riseEndNormalizedTime:F2} | hoverDuration={hoverDuration:F2} | slamSpeed={slamSpeed:F1}");

        // 等动画真正开始播
        int waitFrames = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            int curHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (curHash != oldHash && curHash != 0)
                break;
            waitFrames++;
            yield return null;
        }

        if (owner.fighter.Attackstate == AttackStates.Idle) { isJumping = false; yield break; }

        Debug.Log($"[JumpAttack] Animation started | waitFrames={waitFrames}");

        groundY = owner.transform.position.y;
        float targetY = groundY + jumpHeight;

        // ===== 阶段1+2：WAIT + RISE =====
        int frameCount = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            var animState = owner.anim.GetCurrentAnimatorStateInfo(layer);
            float nt = animState.normalizedTime;

            if (frameCount == 0)
                Debug.Log($"[JumpAttack] Phase={(nt < jumpStartNormalizedTime ? "WAIT" : "RISE")} | nt={nt:F3}");

            if (nt < jumpStartNormalizedTime)
            {
                // WAIT：纯播动画
            }
            else if (nt < riseEndNormalizedTime)
            {
                // RISE：恒定速度上升，高度到顶则提前结束
                float newY = owner.transform.position.y + riseSpeed * Time.deltaTime;
                if (newY >= targetY)
                {
                    Vector3 pos = owner.transform.position;
                    pos.y = targetY;
                    owner.transform.position = pos;
                    break;
                }
                owner.transform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
                owner.FacePlayer();
            }
            else
            {
                break; // 动画时间到顶了
            }

            frameCount++;
            yield return null;
        }

        Debug.Log($"[JumpAttack] Rise complete | y={owner.transform.position.y:F3}");

        // ===== 阶段3：冻结动画，天上悬停，瞄准玩家 =====
        owner.anim.speed = 0f;

        float hoverTimer = 0f;
        while (hoverTimer < hoverDuration)
        {
            hoverTimer += Time.deltaTime;
            owner.FacePlayer();
            if (hoverTimer % 1f < Time.deltaTime) // 每秒打一次log
                Debug.Log($"[JumpAttack] HOVER | t={hoverTimer:F1}s | y={owner.transform.position.y:F3}");
            yield return null;
        }

        Debug.Log($"[JumpAttack] Hover done, slamming | target={owner.playerTarget.position}");

        // ===== 阶段4：朝玩家砸下 =====
        Vector3 slamTarget = owner.playerTarget.position;
        slamTarget.y = groundY;

        bool landed = false;
        while (!landed)
        {
            Vector3 currentPos = owner.transform.position;
            Vector3 dir = (slamTarget - currentPos).normalized;
            Vector3 move = dir * slamSpeed * Time.deltaTime;

            // 防止穿过目标
            if (move.sqrMagnitude >= (slamTarget - currentPos).sqrMagnitude)
            {
                owner.transform.position = slamTarget;
                landed = true;
                Debug.Log($"[JumpAttack] Reached slam target");
            }
            else
            {
                owner.transform.Translate(move, Space.World);
            }

            // 地面检测
            if (!landed)
            {
                Vector3 rayOrigin = owner.transform.position + Vector3.up * groundRayStartY;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayStartY + 0.3f, groundLayer))
                {
                    Vector3 pos = owner.transform.position;
                    pos.y = hit.point.y;
                    owner.transform.position = pos;
                    landed = true;
                    Debug.Log($"[JumpAttack] LANDED | groundY={hit.point.y:F3}");
                }
            }

            yield return null;
        }

        // ===== 落地，恢复动画继续播放 =====
        owner.anim.speed = 1f;
        Debug.Log($"[JumpAttack] Animation resumed");

        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            yield return null;
        }

        Debug.Log($"[JumpAttack] DONE | finalY={owner.transform.position.y:F3}");

        isJumping = false;
        owner.ChangeState(owner.GetNextStateAfterAttack());
    }

    public override void Exit()
    {
        StopAllCoroutines();
        isJumping = false;
        owner.anim.speed = 1f; // 防止动画被冻结时退出
        owner.fighter.DisableHitboxes();
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.Warp(owner.transform.position);  // 同步当前位置，防止回弹到起跳点
            owner.agent.isStopped = false;
            owner.agent.updatePosition = true;
            owner.agent.ResetPath();
        }
    }
}
