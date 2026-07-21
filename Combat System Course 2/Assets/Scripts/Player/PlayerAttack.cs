using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    bool attack;

    [Header("Hit Rotation")]
    [SerializeField] [Range(0f, 1f)] private float hitRotationStrength = 0.5f;

    [Header("Execution")]
    [SerializeField] private float executionRange = 1.5f;
    [SerializeField] private float executeBonus = 3f;
    [SerializeField] [Range(-1f, 0f)] private float executionAngleDot = -0.5f;   // cos(120°)，负值=背后
    [SerializeField] private float idealExecutionDistance = 1.0f;                 // 动画最佳距离，超过此距离自动拉近
    [SerializeField] private Vector3 playerPosOffset = Vector3.zero;              // 玩家位置微调
    [SerializeField] private Vector3 playerRotOffset = Vector3.zero;              // 玩家旋转微调
    [SerializeField] private float indicatorScanInterval = 0.2f;                  // 处决图标扫描间隔

    private float indicatorScanTimer;

    private WeaponEquipmentManager weaponEquipmentManager;
    private PlayerFighter fighter;
    private bool canCombo = true;
    private Animator animator;

    private bool isExecuting;
    private EnemyFighter executionTarget;
    private EnemyTest executionTestTarget;
    private int actionLayerIndex;
    void Start()
    {
        weaponEquipmentManager=GetComponent<WeaponEquipmentManager>();
        animator=GetComponent<Animator>();
        actionLayerIndex = animator.GetLayerIndex("ActionLayer");

        fighter = GetComponent<PlayerFighter>();
        if (fighter != null)
        {
            fighter.OnDamageDealt += OnPlayerDealtDamage;
        }
    }

    private void Update()
    {
        indicatorScanTimer -= Time.deltaTime;
        if (indicatorScanTimer <= 0f)
        {
            indicatorScanTimer = indicatorScanInterval;
            UpdateExecutableIndicators();
        }
    }

    private void OnDestroy()
    {
        if (fighter != null)
        {
            fighter.OnDamageDealt -= OnPlayerDealtDamage;
        }
    }

    /// <summary>
    /// 攻击命中敌人时，角色向目标方向微旋转（增强打击感）
    /// </summary>
    private void OnPlayerDealtDamage(GameObject target)
    {
        if (target == null) return;

        Vector3 dirToTarget = target.transform.position - transform.position;
        dirToTarget.y = 0f;

        if (dirToTarget.sqrMagnitude > 0.001f)
        {
           PlayerController.i?.ForceRotateTowards(dirToTarget, hitRotationStrength);
           
        }
    }
    private void OnEnable()
    {
        InputManager.Instance.OnAttack += TryAttack;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnAttack -= TryAttack;
    }

    private void TryAttack()
    {
        if (isExecuting) return;
        if (!canCombo) return;

        // 反弹期间禁止输入新攻击（canCombo 可能在正放阶段已变为 true）
        var fighter = PlayerController.i?.combatSystem as PlayerFighter;
        if (fighter != null && fighter.IsInPassiveAction) return;

        // ===== 处决检测 =====
        EnemyFighter execTarget = FindExecutionTarget();
        if (execTarget != null)
        {
            StartExecution(execTarget);
            return;
        }

        EnemyTest testTarget = FindTestExecutionTarget();
        if (testTarget != null)
        {
            StartTestExecution(testTarget);
            return;
        }

        if(weaponEquipmentManager.GetCurrentWeapon()!=null&& weaponEquipmentManager.isWeaponDrawn)
        {
            // 清理可能残留的 Trigger，确保动画机状态干净
            animator.ResetTrigger("MeleeAttack");
            animator.ResetTrigger("attack");

            float animSpeed=animator.GetFloat("Speed");
            if (animSpeed < 1.9f)
            {
                animator.SetTrigger("MeleeAttack");
                Debug.Log("MeleeAttack!");
            }
            else
                animator.SetTrigger("attack");

            // 确保 AttackSpeed 为正常值（反弹协程可能残留 0 或 -1）
            animator.SetFloat("AttackSpeed", 1f);

            canCombo = false;
            animator.applyRootMotion = true;

            // 攻击开始时立即锁定脚本旋转，避免与 CombatController 的 Root Motion 旋转冲突导致相机抖动
            if (PlayerController.i != null)
            {
                PlayerController.i.LockRotation = true;
                if (PlayerController.i.combatSystem != null)
                {
                    PlayerController.i.combatSystem.InAction = true;
                }
            }
            
        }
        else
            Debug.Log("没有装备/拔出武器，无法攻击");

    }
    public void OnAttackEnd()
    {
        if (!canCombo) 
        {
            return; 
        }
        // 动画结束，关闭 Root Motion，交还给代码控制位移
        animator.applyRootMotion = false;
        canCombo = true;
        
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = false; // 确保解锁
            if (PlayerController.i.combatSystem != null)
            {
                PlayerController.i.combatSystem.InAction = false;
            }

        }
    }
    // 停止转向：在动画开始突进或发力时调用
    public void StartRotationLock()
    {
        Debug.Log($"[Attack] StartRotationLock called, frame={Time.frameCount}");
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = true;
        }
    }

    // 恢复转向：在动画收招或允许玩家微调方向时调用
    public void StopRotationLock()
    {
        Debug.Log($"[Attack] StopRotationLock called, frame={Time.frameCount}");
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = false;
            canCombo = true;
        }
    }

    // 供外部（如反弹系统）强制重置攻击状态
    public void ForceResetAttackState()
    {
        canCombo = true;
        animator.applyRootMotion = false;
    }

    // ==========================================
    // 处决图标扫描
    // ==========================================

    private void UpdateExecutableIndicators()
    {
        // ---- EnemyFighter ----
        var fighters = FindObjectsOfType<EnemyFighter>();
        foreach (var enemy in fighters)
        {
            bool canExec = IsEnemyFighterExecutable(enemy);
            enemy.SetExecutableIndicator(canExec);
        }

        // ---- EnemyTest ----
        var tests = FindObjectsOfType<EnemyTest>();
        foreach (var enemy in tests)
        {
            bool canExec = IsEnemyTestExecutable(enemy);
            enemy.SetExecutableIndicator(canExec);
        }
    }

    private bool IsEnemyFighterExecutable(EnemyFighter enemy)
    {
        if (enemy.HealthSystem == null || enemy.HealthSystem.IsDead) return false;
        if (!enemy.CanBeExecuted) return false;

        var ec = enemy.GetComponent<EnemyController>();
        if (ec == null) return false;
        if (!ec.IsInState(EnemyStates.Idle)) return false;

        Vector3 toPlayer = transform.position - enemy.transform.position;
        float distSq = toPlayer.sqrMagnitude;
        if (distSq > executionRange * executionRange) return false;

        toPlayer.y = 0f;
        Vector3 enemyForward = enemy.transform.forward;
        enemyForward.y = 0f;
        float dot = Vector3.Dot(enemyForward.normalized, toPlayer.normalized);
        if (dot > executionAngleDot) return false;

        return true;
    }

    private bool IsEnemyTestExecutable(EnemyTest enemy)
    {
        if (enemy.HealthSystem == null || enemy.HealthSystem.IsDead) return false;
        if (!enemy.CanBeExecuted) return false;

        Vector3 toPlayer = transform.position - enemy.transform.position;
        float distSq = toPlayer.sqrMagnitude;
        if (distSq > executionRange * executionRange) return false;

        toPlayer.y = 0f;
        Vector3 enemyForward = enemy.transform.forward;
        enemyForward.y = 0f;
        float dot = Vector3.Dot(enemyForward.normalized, toPlayer.normalized);
        if (dot > executionAngleDot) return false;

        return true;
    }

    // ==========================================
    // 处决系统
    // ==========================================

    /// <summary>扫描附近敌人，返回满足处决条件且最近的一个（无则 null）</summary>
    private EnemyFighter FindExecutionTarget()
    {
        EnemyFighter best = null;
        float bestDist = executionRange * executionRange;

        var allEnemies = FindObjectsOfType<EnemyFighter>();
        Debug.Log($"[Execution] FindExecutionTarget: 扫描 {allEnemies.Length} 个敌人, range={executionRange}, angleDot={executionAngleDot}");

        foreach (var enemy in allEnemies)
        {
            if (enemy.HealthSystem == null || enemy.HealthSystem.IsDead)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(dead或null HealthSystem)");
                continue;
            }
            if (!enemy.CanBeExecuted)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(!CanBeExecuted)");
                continue;
            }

            var ec = enemy.GetComponent<EnemyController>();
            if (ec == null)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(无EnemyController)");
                continue;
            }
            if (!ec.IsInState(EnemyStates.Idle))
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(非Idle, 当前状态机运行中)");
                continue;
            }

            Vector3 toPlayer = transform.position - enemy.transform.position;
            float distSq = toPlayer.sqrMagnitude;
            if (distSq > bestDist)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(距离 {Mathf.Sqrt(distSq):F2}m > {executionRange}m)");
                continue;
            }

            toPlayer.y = 0f;
            Vector3 enemyForward = enemy.transform.forward;
            enemyForward.y = 0f;
            float dot = Vector3.Dot(enemyForward.normalized, toPlayer.normalized);
            if (dot > executionAngleDot)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(角度 dot={dot:F3} > {executionAngleDot}, 不在背后)");
                continue;
            }

            Debug.Log($"[Execution]   {enemy.gameObject.name}: ✓ 满足条件! dist={Mathf.Sqrt(distSq):F2}m, dot={dot:F3}");
            bestDist = distSq;
            best = enemy;
        }

        Debug.Log($"[Execution] FindExecutionTarget 结果: {(best != null ? best.gameObject.name : "null")}");
        return best;
    }

    /// <summary>开始处决：触发双方 root motion 动画，动画自带位移和旋转</summary>
    private void StartExecution(EnemyFighter enemy)
    {
        executionTarget = enemy;

        Vector3 playerPosBefore = transform.position;
        Vector3 enemyPosBefore = enemy.transform.position;
        Debug.Log($"[Execution] ===== StartExecution =====");
        Debug.Log($"[Execution] 玩家位置: {playerPosBefore}");
        Debug.Log($"[Execution] 敌人位置: {enemyPosBefore}");
        Debug.Log($"[Execution] 距离: {Vector3.Distance(playerPosBefore, enemyPosBefore):F3}m");

        // 计算敌人到玩家的水平方向
        Vector3 dirFromEnemy = (transform.position - enemy.transform.position);
        dirFromEnemy.y = 0f;
        dirFromEnemy.Normalize();

        // 双方旋转面对面
        enemy.transform.rotation = Quaternion.LookRotation(dirFromEnemy);
        transform.rotation = Quaternion.LookRotation(-dirFromEnemy);
        Debug.Log($"[Execution] 敌人旋转后 forward={enemy.transform.forward}");

        // 只拉近不推远：超过最佳距离时沿前方拉近
        float currentDist = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z));
        if (currentDist > idealExecutionDistance)
        {
            float pullIn = currentDist - idealExecutionDistance;
            transform.position += transform.forward * pullIn;
            Debug.Log($"[Execution] 拉近 {pullIn:F3}m (原距离 {currentDist:F3}m → {idealExecutionDistance}m)");
        }

        // 应用微调偏移
        transform.position += playerPosOffset;
        transform.rotation *= Quaternion.Euler(playerRotOffset);
        Debug.Log($"[Execution] 对齐后 玩家位置: {transform.position}, forward={transform.forward}");

        // 标记状态
        isExecuting = true;
        fighter.InAction = true;
        if (PlayerController.i != null)
            PlayerController.i.isMovementEnabled = false;

        // 清理残留 Trigger
        animator.ResetTrigger("MeleeAttack");
        animator.ResetTrigger("attack");

        // 在 ActionLayer 播放处决动画（in-place，不用 root motion）
        Debug.Log($"[Execution] CrossFade Execution on layer {actionLayerIndex}");
        animator.CrossFade("Execution", 0.1f, actionLayerIndex);
        var nextState = animator.GetNextAnimatorStateInfo(actionLayerIndex);
        Debug.Log($"[Execution] NextState on layer {actionLayerIndex}: nameHash={nextState.fullPathHash}, length={nextState.length}");

        // 敌人播放被处决动画（layer 1 ActionLayer，root motion）
        enemy.PlayExecutionReaction();
    }

    // ==========================================
    // 处决系统 — EnemyTest
    // ==========================================

    private EnemyTest FindTestExecutionTarget()
    {
        EnemyTest best = null;
        float bestDist = executionRange * executionRange;

        var all = FindObjectsOfType<EnemyTest>();
        Debug.Log($"[Execution] FindTestExecutionTarget: 扫描 {all.Length} 个 EnemyTest, range={executionRange}, angleDot={executionAngleDot}");

        foreach (var enemy in all)
        {
            if (enemy.HealthSystem == null || enemy.HealthSystem.IsDead)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(dead或无HealthSystem)");
                continue;
            }
            if (!enemy.CanBeExecuted)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(!CanBeExecuted)");
                continue;
            }

            Vector3 toPlayer = transform.position - enemy.transform.position;
            float distSq = toPlayer.sqrMagnitude;
            if (distSq > bestDist)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(距离 {Mathf.Sqrt(distSq):F2}m > {executionRange}m)");
                continue;
            }

            toPlayer.y = 0f;
            Vector3 enemyForward = enemy.transform.forward;
            enemyForward.y = 0f;
            float dot = Vector3.Dot(enemyForward.normalized, toPlayer.normalized);
            if (dot > executionAngleDot)
            {
                Debug.Log($"[Execution]   {enemy.gameObject.name}: 跳过(角度 dot={dot:F3} > {executionAngleDot}, 不在背后)");
                continue;
            }

            Debug.Log($"[Execution]   {enemy.gameObject.name}: ✓ 满足条件! dist={Mathf.Sqrt(distSq):F2}m, dot={dot:F3}");
            bestDist = distSq;
            best = enemy;
        }

        Debug.Log($"[Execution] FindTestExecutionTarget 结果: {(best != null ? best.gameObject.name : "null")}");
        return best;
    }

    private void StartTestExecution(EnemyTest enemy)
    {
        executionTestTarget = enemy;

        Vector3 playerPosBefore = transform.position;
        Vector3 enemyPosBefore = enemy.transform.position;
        Debug.Log($"[Execution] ===== StartTestExecution =====");
        Debug.Log($"[Execution] 玩家位置: {playerPosBefore}");
        Debug.Log($"[Execution] EnemyTest位置: {enemyPosBefore}");
        Debug.Log($"[Execution] 距离: {Vector3.Distance(playerPosBefore, enemyPosBefore):F3}m");

        // 计算敌人到玩家的水平方向
        Vector3 dirFromEnemy = (transform.position - enemy.transform.position);
        dirFromEnemy.y = 0f;
        dirFromEnemy.Normalize();

        // 双方旋转面对面
        enemy.transform.rotation = Quaternion.LookRotation(dirFromEnemy);
        transform.rotation = Quaternion.LookRotation(-dirFromEnemy);
        Debug.Log($"[Execution] EnemyTest旋转后 forward={enemy.transform.forward}");

        // 只拉近不推远：超过最佳距离时沿前方拉近
        float currentDist = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(enemy.transform.position.x, 0f, enemy.transform.position.z));
        if (currentDist > idealExecutionDistance)
        {
            float pullIn = currentDist - idealExecutionDistance;
            transform.position += transform.forward * pullIn;
            Debug.Log($"[Execution] 拉近 {pullIn:F3}m (原距离 {currentDist:F3}m → {idealExecutionDistance}m)");
        }

        // 应用微调偏移
        transform.position += playerPosOffset;
        transform.rotation *= Quaternion.Euler(playerRotOffset);
        Debug.Log($"[Execution] 对齐后 玩家位置: {transform.position}, forward={transform.forward}");

        // 标记状态
        isExecuting = true;
        fighter.InAction = true;
        if (PlayerController.i != null)
            PlayerController.i.isMovementEnabled = false;

        animator.ResetTrigger("MeleeAttack");
        animator.ResetTrigger("attack");
        animator.CrossFade("Execution", 0.1f, actionLayerIndex);

        enemy.PlayExecutionReaction();
    }

    /// <summary>动画事件：处决命中帧，结算x倍武器伤害</summary>
    public void AE_ExecutionImpact()
    {
        float weaponDamage = fighter?.GetWeaponDamage() ?? 1f;
        float execDamage = weaponDamage * executeBonus;

        Debug.Log($"[Execution] ===== AE_ExecutionImpact =====");
        Debug.Log($"[Execution] 玩家位置: {transform.position}");

        // EnemyFighter 目标
        if (executionTarget != null)
        {
            Debug.Log($"[Execution] EnemyFighter位置: {executionTarget.transform.position}");
            Debug.Log($"[Execution] 距离: {Vector3.Distance(transform.position, executionTarget.transform.position):F3}m");
            executionTarget.TakeDamage(execDamage);

            if (!executionTarget.HealthSystem.IsDead)
            {
                executionTarget.animator.SetTrigger("Undead");
            }
        }

        // EnemyTest 目标
        if (executionTestTarget != null)
        {
            Debug.Log($"[Execution] EnemyTest位置: {executionTestTarget.transform.position}");
            Debug.Log($"[Execution] 距离: {Vector3.Distance(transform.position, executionTestTarget.transform.position):F3}m");
            executionTestTarget.TakeDamage(execDamage);

            if (!executionTestTarget.HealthSystem.IsDead)
            {
                if (executionTestTarget.animator != null)
                    executionTestTarget.animator.SetTrigger("Undead");
            }
        }
    }

    /// <summary>动画事件：处决动画结束，清理所有状态</summary>
    public void AE_ExecutionEnd()
    {
        Debug.Log($"[Execution] ===== AE_ExecutionEnd =====");
        Debug.Log($"[Execution] 玩家结束位置: {transform.position}");

        if (executionTarget != null)
        {
            Debug.Log($"[Execution] EnemyFighter结束位置: {executionTarget.transform.position}");
            Debug.Log($"[Execution] 结束距离: {Vector3.Distance(transform.position, executionTarget.transform.position):F3}m");
            executionTarget.OnExecutionEnd();
        }
        if (executionTestTarget != null)
        {
            Debug.Log($"[Execution] EnemyTest结束位置: {executionTestTarget.transform.position}");
            Debug.Log($"[Execution] 结束距离: {Vector3.Distance(transform.position, executionTestTarget.transform.position):F3}m");
            executionTestTarget.OnExecutionEnd();
        }

        isExecuting = false;
        executionTarget = null;
        executionTestTarget = null;

        fighter.InAction = false;
        animator.applyRootMotion = false;

        if (PlayerController.i != null)
            PlayerController.i.isMovementEnabled = true;
    }

}