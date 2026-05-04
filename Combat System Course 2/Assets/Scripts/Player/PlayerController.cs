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
    private bool rollRequested;
    private bool crouchHeld;
    private Vector2 lookInput; // ԭʼ���/ҡ������
    public Vector2 LookInput => lookInput;
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeedMultiplier = 0.5f; // ��·�ٶȳ���
    [SerializeField] private float crouchSpeedMultiplier = 0.3f; // �����ٶȣ�30%��
    private bool shouldCrouch;      // ʵ�ʶ���״̬
    private float currentRunBlend = 0f;
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

    [SerializeField] private bool Armed = false;
    private bool isFalling = false;
    [SerializeField] float moveSpeed = 5f;
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
    public bool isRolling = false;
    [SerializeField] private float rollCooldown = 0.8f; // ��ȴʱ���Գ��ڷ�������ʱ��
    private float lastRollTime = -Mathf.Infinity;
    private Coroutine currentRollCoroutine;

    [HideInInspector] public bool isLockedOn = false;
    [HideInInspector] public Vector3 lockedTargetDir;
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
        speedHash = Animator.StringToHash("Speed");
        dirXHash = Animator.StringToHash("DirX");
        dirYHash = Animator.StringToHash("DirY");
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
        if (inputActions.Player.Roll.WasPressedThisFrame())
        {
            rollRequested = true;
        }
        sprintHeld = inputActions.Player.Sprint.IsPressed();
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // 喝药逻辑
        if (isGrounded && !isDrinking && !isRolling && Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayerProperty.Instance.UseDrag(testHealthPotion);
            isDrinking = true;
        }

       
        if (!isMovementEnabled || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            animator.SetFloat(speedHash, 0f);
            // 如果不再用 DirX/DirY，下面两行可以删掉
            animator.SetFloat(dirXHash, 0f);
            animator.SetFloat(dirYHash, 0f);
            return;
        }

        // ─────────────── 攀爬与翻滚输入 ───────────────
        if (rollRequested && !isDrinking && !UIStateManager.IsAnyUIActive && isGrounded && !isRolling)
        {
            rollRequested = false;
            if (parkourController != null && parkourController.TryClimb()) return;
            if (Time.time >= lastRollTime + rollCooldown)
            {
                StartRoll();
                return;
            }
        }

        // ─────────────── 移动向量计算 ───────────────
        Vector2 rawInput = Vector2.ClampMagnitude(moveInput, 1f);
        float inputMagnitude = rawInput.magnitude;

        Vector3 cameraForward = GetCameraPlanarRotation() * Vector3.forward;
        Vector3 cameraRight = GetCameraPlanarRotation() * Vector3.right;
        Vector3 worldMoveDir = (cameraRight * rawInput.x + cameraForward * rawInput.y).normalized;

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
        bool wantsSprint = sprintHeld && inputMagnitude > 0.1f && !isRolling && !combatSystem.InAction;
        bool allowSprint = rawInput.y > -0.25f; //这个条件限制玩家只能朝着前面冲刺
        bool canSprint = wantsSprint && allowSprint && PlayerProperty.Instance.EnergyValue > 15;

        if (combatSystem.InAction)
        {
            animSpeed = 0f;
            isSprinting = false;
        }
        else if (canSprint)
        {
            animSpeed = 1.99f;
            isSprinting = true;
        }
        else if (inputMagnitude > 0.01f)
        {
            isSprinting = false;
            animSpeed = Mathf.Lerp(0.01f, 1.0f, accelerationT);
        }
        else
        {
            isSprinting = false;
            animSpeed = 0f;
        }
        animator.SetFloat(speedHash, animSpeed, damping, Time.deltaTime);

        // 能量消耗
        if (isSprinting)
        {
           float costPerSecond = PlayerProperty.Instance.GetSprintCostPerSecond();
            // 移除 Mathf.CeilToInt，直接传入 float 进行精确计算
            if (!PlayerProperty.Instance.ConsumeEnergy(costPerSecond * Time.deltaTime))
            {
                isSprinting = false;
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
            if (shouldCrouch || isCrouching) targetMoveSpeed = walkSpeed * crouchSpeedMultiplier;
            else if (isSprinting) targetMoveSpeed = sprintSpeed * inputMagnitude;
            else targetMoveSpeed = Mathf.Lerp(walkSpeed, runSpeed, accelerationT) * inputMagnitude;
        }

        // 平滑过渡到目标速度
        currentPhysicalSpeed = Mathf.SmoothDamp(currentPhysicalSpeed, targetMoveSpeed, ref speedSmoothVelocity, speedSmoothTime);

        // ─────────────── 旋转与重力逻辑 ───────────────
        Vector3 moveDir = isLockedOn ? (cameraController != null ? cameraController.GetLockedDirection() : worldMoveDir) : worldMoveDir;

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
        if (!isGrounded && notGroundedTimer > fallStartDelay && ySpeed < minFallSpeed && !isFalling && !isRolling)
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
        Vector3 velocity = moveDir * currentPhysicalSpeed;

        // 转向逻辑
        if (inputMagnitude > 0 && !LockRotation)
        {
            targetRotation = Quaternion.LookRotation(moveDir);
        }
        float activeRotSpeed = combatSystem.InAction ? rotationSpeed * 0.5f : rotationSpeed;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, activeRotSpeed * Time.deltaTime);

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
        if (!animator.applyRootMotion)
        {
            charactercontroller.Move(velocity * Time.deltaTime);
        }
        if (!isSprinting && !isRolling && PlayerProperty.Instance.energyValue < PlayerProperty.Instance.MaxEnergy)
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
        if (combatSystem != null && combatSystem.InAction && animator != null && animator.applyRootMotion)
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
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
        }
    }

    private void ToggleArmedMode()
    {
        Armed = !Armed;
        animator.SetBool("Armed", Armed);

        if (!Armed)
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
        }
    }

    public void SetArmedMode(bool armed)
    {
        if (Armed != armed)
        {
            ToggleArmedMode();
        }
    }

    private void StartRoll()
    {
        if (Input.GetKey(KeyCode.LeftControl)) return;
        if (isRolling || Time.time < lastRollTime + rollCooldown)
        {
            return;
        }

        if (PlayerProperty.Instance == null)
        {
           
            return;
        }

        int rollCost = PlayerProperty.Instance.GetRollEnergyCost();
        

        if (!PlayerProperty.Instance.ConsumeEnergy(rollCost))
        {
            
            return;  
        }


        isRolling = true;
        isMovementEnabled = false;
        lastRollTime = Time.time;

        InputDir = Vector3.zero;

        if (combatSystem != null)
        {
            combatSystem.InAction = true;
        }

        animator.SetFloat("forwardSpeed", 0f);
        animator.SetFloat("strafeSpeed", 0f);

        string rollAnimation = Armed ? "ArmedRoll" : "Rolling";
        animator.Play(rollAnimation);

        if (currentRollCoroutine != null)
        {
            StopCoroutine(currentRollCoroutine);
        }

        currentRollCoroutine = StartCoroutine(PerformRoll());
    }

    private IEnumerator PerformRoll()
    {
        float rollDistance = 5.5f;
        float rollDuration = 0.75f;

        // ���㷭������
        float h = moveInput.x;
        float v = moveInput.y;
        Vector3 wishDir;

        if (isLockedOn)
        {
            Vector3 lockedDir = cameraController != null
                ? cameraController.GetLockedDirection()
                : lockedTargetDir;

            if (lockedDir.sqrMagnitude > 0.001f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, lockedDir);
                wishDir = lockedDir * v + right * h;
            }
            else
            {
                wishDir = GetCameraPlanarRotation() * new Vector3(h, 0, v);
            }
        }
        else
        {
            wishDir = GetCameraPlanarRotation() * new Vector3(h, 0, v);
        }

        // �����뷭��
        if (wishDir.sqrMagnitude < 0.1f)
        {
            wishDir = isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f
                ? lockedTargetDir
                : transform.forward;
        }

        wishDir.y = 0;
        wishDir = wishDir.normalized;

        //������-����ǰת��
        if (wishDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(wishDir);
            float turnTime = 0f;
            float turnDuration = 0.1f;   

            while (turnTime < turnDuration)
            {
                turnTime += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnTime / turnDuration);
                yield return null;
            }
            transform.rotation = targetRot; // ȷ����ȫ����
        }

        Vector3 rollDirection = transform.forward;  

        Vector3 startPosition = transform.position;
        float timer = 0f;

        while (timer < rollDuration)
        {
            timer += Time.deltaTime;
            float t = timer / rollDuration;

            Vector3 targetPos = startPosition + rollDirection * rollDistance;
            Vector3 newPos = Vector3.Lerp(startPosition, targetPos, t);

            charactercontroller.Move(newPos - transform.position);
            yield return null;
        }
        yield return new WaitForSeconds(0.15f);

        isRolling = false;
        if (combatSystem != null) combatSystem.InAction = false;
        if (!UIStateManager.IsAnyUIActive) isMovementEnabled = true;

        currentRollCoroutine = null;
    }
    private void OnDestroy()
    {
        UIStateManager.OnUIActiveStateChanged -= OnUIActiveStateChanged;
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
            charactercontroller.height = Mathf.SmoothDamp(
                charactercontroller.height,
                targetHeight,
                ref currentHeightVelocity,
                0.1f
            );

            

            if (cameraController != null)
                cameraController.SetCameraHeight(charactercontroller.center.y, true);
        }
    }

    public void OnDrinkAnimationComplete()
    {
        isDrinking = false;
    }
   
    public float RotationSpeed => rotationSpeed;

}