using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool sprintHeld;
    private bool crouchHeld;
    private Vector2 lookInput; // ԭʼ���/ҡ������
    public Vector2 LookInput => lookInput;
    [Header("Movement Settings")]
    [SerializeField] private float crouchSpeedMultiplier = 0.3f; // �����ٶȣ�30%��
    private bool shouldCrouch;      // ʵ�ʶ���״̬
    private bool isCrouching = false;
    [SerializeField] LayerMask obstacleLayer; // ֻ����ϰ���



    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float standHeight = 1.8f;
    private float currentHeightVelocity;  // ����������ƽ������
    [SerializeField] private float fallStartDelay = 0.15f; // ��ض�ò�������
    [SerializeField] private float minFallSpeed = -6f;     // ����������ٶ���ֵ

    private float notGroundedTimer = 0f;



    private float moveDuration = 0f; // 移动持续时间
    [SerializeField] private float timeToRun = 1.5f; // 走多久转为跑（秒）
    private bool isSprinting = false; // ��ǰ�Ƿ����ڳ�̣�����������

    private bool isFalling = false;
    [SerializeField] float rotationSpeed = 500f;


    public PlayerCameraController cameraController;

    public Vector3 InputDir { get; private set; }
    public static PlayerController i { get; private set; }
    public bool LockRotation { get; set; } = false;

    public Quaternion targetRotation;
    public ICombatSystem 
    combatSystem;

    public Animator animator;
    private int speedHash;
    private int dirXHash;
    private int dirYHash;
    [Header("Locomotion Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float lockWalkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float sprintSpeed = 9.5f;
    [Header("Smooth Locomotion & Root Motion")]
    private float currentPhysicalSpeed; // 替代原本局部的 currentMoveSpeed
    private float speedSmoothVelocity;  
    [SerializeField] private float speedSmoothTime = 0.1f; 
    private bool wasInActionLastFrame;  // 用于检测动量接力的时机
    private CharacterController charactercontroller;
    private ParkourController parkourController;
    private GroundCheckSensor groundSensor; 
    public CombatController combatController;
    private HeadCollisionChecker headChecker;
    public bool isGrounded;
    private bool isDrinking;
    private float ySpeed;
    public bool isMovementEnabled = true;

    [Header("Roll")]
    [SerializeField] private string rollAnimFront = "Esc_Roll_Front_Root";
    [SerializeField] private string rollAnimBack = "Esc_Roll_Back_Root";
    [SerializeField] private string rollAnimLeft = "Esc_Roll_Left_Root";
    [SerializeField] private string rollAnimRight = "Esc_Roll_Right_Root";
    [SerializeField] private float rollCooldown = 0.8f;
    [SerializeField] private int rollEnergyCost = 15;
    [SerializeField] private float rollExitTime = 0.75f;
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private AnimationCurve rollSpeedCurve =
        AnimationCurve.EaseInOut(0, 1, 1, 0);
    private Vector3 rollDirection;
    private bool isRolling = false;
    private float lastRollTime = -Mathf.Infinity;
    private int rollLayerIndex;
    private bool rollEndTriggered;
    private Coroutine fadeRollCoroutine;

    [HideInInspector] public bool isLockedOn = false;
    [HideInInspector] public Vector3 lockedTargetDir;
    private bool wasLockedOnLastFrame;
    public ItemSO testHealthPotion;

    private void Awake()
    {
        i = this;
        animator = GetComponent<Animator>();
        charactercontroller = GetComponent<CharacterController>();
        combatSystem = GetComponent<ICombatSystem>();
        cameraController = GetComponent<PlayerCameraController>();
        parkourController = GetComponent<ParkourController>();
        groundSensor=GetComponent<GroundCheckSensor>();
        headChecker = GetComponentInChildren<HeadCollisionChecker>();
        inputActions = InputManager.Instance.Actions;
        isCrouching = false;
        StartCoroutine(DelayedRegistration());
        RegisterToHUD();
        UIStateManager.OnUIActiveStateChanged += OnUIActiveStateChanged;
        InputManager.Instance.OnQuickItemUse += OnQuickItemUse;
        speedHash = Animator.StringToHash("Speed");
        dirXHash = Animator.StringToHash("DirX");
        dirYHash = Animator.StringToHash("DirY");
        rollLayerIndex = animator.GetLayerIndex("Roll");
    }

    private IEnumerator DelayedRegistration()
    {
        yield return new WaitUntil(() => PlayerHUDUI.Instance != null);

        PlayerProperty property = GetComponent<PlayerProperty>();
        HealthSystem healthSys = GetComponent<HealthSystem>();

        if (property != null && healthSys != null)
        {
            PlayerHUDUI.Instance.RegisterPlayerComponents(property, healthSys);
        }
    }

    private void Start()
    {
        UIStateManager.SetUIActive(false);
        InputManager.Instance.SwitchToPlayer();
        
    }

    private void Update()
    {
        // ─────────────── 基础输入读取 ───────────────
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        crouchHeld = inputActions.Player.Crouch.IsPressed();
        sprintHeld = inputActions.Player.Sprint.IsPressed();
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if ((!isMovementEnabled && !isRolling) || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            animator.SetFloat(speedHash, 0f);
            // 如果不再用 DirX/DirY，下面两行可以删掉
            animator.SetFloat(dirXHash, 0f);
            animator.SetFloat(dirYHash, 0f);
            return;
        }

        // ─────────────── 下蹲逻辑 ───────────────
        if (crouchHeld && isGrounded && !combatSystem.InAction)
        {
            isCrouching = true;
        }
        else if (!crouchHeld && isCrouching && headChecker != null && headChecker.CanStandUpFromCrouch())
        {
            isCrouching = false;
        }

        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("CrouchMoving", isCrouching && moveInput.magnitude > 0.1f);

        // ─────────────── 攀爬与翻滚输入 ───────────────
        if (inputActions.Player.Roll.WasPressedThisFrame() && !isDrinking && !UIStateManager.IsAnyUIActive && isGrounded && !isRolling)
        {
            if (parkourController != null && parkourController.TryClimb()) return;
            StartRoll();
            return;
        }

        // ─────────────── 移动向量计算 ───────────────
        Vector2 rawInput = Vector2.ClampMagnitude(moveInput, 1f);
        float inputMagnitude = rawInput.magnitude;

        
        Vector3 refForward;
        Vector3 refRight;

        if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
        {
            // 【锁定状态】：彻底无视摇晃过渡的相机！强制使用“敌人方向”作为输入参考系
            refForward = lockedTargetDir.normalized;
            refRight = Vector3.Cross(Vector3.up, refForward).normalized;
        }
        else
        {
            // 【非锁定状态】：使用相机的平面朝向作为输入参考系
            refForward = GetCameraPlanarRotation() * Vector3.forward;
            refRight = GetCameraPlanarRotation() * Vector3.right;
        }

        Vector3 worldMoveDir = (refRight * rawInput.x + refForward * rawInput.y).normalized;

        // ─────────────── 核心：移动时间与动量累积 ───────────────
        // 只有在地面、没蹲下、且有有效位移输入时才累积时间
        if (inputMagnitude > 0.1f && isGrounded && !isCrouching)
        {
            moveDuration += Time.deltaTime;
        }
        else
        {
            if (inputMagnitude <= 0.1f || notGroundedTimer > 0.05f)
            {
                moveDuration = 0f;
            }
        }

        // 计算当前处于 Walk(0) 到 Run(1) 的哪个进度
        float accelerationT = Mathf.Clamp01(moveDuration / timeToRun);

        // ─────────────── 动画参数设置 (1D Blend Tree) ───────────────
        float animSpeed = 0f;
        float damping = 0.075f;

        // 冲刺判定
        bool wantsSprint = sprintHeld && inputMagnitude > 0.1f && !combatSystem.InAction;
        bool wasSprinting = isSprinting;
        isSprinting = false;

        if (wantsSprint && !isRolling)
        {
            float costPerSecond = PlayerProperty.Instance.GetSprintCostPerSecond();
            float threshold = wasSprinting ? 0f : 15f;
            if (PlayerProperty.Instance.EnergyValue > threshold
                && PlayerProperty.Instance.ConsumeEnergy(costPerSecond * Time.deltaTime))
            {
                isSprinting = true;
            }
        }

        if (combatSystem.InAction || isRolling)
        {
            animSpeed = 0f;
            isSprinting = false;
        }
        else if (isSprinting)
        {
            animSpeed = 1.99f;
        }
        else if (inputMagnitude > 0.01f)
        {
            animSpeed = Mathf.Lerp(0.01f, 1.0f, accelerationT);
        }
        else
        {
            animSpeed = 0f;
        }
        // ─────────────── 动画参数设置 (新增 2D 锁敌移动逻辑) ───────────────
        bool lockJustToggled = isLockedOn != wasLockedOnLastFrame;
        wasLockedOnLastFrame = isLockedOn;

        if (isLockedOn)
        {
            // 1. 将世界位移方向转为相对于玩家的本地坐标
            Vector3 localMove = transform.InverseTransformDirection(worldMoveDir);
            if (localMove.magnitude > 1f) localMove.Normalize();

            // 2. 确定振幅：走是 0.1，跑是 1.0
            float targetAmplitude = 0f;
            if (inputMagnitude > 0.01f)
                targetAmplitude = isSprinting ? 1.0f : 0.1f;

            // 3. 计算最终传递给 Animator 的坐标
            float finalX = localMove.x * targetAmplitude;
            float finalY = localMove.z * targetAmplitude;

            // 4. Speed 必须在 Blend Tree 阈值范围 [0, 0.64] 内
            //    walk 阈值区间 ~0.08~0.32 / run 阈值区间 ~0.40~0.64
            //    之前用 1.0 越界，Unity clamp 到 0.64 永远指向 Run Right → 方向错乱
            float lockSpeed = targetAmplitude > 0.5f ? 0.5f : (targetAmplitude > 0.01f ? 0.2f : 0f);

            if (lockJustToggled)
            {
                // 切换帧：立即设置，不用阻尼，避免过渡期新旧数值混用
                animator.SetFloat(dirXHash, finalX);
                animator.SetFloat(dirYHash, finalY);
                animator.SetFloat(speedHash, lockSpeed);
            }
            else
            {
                animator.SetFloat(dirXHash, finalX, 0.1f, Time.deltaTime);
                animator.SetFloat(dirYHash, finalY, 0.1f, Time.deltaTime);
                animator.SetFloat(speedHash, lockSpeed, damping, Time.deltaTime);
            }
        }
        else
        {
            // --- 非锁定状态：保持原本的 1D 逻辑 ---
            animator.SetFloat(speedHash, animSpeed, damping, Time.deltaTime);

            if (lockJustToggled)
            {
                // 解锁帧：立即重置 DirX/DirY，避免 2D 残留值混入 1D 过渡
                animator.SetFloat(dirXHash, 0f);
                animator.SetFloat(dirYHash, 0f);
            }
            else
            {
                animator.SetFloat(dirXHash, 0f, 0.1f, Time.deltaTime);
                animator.SetFloat(dirYHash, 0f, 0.1f, Time.deltaTime);
            }
        }

        // ─────────────── 动量接力与目标速度计算 ───────────────
        if (wasInActionLastFrame && !combatSystem.InAction)
        {
            if (inputMagnitude > 0.1f)
            {
                // 攻击结束瞬间如果推着摇杆，给予初速度，避免从0起步的顿挫
                currentPhysicalSpeed = isSprinting ? (sprintSpeed * 0.6f) : walkSpeed; 
                moveDuration = timeToRun; // 瞬间填满加速时间
            }
        }
        wasInActionLastFrame = combatSystem.InAction;

        float targetMoveSpeed = 0f;

        // 只有非动作状态，才产生 WASD 目标速度
        if (!combatSystem.InAction)
        {
            if (isCrouching) targetMoveSpeed = walkSpeed * crouchSpeedMultiplier;
            else if (isSprinting) targetMoveSpeed = sprintSpeed * inputMagnitude;
            else if (isLockedOn) targetMoveSpeed = lockWalkSpeed * inputMagnitude;
            else targetMoveSpeed = Mathf.Lerp(walkSpeed, runSpeed, accelerationT) * inputMagnitude;

            // 重型武器减速
            targetMoveSpeed *= WeaponEquipmentManager.Instance.CurrentSpeedMultiplier;
        }

        // 平滑过渡到目标速度
        currentPhysicalSpeed = Mathf.SmoothDamp(currentPhysicalSpeed, targetMoveSpeed, ref speedSmoothVelocity, speedSmoothTime);

        // ─────────────── 旋转与重力逻辑 ───────────────
        Vector3 moveDir = worldMoveDir;

        // 1. 获取传感器信息
        var snapInfo = groundSensor.GetSnapInfo();

        // 2. 核心修改：综合判定地面状态
        // 只要物理显示在地，或者传感器建议吸附，都视为 isGrounded
        bool physicalGrounded = charactercontroller.isGrounded;
        isGrounded = physicalGrounded || snapInfo.shouldSnap;

        if (isGrounded)
        {
            // 3. 执行吸附位移：如果物理离地但传感器发现地面（下楼梯瞬间），强制下压
            if (!physicalGrounded && snapInfo.shouldSnap)
            {
                charactercontroller.Move(Vector3.down * snapInfo.distanceToGround);
            }

            // 4. 根据状态设置下压力
            // 如果在冲刺，给 -12f 强力压在台阶上；否则保持原有的 -1.5f
            ySpeed = isSprinting ? -12f : -1.5f; 
            
            notGroundedTimer = 0f;
        }
        else
        {
            // 5. 只有彻底离地才应用重力
            ySpeed += Physics.gravity.y * Time.deltaTime;
            notGroundedTimer += Time.deltaTime;
        }

        // 下落动画切换
        if (!isGrounded && notGroundedTimer > fallStartDelay && ySpeed < minFallSpeed && !isFalling)
        {
            isFalling = true;
            animator.SetBool("Falling", true);
        }
        else if (isGrounded && isFalling)
        {
            isFalling = false;

            animator.SetBool("Falling", false);

            float speed = animator.GetFloat(speedHash);

            if (speed < 1.9f)
            {
                animator.Play("Land_Idle", -1, 0f);
            }
            else
            {
                animator.Play("Land_Move", -1, 0f);
            }
        }

        // ─────────────── 应用位移与旋转 ───────────────
        Vector3 velocity;

        if (isRolling)
        {
            float curveValue = 1f;

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(rollLayerIndex);

            bool stateMatch = state.IsName(rollAnimFront) ||
                state.IsName(rollAnimBack) ||
                state.IsName(rollAnimLeft) ||
                state.IsName(rollAnimRight);

            if (stateMatch)
            {
                curveValue = rollSpeedCurve.Evaluate(state.normalizedTime);
            }

            velocity = rollDirection * rollSpeed * curveValue;

            if (Time.frameCount % 3 == 0) // 每3帧打一次，避免刷屏
                Debug.Log($"[ROLL] frame={Time.frameCount} state={state.shortNameHash} stateMatch={stateMatch} normTime={state.normalizedTime:F3} curve={curveValue:F3} vel={velocity.magnitude:F2}");
        }
        else
        {
            velocity = moveDir * currentPhysicalSpeed;
        }

        if (!LockRotation && !isRolling)
        {
            if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
            {
                // 锁定状态：身体强制面向敌人 (EnemyLockSystem 已经在更新 lockedTargetDir)
                targetRotation = Quaternion.LookRotation(lockedTargetDir);
            }
            else if (inputMagnitude > 0)
            {
                // 非锁定状态：身体面向移动方向
                targetRotation = Quaternion.LookRotation(moveDir);
            }
        }
        if (!LockRotation)
        {
            float activeRotSpeed = combatSystem.InAction ? rotationSpeed * 0.5f : rotationSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, activeRotSpeed * Time.deltaTime);
        }

        // 梯子检测 
        Vector3 rayOrigin = transform.position + Vector3.up * 0.4f + transform.forward * (charactercontroller.radius + 0.05f);
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, 0.4f))
        {
            if (hit.transform.TryGetComponent<Ladder>(out Ladder ladder))
            {
                velocity = Vector3.up * 3f;
                ySpeed = 0f;
                isGrounded = true;
            }
        }
        InputDir = worldMoveDir;
        velocity.y = ySpeed;
        if (!animator.applyRootMotion || isRolling)
        {
            charactercontroller.Move(velocity * Time.deltaTime);
        }
        if (!isSprinting && PlayerProperty.Instance.energyValue < PlayerProperty.Instance.MaxEnergy)
        {
            float regenRate = 0f;

            // 根据是否在地面移动来决定恢复速率[cite: 4, 5]
            if (isGrounded && moveInput.magnitude > 0.1f)
            {
                regenRate = PlayerProperty.Instance.walkRegenRate; // 使用行走恢复率
            }
            else
            {
                regenRate = PlayerProperty.Instance.idleRegenRate; // 使用静止恢复率[cite: 4]
            }

            // 执行恢复
            PlayerProperty.Instance.RestoreEnergy(regenRate * Time.deltaTime);
        }
    }

    private void OnAnimatorMove()
    {
        if (!isRolling &&
            combatSystem != null &&
            combatSystem.InAction &&
            animator != null &&
            animator.applyRootMotion)
        {
            // 获取本帧动画自带的位移
            Vector3 rootMotionDeltaPosition = animator.deltaPosition;

            // 将系统计算的重力/下压力叠加进去，防止播放攻击动画时角色浮空
            rootMotionDeltaPosition.y = ySpeed * Time.deltaTime;

            // 使用 CharacterController 移动，确保攻击突进会吃物理碰撞（不会穿墙）
            charactercontroller.Move(rootMotionDeltaPosition);
        }
    }

    public Vector3 GetIntentDirection()
    {
        return InputDir != Vector3.zero ? InputDir : transform.forward;
    }

   

    private void RegisterToHUD()
    {
        PlayerProperty property = GetComponent<PlayerProperty>();
        HealthSystem healthSys = GetComponent<HealthSystem>();

        if (PlayerHUDUI.Instance != null && property != null && healthSys != null)
        {
            PlayerHUDUI.Instance.RegisterPlayerComponents(property, healthSys);
        }
    }

    private void OnUIActiveStateChanged(bool isUIActive)
    {
        isMovementEnabled = !isUIActive;

        if (isUIActive)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
        }
    }

    private void StartRoll()
    {
        if (isRolling || Time.time < lastRollTime + rollCooldown)
        {
            Debug.Log($"[ROLL] StartRoll blocked: isRolling={isRolling}, cooldownRemain={lastRollTime + rollCooldown - Time.time:F3}");
            return;
        }

        if (!PlayerProperty.Instance.ConsumeEnergy(rollEnergyCost))
        {
            Debug.Log("[ROLL] StartRoll blocked: not enough energy");
            return;
        }

        if (rollLayerIndex < 0)
        {
            Debug.LogError("Roll layer not found in Animator.");
            return;
        }

        isRolling = true;
        isMovementEnabled = false;
        lastRollTime = Time.time;

        combatSystem.InAction = true;
        isCrouching = false;

        // 记录翻滚方向
        if (moveInput.magnitude > 0.1f)
        {
            Quaternion camRot = GetCameraPlanarRotation();
            rollDirection = camRot * new Vector3(moveInput.x, 0, moveInput.y);
            rollDirection.y = 0f;
            rollDirection.Normalize();
        }
        else
        {
            rollDirection = transform.forward;
        }

        if (fadeRollCoroutine != null)
            StopCoroutine(fadeRollCoroutine);

        animator.SetLayerWeight(rollLayerIndex, 1f);

        string animName = GetRollDirection();
        Debug.Log($"[ROLL] StartRoll frame={Time.frameCount} anim={animName} dir={rollDirection} layerWeight={animator.GetLayerWeight(rollLayerIndex):F2}");
        animator.CrossFade(animName, 0.05f, rollLayerIndex, 0f);

        StartCoroutine(WaitForRollEnd(animName));
    }

    private string GetRollDirection()
    {
        Quaternion camRot = GetCameraPlanarRotation();
        Vector3 worldDir = camRot * new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 localDir = transform.InverseTransformDirection(worldDir);

        if (localDir.z > 0.3f) return rollAnimFront;
        if (localDir.z < -0.3f) return rollAnimBack;
        if (localDir.x < -0.3f) return rollAnimLeft;
        if (localDir.x > 0.3f) return rollAnimRight;
        return rollAnimFront;
    }

    private IEnumerator WaitForRollEnd(string animName)
    {
        Debug.Log($"[ROLL] WaitForRollEnd started frame={Time.frameCount} anim={animName}");
        // 等动画事件或外部中断触发 OnRollEnd()
        yield return new WaitUntil(() => rollEndTriggered);

        Debug.Log($"[ROLL] WaitForRollEnd triggered frame={Time.frameCount} isRolling={isRolling}");

        rollEndTriggered = false;

        // OnRollEnd 已经做完清理的话直接退出
        if (!isRolling) yield break;
        animator.SetLayerWeight(rollLayerIndex, 0f);
        combatSystem.InAction = false;
        isRolling = false;
        if (!UIStateManager.IsAnyUIActive) isMovementEnabled = true;
    }

    /// <summary>翻滚结束：Animation Event 调用，或受击/死亡中断时由 PlayerFighter 调用</summary>
    public void OnRollEnd()
    {
        Debug.Log($"[ROLL] OnRollEnd called frame={Time.frameCount} stackTrace: {new System.Diagnostics.StackTrace(1, false)}");

        if (!isRolling) return;

        rollEndTriggered = true;
        combatSystem.InAction = false;
        isRolling = false;
        if (!UIStateManager.IsAnyUIActive) isMovementEnabled = true;

        fadeRollCoroutine = StartCoroutine(FadeRollLayerWeight());
    }

    private IEnumerator FadeRollLayerWeight()
    {
        float startWeight = animator.GetLayerWeight(rollLayerIndex);
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            animator.SetLayerWeight(rollLayerIndex, Mathf.Lerp(startWeight, 0f, elapsed / duration));
            yield return null;
        }
        animator.SetLayerWeight(rollLayerIndex, 0f);
        fadeRollCoroutine = null;
    }

    private void OnDestroy()
    {
        UIStateManager.OnUIActiveStateChanged -= OnUIActiveStateChanged;
        if (InputManager.Instance != null)
            InputManager.Instance.OnQuickItemUse -= OnQuickItemUse;
        if (PlayerHUDUI.Instance != null)
        {
            PlayerHUDUI.Instance.UnregisterPlayerComponents();
        }
    }

    public void ResetMovementBase()
    {
        InputDir = transform.forward;
    }
    public void OnLandBegin()
    {
        isMovementEnabled = false;
        LockRotation = true;  // ������ת
        Debug.Log("Landing Begin, InAction set to true.");
    }
    public void OnLandComplete()
    {
         isMovementEnabled = true;
        LockRotation = false;  // ������ת
        Debug.Log("Landing completed, InAction set to false.");
    }
    private Quaternion GetCameraPlanarRotation()
    {
        return cameraController != null ? cameraController.GetPlanarRotation() : Quaternion.identity;
    }

  
    

    private void LateUpdate()
    {
        if (cameraController != null)
        {
            cameraController.SetLookInput(lookInput);
        }
        // �ж�Ŀ��߶�
        float targetHeight = isCrouching || !headChecker.CanStandUpFromCrouch()
            ? crouchHeight
            : standHeight;

        // ƽ������ CharacterController �߶�
        if (Mathf.Abs(charactercontroller.height - targetHeight) > 0.01f)
        {
            float oldHeight = charactercontroller.height;
            float newHeight = Mathf.SmoothDamp(
                charactercontroller.height,
                targetHeight,
                ref currentHeightVelocity,
                0.1f
            );

            float heightDelta = newHeight - oldHeight;
            Vector3 curCenter = charactercontroller.center;
            curCenter.y += heightDelta * 0.5f;
            charactercontroller.center = curCenter;

            charactercontroller.height = newHeight;

            if (cameraController != null)
                cameraController.SetCameraHeight(charactercontroller.center.y, true);
        }

        // Lock-on: push camera target back when near enemy to prevent overhead view
        if (cameraController != null)
            cameraController.UpdateLockCameraDistance();
    }

    private void OnQuickItemUse()
    {
        // 只在 Player 模式下响应，UI 打开时不触发
        if (UIStateManager.IsAnyUIActive) return;
        if (!isGrounded || isDrinking) return;
        if (combatSystem.InAction || isRolling) return;

        ItemSO item = QuickItemBar.Instance?.CurrentItem;
        if (item == null || item.itemType != ItemType.Consumable) return;

        // 消耗物品数量
        if (item.IsStackable() && item.amount > 1)
        {
            item.amount -= 1;
            InventoryUI.Instance?.UpdateItemAmountDisplay(item);
            QuickItemBar.Instance?.RefreshView();
        }
        else
        {
            // 最后一个 → 从背包移除，触发 OnItemRemoved → QuickItemBar 被动清槽
            InventoryManager.Instance?.RemoveItem(item, 1);
        }

        // 播放喝药动画
        PlayerProperty.Instance.UseDrag(item);
        isDrinking = true;
    }

    public void OnDrinkAnimationComplete()
    {
        isDrinking = false;
    }
   
    public float RotationSpeed => rotationSpeed;

}