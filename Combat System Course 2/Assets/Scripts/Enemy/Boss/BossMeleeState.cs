using UnityEngine;

public class BossMeleeState : State<BossController>
{
    private float attackTimer;
    private float totalAttackDuration = 2.0f;

    // 时间节点
    private float windupEndTime = 0.183f;   // 前摇结束
    private float trackingEndTime = 0.7f;   // 锁定结束（触发突进）
    private float strikeTime = 0.9f;        // 命中帧

    private bool hasDealtDamage;

    // 三段状态
    private enum AttackPhase
    {
        Windup,
        Tracking,
        Strike
    }

    private AttackPhase phase;

    // 锁定目标
    private Transform lockedTarget;

    // =========================
    // ⭐ 突进（替代瞬移）
    // =========================
    private bool isBurstMoving;
    private Vector3 burstStartPos;
    private Vector3 burstTargetPos;
    private float burstTimer;
    private float burstDuration = 0.15f;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);

        attackTimer = 0f;
        hasDealtDamage = false;

        phase = AttackPhase.Windup;

        lockedTarget = owner.playerTarget;

        isBurstMoving = false;

        owner.agent.isStopped = true;
        owner.agent.velocity = Vector3.zero;
        owner.agent.updateRotation = false;

        owner.PlayAnim("MeleeAttack", 0.1f, 1);
        owner.SetAnimFloat("Speed", 0f);
    }

    public override void Execute()
    {
        // =========================
        // ⭐ 突进优先执行
        // =========================
        if (isBurstMoving)
        {
            burstTimer += Time.deltaTime;

            float t = burstTimer / burstDuration;

            // ease-out 曲线（前快后慢）
            float curve = 1 - Mathf.Pow(1 - t, 3);

            owner.transform.position = Vector3.Lerp(burstStartPos, burstTargetPos, curve);

            // 锁朝向
            Vector3 dir = (burstTargetPos - burstStartPos).normalized;
            if (dir != Vector3.zero)
                owner.transform.forward = dir;

            if (t >= 1f)
            {
                isBurstMoving = false;
            }

            return;
        }

        attackTimer += Time.deltaTime;

        switch (phase)
        {
            // =========================
            // ① 前摇
            // =========================
            case AttackPhase.Windup:
            {
                LookAtPlayerFast(30f);

                if (attackTimer >= windupEndTime)
                {
                    phase = AttackPhase.Tracking;

                    owner.agent.isStopped = false;

                    Debug.Log("<color=yellow>[Melee] 进入锁定阶段</color>");
                }
                break;
            }

            // =========================
            // ② 锁定追踪
            // =========================
            case AttackPhase.Tracking:
            {
                if (lockedTarget == null) return;

                Vector3 dir = (lockedTarget.position - owner.transform.position);
                dir.y = 0;

                owner.agent.SetDestination(lockedTarget.position);
                owner.SetAnimFloat("Speed", 1f);

                LookAtPlayerFast(20f);

                if (attackTimer >= trackingEndTime)
                {
                    phase = AttackPhase.Strike;

                    owner.agent.isStopped = true;
                    owner.agent.velocity = Vector3.zero;
                    owner.SetAnimFloat("Speed", 0f);

                    float distance = dir.magnitude;

                    // ⭐ 触发突进（替代瞬移）
                    if (distance > owner.attackRange * 0.7f)
                    {
                        Vector3 targetPos = lockedTarget.position - dir.normalized * (owner.attackRange * 0.6f);
                        StartBurstMove(targetPos);

                        Debug.Log("<color=orange>[Melee] 突进补位</color>");
                    }
                }

                break;
            }

            // =========================
            // ③ 落刀
            // =========================
            case AttackPhase.Strike:
            {
                if (!hasDealtDamage && attackTimer >= strikeTime)
                {
                    PerformGuaranteedHit();
                    hasDealtDamage = true;
                }

                if (attackTimer >= totalAttackDuration)
                {
                    owner.ChangeState(owner.chaseState);
                }

                break;
            }
        }
    }

    // =========================
    // 突进逻辑
    // =========================
    private void StartBurstMove(Vector3 targetPos)
    {
        isBurstMoving = true;
        burstTimer = 0f;

        burstStartPos = owner.transform.position;
        burstTargetPos = targetPos;

        owner.agent.isStopped = true;

        // 可选：更快的移动动画
        owner.SetAnimFloat("Speed", 2f);
    }

    // =========================
    // 朝向
    // =========================
    private void LookAtPlayerFast(float speed)
    {
        if (lockedTarget == null) return;

        Vector3 dir = (lockedTarget.position - owner.transform.position).normalized;
        dir.y = 0;

        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            owner.transform.rotation = Quaternion.Slerp(
                owner.transform.rotation,
                targetRot,
                Time.deltaTime * speed
            );
        }
    }

    // =========================
    // 必中判定
    // =========================
    private void PerformGuaranteedHit()
    {
        if (lockedTarget == null) return;

        float distance = Vector3.Distance(owner.transform.position, lockedTarget.position);

        if (distance <= owner.attackRange + 1.0f)
        {
            var player = lockedTarget.GetComponent<PlayerController>();

            if (player != null)
            {
               
            }
        }
    }

    public override void Exit()
    {
        owner.agent.updateRotation = true;
        owner.agent.isStopped = false;

        owner.SetAnimFloat("Speed", 0f);
    }
}