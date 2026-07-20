using System.Collections;
using UnityEngine;

public class BossUltimateState : State<BossController>
{
    [Header("Attack Data")]
    public AttackData ultimateAttackData;
    [Tooltip("大招 VFX 覆盖，拖了就用这个，不拖则走 AttackData 里的默认 VFX")]
    public GameObject ultimateVfxPrefab;

    [Header("矫正跳跃")]
    [Tooltip("先跳到这个位置再开大，null则跳过矫正")]
    public Transform correctionJumpTarget;

    [Header("Charge Config")]
    [Tooltip("第一段蓄力卡住的 normalizedTime（0→1）")]
    [Range(0f, 1f)] public float pauseNormalizedTime0 = 0.3f;
    [Tooltip("第二段蓄力卡住的 normalizedTime（1→2），动画从 pause0 播到这儿")]
    [Range(0f, 1f)] public float pauseNormalizedTime1 = 0.5f;
    [Tooltip("第一段蓄力时长（秒），卡在 pause0")]
    public float chargeDuration0 = 1.5f;
    [Tooltip("第二段蓄力时长（秒），卡在 pause1")]
    public float chargeDuration1 = 1.5f;
    [Tooltip("累计受到该伤害量则打断进 stun")]
    public float interruptDamageThreshold = 300f;

    [Header("Charge VFX")]
    [Tooltip("蓄力期间生成的 VFX prefab")]
    public GameObject chargeVfxPrefab;
    [Tooltip("VFX 挂载的父物体，null = Boss 自身")]
    public Transform chargeVfxSpawnParent;
    [Tooltip("最终阶段激活的碰撞器 GameObject")]
    public GameObject ultimateColliderObject;

    [Header("Phase Switch (大招后随机切形态)")]
    [Tooltip("切形态动画的 Animator state 名（Agility/Strength 共用一个）")]
    public string phaseSwitchStateName;
    [Tooltip("该 state 所在的 Animator Layer")]
    public int phaseSwitchAnimLayer = 0;
    [Tooltip("CrossFade 过渡时间（秒）")]
    public float phaseSwitchCrossFade = 0.1f;
    [Tooltip("切到 Agility 时生成的 VFX prefab")]
    public GameObject agilityPhaseVfx;
    [Tooltip("切到 Strength 时生成的 VFX prefab")]
    public GameObject strengthPhaseVfx;
    [Tooltip("VFX 生成点，null = Boss 自身位置")]
    public Transform vfxSpawnPoint;
    [Tooltip("VFX 自动销毁时间（秒），<=0 不自动销毁")]
    public float vfxLifetime = 5f;
    [Tooltip("切换动画播到这个归一化时间时，把形态材质追加到 Boss 身上")]
    [Range(0f, 1f)] public float materialApplyNormalizedTime = 0.5f;
    [Tooltip("Agility 形态追加的材质")]
    public Material agilityPhaseMat;
    [Tooltip("Strength 形态追加的材质")]
    public Material strengthPhaseMat;
    [Tooltip("要追加形态材质的 Renderer（拖 Boss 身体，可多个）")]
    public Renderer[] phaseMatRenderers;

    private bool isCharging;
    private float chargeTimer;
    private float accumulatedDamage;
    private bool interrupted;
    private Material appliedPhaseMat;   // 当前挂在身上的形态材质，下次切换时先移除
    private GameObject chargeVfxInstance;

    public float AccumulatedDamage => accumulatedDamage;
    public bool IsCharging => isCharging;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        Debug.Log($"[Ultimate] ENTER | correctionJumpTarget={(correctionJumpTarget != null ? correctionJumpTarget.name : "null")} | hp%={owner.GetComponent<HealthSystem>()?.HealthPercent}");

        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;

        owner.anim?.SetFloat("Speed", 0);
        owner.fighter.InCounter = false;

        isCharging = false;
        chargeTimer = 0f;
        accumulatedDamage = 0f;
        interrupted = false;

        owner.pendingUltimate = false;

        // 确保碰撞器初始关闭
        if (ultimateColliderObject != null)
            ultimateColliderObject.SetActive(false);
    }

    public override void Execute()
    {
        if (isCharging) return;
        if (owner.playerTarget == null) return;

        owner.fighter.OverrideAttacks(new System.Collections.Generic.List<AttackData> { ultimateAttackData });
        StartCoroutine(DoUltimate());
    }

    private IEnumerator DoUltimate()
    {
        isCharging = true;
        Debug.Log($"[Ultimate] DoUltimate START | hasCorrectionJump={correctionJumpTarget != null}");

        // ===== 位置矫正 JumpAttack =====
        if (correctionJumpTarget != null)
        {
            Debug.Log($"[Ultimate] → Starting correction jump...");
            yield return DoCorrectionJump();
            if (owner == null) { Debug.LogError("[Ultimate] owner destroyed during correction jump!"); yield break; }
            Debug.Log($"[Ultimate] ← Correction jump returned. Waiting 1 frame for animator reset...");
            yield return null; // 等一帧，让 Animator 从 JumpAttack 退出
            Debug.Log($"[Ultimate] Animator state after wait: hash={owner.anim.GetCurrentAnimatorStateInfo(owner.fighter.AttackAnimLayer).shortNameHash}, nt={owner.anim.GetCurrentAnimatorStateInfo(owner.fighter.AttackAnimLayer).normalizedTime:F3}");
        }

        // ===== Ultimate 蓄力 =====
        Debug.Log($"[Ultimate] Preparing ultimate attack | attackData={ultimateAttackData?.name}, target={owner.playerTarget}");
        owner.anim.applyRootMotion = false;

        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        owner.fighter.AttackVfxOverride = ultimateVfxPrefab;
        Debug.Log($"[Ultimate] Calling TryToAttack | target={target} | fighter.AttackState before={owner.fighter.Attackstate}");
        owner.fighter.TryToAttack(target);
        Debug.Log($"[Ultimate] TryToAttack done | fighter.AttackState after={owner.fighter.Attackstate}");

        int layer = owner.fighter.AttackAnimLayer;

        // 等动画过渡完成
        int transitionFrames = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle && owner.anim.IsInTransition(layer))
        {
            transitionFrames++;
            yield return null;
        }
        Debug.Log($"[Ultimate] Transition complete | frames={transitionFrames} | AttackState={owner.fighter.Attackstate}");

        if (owner.fighter.Attackstate == AttackStates.Idle)
        {
            Debug.LogError($"[Ultimate] FAILED: AttackState is Idle after TryToAttack! Ultimate animation never started.");
            isCharging = false;
            yield break;
        }

        // ===== 阶段0：播到第一个卡点 (0→1) =====
        Debug.Log($"[Ultimate] Phase 0→1 | waiting for first pause point NT={pauseNormalizedTime0:F2}");
        yield return WaitForNormalizedTime(layer, pauseNormalizedTime0);

        if (owner.fighter.Attackstate == AttackStates.Idle)
        {
            Debug.LogError("[Ultimate] FAILED: AttackState became Idle before first pause point!");
            isCharging = false;
            yield break;
        }

        // 卡住，第一段蓄力
        owner.anim.speed = 0f;
        SpawnChargeVfx();
        Debug.Log($"[Ultimate] Charge 0→1 | duration={chargeDuration0}s | anim.speed=0");
        chargeTimer = 0f;
        while (chargeTimer < chargeDuration0)
        {
            chargeTimer += Time.unscaledDeltaTime;
            if (accumulatedDamage >= interruptDamageThreshold)
            {
                Debug.Log($"[Ultimate] INTERRUPTED during charge 0→1 | dmg={accumulatedDamage}");
                interrupted = true;
                break;
            }
            yield return null;
        }
        if (interrupted) { owner.anim.speed = 1f; isCharging = false; owner.fighter.AttackVfxOverride = null; DestroyChargeVfx(); owner.ChangeState(owner.stunnedState); yield break; }
        Debug.Log($"[Ultimate] Charge 0→1 complete");

        // ===== 阶段1→2：播放到第二个卡点 =====
        owner.anim.speed = 1f;
        Debug.Log($"[Ultimate] Phase 1→2 | playing to second pause point NT={pauseNormalizedTime1:F2}");
        yield return WaitForNormalizedTime(layer, pauseNormalizedTime1);

        if (owner.fighter.Attackstate == AttackStates.Idle)
        {
            Debug.LogError("[Ultimate] FAILED: AttackState became Idle before second pause point!");
            isCharging = false;
            DestroyChargeVfx();
            yield break;
        }

        // 卡住，第二段蓄力
        owner.anim.speed = 0f;
        Debug.Log($"[Ultimate] Charge 1→2 | duration={chargeDuration1}s | anim.speed=0");
        chargeTimer = 0f;
        while (chargeTimer < chargeDuration1)
        {
            chargeTimer += Time.unscaledDeltaTime;
            if (accumulatedDamage >= interruptDamageThreshold)
            {
                Debug.Log($"[Ultimate] INTERRUPTED during charge 1→2 | dmg={accumulatedDamage}");
                interrupted = true;
                break;
            }
            yield return null;
        }
        if (interrupted) { owner.anim.speed = 1f; isCharging = false; owner.fighter.AttackVfxOverride = null; DestroyChargeVfx(); owner.ChangeState(owner.stunnedState); yield break; }
        Debug.Log($"[Ultimate] Charge 1→2 complete");

        // ===== 阶段2→3：播放剩下的，碰撞器跟随 AttackData 动画时间 =====
        Debug.Log($"[Ultimate] Phase 2→3 | playing rest of animation, collider follows AttackState");
        owner.anim.speed = 1f;

        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            if (ultimateColliderObject != null)
            {
                bool shouldBeActive = owner.fighter.Attackstate == AttackStates.Impact;
                if (ultimateColliderObject.activeSelf != shouldBeActive)
                    ultimateColliderObject.SetActive(shouldBeActive);
            }
            yield return null;
        }

        // 兜底关闭
        if (ultimateColliderObject != null)
            ultimateColliderObject.SetActive(false);

        Debug.Log($"[Ultimate] DONE | final attackState={owner.fighter.Attackstate}");

        DestroyChargeVfx();

        // ===== 释放完毕：随机切形态（Agility / Strength），播动画 + 对应 VFX =====
        yield return DoPhaseSwitch();

        isCharging = false;
        owner.fighter.AttackVfxOverride = null;
        owner.ChangeState(owner.chaseState);
    }

    ///大招结束后：随机切到 Agility/Strength，生成对应 VFX 并播共用的切换动画，动画中途按卡点追加形态材质，播完返回
    private IEnumerator DoPhaseSwitch()
    {
        BossPhase newPhase = Random.value < 0.5f ? BossPhase.Agility : BossPhase.Strength;
        owner.bossPhase = newPhase;

        GameObject vfxPrefab = newPhase == BossPhase.Agility ? agilityPhaseVfx : strengthPhaseVfx;
        Material phaseMat = newPhase == BossPhase.Agility ? agilityPhaseMat : strengthPhaseMat;
        Debug.Log($"[Ultimate] PhaseSwitch | newPhase={newPhase} | vfx={(vfxPrefab != null ? vfxPrefab.name : "null")} | mat={(phaseMat != null ? phaseMat.name : "null")} | anim={phaseSwitchStateName}");

        // 生成对应形态的 VFX
        if (vfxPrefab != null)
        {
            Transform spawn = vfxSpawnPoint != null ? vfxSpawnPoint : owner.transform;
            GameObject vfx = Instantiate(vfxPrefab, spawn.position, spawn.rotation);
            if (vfxLifetime > 0f)
                Destroy(vfx, vfxLifetime);
        }

        bool matApplied = false;

        // 播切换动画并等播完
        if (!string.IsNullOrEmpty(phaseSwitchStateName))
        {
            owner.anim.CrossFadeInFixedTime(phaseSwitchStateName, phaseSwitchCrossFade, phaseSwitchAnimLayer);

            int hash = Animator.StringToHash(phaseSwitchStateName);

            // 等过渡真正进入该 state（超时保护，防 state 名/layer 填错卡死）
            float timeout = 1f;
            while (owner.anim.GetCurrentAnimatorStateInfo(phaseSwitchAnimLayer).shortNameHash != hash && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (timeout <= 0f)
            {
                Debug.LogError($"[Ultimate] PhaseSwitch: 1秒内没进入 state '{phaseSwitchStateName}'，检查 state 名和 layer={phaseSwitchAnimLayer}");
            }
            else
            {
                // 等动画播完（或 Animator 自己转走），途中到卡点就上材质
                while (true)
                {
                    var st = owner.anim.GetCurrentAnimatorStateInfo(phaseSwitchAnimLayer);
                    if (st.shortNameHash != hash || st.normalizedTime >= 1f) break;

                    if (!matApplied && st.normalizedTime >= materialApplyNormalizedTime)
                    {
                        ApplyPhaseMaterial(phaseMat);
                        matApplied = true;
                        Debug.Log($"[Ultimate] PhaseSwitch mat applied | nt={st.normalizedTime:F3}");
                    }
                    yield return null;
                }
            }
        }

        // 兜底：没配动画 / 没进 state / 卡点太靠后没赶上，也要把材质上了
        if (!matApplied)
            ApplyPhaseMaterial(phaseMat);

        Debug.Log($"[Ultimate] PhaseSwitch DONE | bossPhase={owner.bossPhase}");
    }

    /// <summary>把形态材质追加到 phaseMatRenderers 的 materials 末尾，先移除上一个形态的</summary>
    private void ApplyPhaseMaterial(Material newMat)
    {
        RemovePhaseMaterial();
        if (newMat == null) return;
        if (phaseMatRenderers == null || phaseMatRenderers.Length == 0)
        {
            Debug.LogWarning("[Ultimate] PhaseSwitch: phaseMatRenderers 没拖 Renderer，形态材质加不上");
            return;
        }

        foreach (var r in phaseMatRenderers)
        {
            if (r == null) continue;
            var mats = new System.Collections.Generic.List<Material>(r.sharedMaterials);
            mats.Add(newMat);
            r.sharedMaterials = mats.ToArray();
        }
        appliedPhaseMat = newMat;
    }

    /// <summary>移除当前挂在身上的形态材质</summary>
    public void RemovePhaseMaterial()
    {
        if (appliedPhaseMat == null || phaseMatRenderers == null) return;

        foreach (var r in phaseMatRenderers)
        {
            if (r == null) continue;
            var mats = new System.Collections.Generic.List<Material>(r.sharedMaterials);
            mats.RemoveAll(m => m == appliedPhaseMat);
            r.sharedMaterials = mats.ToArray();
        }
        appliedPhaseMat = null;
    }

    /// <summary>矫正跳跃：朝 correctionJumpTarget 做一次完整 JumpAttack，落地后继续 Ultimate</summary>
    private IEnumerator DoCorrectionJump()
    {
        var jumpCfg = owner.jumpAttackState;
        if (jumpCfg == null) { Debug.LogError("[Ultimate] jumpAttackState is null!"); yield break; }

        Debug.Log($"[Ultimate|Jump] START | target={correctionJumpTarget.position} | height={jumpCfg.jumpHeight} | riseSpeed={jumpCfg.riseSpeed} | hover={jumpCfg.hoverDuration} | slam={jumpCfg.slamSpeed}");

        owner.anim.applyRootMotion = false;

        int layer = owner.fighter.AttackAnimLayer;
        int oldHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
        Debug.Log($"[Ultimate|Jump] oldHash={oldHash} | layer={layer}");

        // 用 JumpAttack 的攻击数据触发动画和伤害
        owner.fighter.OverrideAttacks(new System.Collections.Generic.List<AttackData> { jumpCfg.jumpAttackData });
        var target = owner.playerTarget?.GetComponent<ICombatSystem>();
        Debug.Log($"[Ultimate|Jump] Calling TryToAttack | AttackState before={owner.fighter.Attackstate}");
        owner.fighter.TryToAttack(target);
        Debug.Log($"[Ultimate|Jump] TryToAttack done | AttackState after={owner.fighter.Attackstate}");

        // 等动画开始
        int waitAnimFrames = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            int curHash = owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            if (curHash != oldHash && curHash != 0) break;
            waitAnimFrames++;
            yield return null;
        }

        if (owner.fighter.Attackstate == AttackStates.Idle)
        {
            Debug.LogError($"[Ultimate|Jump] FAILED: AttackState is Idle, jump animation never started! waitFrames={waitAnimFrames}");
            yield break;
        }
        Debug.Log($"[Ultimate|Jump] Animation started | waitFrames={waitAnimFrames} | curHash={owner.anim.GetCurrentAnimatorStateInfo(layer).shortNameHash}");

        // 关掉 NavMeshAgent 的位置控制，否则每帧被拉回地面
        if (owner.agent != null)
        {
            owner.agent.updatePosition = false;
            owner.agent.isStopped = true;
        }

        float groundY = owner.transform.position.y;
        float targetY = groundY + jumpCfg.jumpHeight;
        Debug.Log($"[Ultimate|Jump] Rise phase | groundY={groundY:F3} | targetY={targetY:F3} | riseStartNT={jumpCfg.jumpStartNormalizedTime} | riseEndNT={jumpCfg.riseEndNormalizedTime}");

        // ===== RISE =====
        int riseFrames = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            float nt = owner.anim.GetCurrentAnimatorStateInfo(layer).normalizedTime;

            if (nt < jumpCfg.jumpStartNormalizedTime)
            {
                // WAIT
            }
            else if (nt < jumpCfg.riseEndNormalizedTime)
            {
                float newY = owner.transform.position.y + jumpCfg.riseSpeed * Time.deltaTime;
                if (newY >= targetY)
                {
                    Vector3 pos = owner.transform.position;
                    pos.y = targetY;
                    owner.transform.position = pos;
                    Debug.Log($"[Ultimate|Jump] Rise capped at targetY={targetY:F3} | frames={riseFrames}");
                    break;
                }
                owner.transform.Translate(Vector3.up * jumpCfg.riseSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                Debug.Log($"[Ultimate|Jump] Rise ended by anim time | nt={nt:F3} >= riseEndNT={jumpCfg.riseEndNormalizedTime} | frames={riseFrames}");
                break;
            }

            riseFrames++;
            yield return null;
        }

        Debug.Log($"[Ultimate|Jump] Rise complete | y={owner.transform.position.y:F3} | AttackState={owner.fighter.Attackstate}");

        // ===== HOVER：动画冻结，天上悬停 =====
        owner.anim.speed = 0f;
        Debug.Log($"[Ultimate|Jump] Hover start | anim.speed=0 | duration={jumpCfg.hoverDuration}s");
        float hoverTimer = 0f;
        while (hoverTimer < jumpCfg.hoverDuration)
        {
            hoverTimer += Time.deltaTime;
            // 面朝落点
            Vector3 toTarget = correctionJumpTarget.position - owner.transform.position;
            toTarget.y = 0;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                owner.transform.rotation = Quaternion.RotateTowards(
                    owner.transform.rotation,
                    Quaternion.LookRotation(toTarget),
                    owner.rotationSpeed * Time.deltaTime);
            }
            yield return null;
        }

        Debug.Log($"[Ultimate|Jump] Hover done, slamming | target={correctionJumpTarget.position} | bossPos={owner.transform.position}");

        // ===== SLAM：朝矫正位置砸下 =====
        Vector3 slamTarget = correctionJumpTarget.position;
        slamTarget.y = groundY;
        Debug.Log($"[Ultimate|Jump] Slam start | from={owner.transform.position} | to={slamTarget} | speed={jumpCfg.slamSpeed}");

        bool landed = false;
        int slamFrames = 0;
        while (!landed)
        {
            Vector3 currentPos = owner.transform.position;
            Vector3 dir = (slamTarget - currentPos).normalized;
            Vector3 move = dir * jumpCfg.slamSpeed * Time.deltaTime;

            if (move.sqrMagnitude >= (slamTarget - currentPos).sqrMagnitude)
            {
                owner.transform.position = slamTarget;
                landed = true;
                Debug.Log($"[Ultimate|Jump] Slam reached target | frames={slamFrames}");
            }
            else
            {
                owner.transform.Translate(move, Space.World);
            }

            // 地面检测
            if (!landed)
            {
                Vector3 rayOrigin = owner.transform.position + Vector3.up * jumpCfg.groundRayStartY;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    jumpCfg.groundRayStartY + 0.3f, jumpCfg.groundLayer))
                {
                    Vector3 pos = owner.transform.position;
                    pos.y = hit.point.y;
                    owner.transform.position = pos;
                    landed = true;
                    Debug.Log($"[Ultimate|Jump] Slam LANDED by raycast | groundY={hit.point.y:F3} | frames={slamFrames}");
                }
            }

            slamFrames++;
            yield return null;
        }

        // 落地，恢复动画 + NavMeshAgent
        owner.anim.speed = 1f;
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.Warp(owner.transform.position);
            owner.agent.updatePosition = true;
        }
        Debug.Log($"[Ultimate|Jump] Landed, anim.speed=1 | y={owner.transform.position.y:F3} | AttackState={owner.fighter.Attackstate}");

        // 等 JumpAttack 动画播完
        int endFrames = 0;
        while (owner.fighter.Attackstate != AttackStates.Idle)
        {
            endFrames++;
            yield return null;
        }
        Debug.Log($"[Ultimate|Jump] Animation finished | endFrames={endFrames} | AttackState={owner.fighter.Attackstate}");

        // 恢复 Ultimate 的攻击数据，否则动画被 JumpAttack 覆盖
        owner.fighter.OverrideAttacks(new System.Collections.Generic.List<AttackData> { ultimateAttackData });
        Debug.Log($"[Ultimate|Jump] OverrideAttacks restored to ultimateAttackData");

        Debug.Log($"[Ultimate|Jump] DONE, returning to DoUltimate");
    }

    /// <summary>等待动画 normalizedTime 到达目标值</summary>
    private IEnumerator WaitForNormalizedTime(int layer, float targetNT)
    {
        AnimatorStateInfo state;
        do
        {
            state = owner.anim.GetCurrentAnimatorStateInfo(layer);
            yield return null;
        }
        while (state.normalizedTime < targetNT && owner.fighter.Attackstate != AttackStates.Idle);
        Debug.Log($"[Ultimate] WaitForNT reached | target={targetNT:F2} | actual={state.normalizedTime:F3} | attackState={owner.fighter.Attackstate}");
    }

    /// <summary>由 BossController.OnGotHit 调用</summary>
    public void AddDamage(float damage)
    {
        if (!isCharging) return;
        accumulatedDamage += damage;
    }

    private void SpawnChargeVfx()
    {
        if (chargeVfxPrefab == null) return;
        Transform parent = chargeVfxSpawnParent != null ? chargeVfxSpawnParent : owner.transform;
        chargeVfxInstance = Instantiate(chargeVfxPrefab, parent.position, parent.rotation, parent);
    }

    private void DestroyChargeVfx()
    {
        if (chargeVfxInstance != null)
        {
            Destroy(chargeVfxInstance);
            chargeVfxInstance = null;
        }
    }

    public override void Exit()
    {
        StopAllCoroutines();
        isCharging = false;
        DestroyChargeVfx();
        owner.anim.speed = 1f;
        owner.fighter.AttackVfxOverride = null;
        owner.fighter.DisableHitboxes();
        if (ultimateColliderObject != null)
            ultimateColliderObject.SetActive(false);
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = false;
            owner.agent.ResetPath();
        }
    }
}
