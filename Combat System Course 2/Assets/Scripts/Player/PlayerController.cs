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
    private Vector2 lookInput; // 原始鼠标/摇杆输入
    public Vector2 LookInput => lookInput;
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeedMultiplier = 0.5f; // 走路速度乘数
    [SerializeField] private float crouchSpeedMultiplier = 0.3f; // 蹲走速度（30%）
    private bool shouldCrouch;      // 实际蹲下状态
    private float currentRunBlend = 0f;
    private bool isCrouching = false;
    [SerializeField] LayerMask obstacleLayer; // 只检测障碍物



    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float standHeight = 1.8f;
    private float currentHeightVelocity;  // 新增：用于平滑过渡
    [SerializeField] private float fallStartDelay = 0.15f; // 离地多久才算下落
    [SerializeField] private float minFallSpeed = -6f;     // 真正下落的速度阈值

    private float notGroundedTimer = 0f;




    private bool isSprinting = false; // 当前是否正在冲刺（消耗能量）

    [SerializeField] private bool Armed = false;
    private bool isFalling = false;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 500f;


    public PlayerCameraController cameraController;

    public Vector3 InputDir { get; private set; }
    public static PlayerController i { get; private set; }
    public bool LockRotation { get; set; } = false;

    public Quaternion targetRotation;
    public ICombatSystem combatSystem;

    public Animator animator;
    private int speedHash;
    private int dirXHash;
    private int dirYHash;
    [Header("Locomotion Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float sprintSpeed = 9.5f;
    private CharacterController charactercontroller;
    private ParkourController parkourController;
    public CombatController combatController;
    private HeadCollisionChecker headChecker;
    public bool isGrounded;
    private bool isDrinking;
    private float ySpeed;
    public bool isMovementEnabled = true;
    public bool isRolling = false;
    [SerializeField] private float rollCooldown = 0.8f; // 冷却时间稍长于翻滚持续时间
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
        
    }

    private void Update()
    {
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        crouchHeld = inputActions.Player.Crouch.IsPressed();
        if (inputActions.Player.Roll.WasPressedThisFrame())
        {
            rollRequested = true;
        }
        sprintHeld = inputActions.Player.Sprint.IsPressed();
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (isGrounded && !isDrinking && !isRolling && Input.GetKeyDown(KeyCode.Alpha1))//在地上，还没喝，没滚
        {
            PlayerProperty.Instance.UseDrag(testHealthPotion);
            isDrinking = true;
        }

        if (combatSystem.InAction) return;


        if (!isMovementEnabled || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            animator.SetFloat(speedHash, 0f);
            animator.SetFloat(dirXHash, 0f);
            animator.SetFloat(dirYHash, 0f);
            return;
        }

        // ====================== 空格输入，可以攀爬？攀爬：翻滚 ======================
        if (rollRequested
      && !isDrinking
      && !UIStateManager.IsAnyUIActive
      && isGrounded
      && !isRolling)
        {
            rollRequested = false; // 消费输入

            // 尝试攀爬
            if (parkourController != null && parkourController.TryClimb())
                return;

            // 再尝试翻滚
            if (Time.time >= lastRollTime + rollCooldown)
            {
                StartRoll();
                return;
            }
        }
        // ─────────────── 输入处理 ───────────────
        Vector2 rawInput = moveInput;
        rawInput = Vector2.ClampMagnitude(rawInput, 1f);
        float inputMagnitude = rawInput.magnitude;       // 0 ~ 1

        // DirX / DirY（本地相对方向）
        Vector3 cameraForward = GetCameraPlanarRotation() * Vector3.forward;
        Vector3 cameraRight = GetCameraPlanarRotation() * Vector3.right;
        Vector3 worldMoveDir = (cameraRight * rawInput.x + cameraForward * rawInput.y).normalized;
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);

        animator.SetFloat(dirXHash, localMoveDir.x, 0.15f, Time.deltaTime);
        animator.SetFloat(dirYHash, localMoveDir.z, 0.15f, Time.deltaTime);

        // ─────────────── Speed 计算 ───────────────
        // 核心：用 inputMagnitude 直接映射到 0 ~ 2.0 左右区间
        // 让轻推 → Walk，重推 → Run
        float animSpeed = inputMagnitude * 2.0f;   // 0 → 0, 0.3 → 0.6, 0.8 → 1.6, 1.0 → 2.0

        // Sprint 覆盖（最高优先级）
        bool wantsSprint = sprintHeld && inputMagnitude > 0.1f && !isRolling && !combatSystem.InAction;
        bool allowSprint = rawInput.y > -0.25f;  // 后退禁止
        bool canSprint = wantsSprint && allowSprint && PlayerProperty.Instance.EnergyValue > 15;

        if (canSprint)
        {
            animSpeed = inputMagnitude * 3.8f;   // 0.38 ~ 3.8 → 强制进入 Sprint (阈值 2.5+)
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        // 能量消耗
        if (isSprinting)
        {
            float costPerSecond = PlayerProperty.Instance.GetSprintCostPerSecond();
            if (!PlayerProperty.Instance.ConsumeEnergy(Mathf.CeilToInt(costPerSecond * Time.deltaTime)))
            {
                isSprinting = false;
                animSpeed = inputMagnitude * 2.0f;  // 能量不够 → 回到 Run 区间
            }
        }

        // 平滑设置
        animator.SetFloat(speedHash, animSpeed, 0.18f, Time.deltaTime);

        // ─────────────── 实际移动速度 ───────────────
        float currentMoveSpeed;

        if (shouldCrouch || isCrouching)
        {
            currentMoveSpeed = walkSpeed * crouchSpeedMultiplier;
        }
        else if (isSprinting)
        {
            currentMoveSpeed = sprintSpeed * inputMagnitude;
        }
        else
        {
            // Walk → Run 丝滑过渡
            float t = Mathf.InverseLerp(0.15f, 1.0f, inputMagnitude);
            currentMoveSpeed = Mathf.Lerp(walkSpeed, runSpeed, t);
        }

        // moveInput3D 继续给后面用
        Vector3 moveInput3D = worldMoveDir;
        float moveAmount = inputMagnitude;
        // ====================== 移动方向 ======================
        Vector3 moveDir;
        if (isLockedOn)
        {
            Vector3 lockedDir = cameraController != null ? cameraController.GetLockedDirection() : lockedTargetDir;

            if (lockedDir.sqrMagnitude > 0.001f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, lockedDir);
                moveDir = lockedDir * rawInput.y + right * rawInput.x;
            }
            else
            {
                moveDir = worldMoveDir;
            }
        }
        else
        {
            moveDir = worldMoveDir;
        }

        // ====================== 重力逻辑(2026/1/20 更新 ) ======================

        isGrounded = charactercontroller.isGrounded;//先前使用Physics.CheckSphere来检测是否着地，现在使用characterrcontroller提供的API进行检测  2026/1/20

        //重力逻辑
        if (isGrounded)
        {
            if (ySpeed < 0f)
            {
                ySpeed = -1.5f;//若已经着地，赋予一个轻微的向下的力，是角色贴合地面，避免不自然的运动表现（注意：-1.5f过大？斜坡处理？）2026/1/20
            }
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;// 若没有落地，则基于向下的速度=初速度（ySpeed）+重力加速度（Physics.gravity.y）*时间（Time.deltaTime）2026/1/20
        }
        // 记录离地时间
        if (!isGrounded)
        {
            notGroundedTimer += Time.deltaTime;
        }
        else
        {
            notGroundedTimer = 0f;
        }

        // 进入 Falling
        if (!isGrounded
            && notGroundedTimer > fallStartDelay
            && ySpeed < minFallSpeed
            && !isFalling
            && !isRolling)
        {
            isFalling = true;
            animator.SetBool("Falling", true);
        }

        // 落地
        else if (isGrounded && isFalling)
        {
            isFalling = false;
            animator.SetBool("Falling", false);
            animator.Play("Land", -1, 0f);
        }

        // ====================== 移动逻辑 ======================
        Vector3 velocity = moveDir * currentMoveSpeed;

        if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
        {
            //  锁定状态[最高优先级]
            velocity /= 3f;  // 减速

            targetRotation = Quaternion.LookRotation(lockedTargetDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);

        }
        else if (Armed)
        {
            //  非锁定+装备武器 
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);
            float forwardSpeed = localVelocity.z / moveSpeed;
            float strafeSpeed = localVelocity.x / moveSpeed;

            if (Mathf.Abs(forwardSpeed) > 0.1f)
                forwardSpeed = Mathf.Sign(forwardSpeed) * Mathf.Lerp(0.2f, 1.0f, currentRunBlend);
            if (Mathf.Abs(strafeSpeed) > 0.1f)
                strafeSpeed = Mathf.Sign(strafeSpeed) * Mathf.Lerp(0.2f, 1.0f, currentRunBlend);



            if (moveAmount > 0 && !LockRotation)
                targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // 非锁定+非装备武器 
            if (moveAmount > 0 && !LockRotation)
                targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);


            // ====================== 应用移动 ======================
            velocity.y = ySpeed;

            //爬梯子逻辑
            Vector3 rayOrigin =
             transform.position + Vector3.up * 0.4f + transform.forward * (charactercontroller.radius + 0.05f);

            Vector3 rayDir = transform.forward;

            Debug.DrawRay(rayOrigin, rayDir * 0.4f, Color.red);

            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, 0.4f))
            {
                if (hit.transform.TryGetComponent<Ladder>(out Ladder ladder))
                {
                    Debug.Log("碰到梯子了");

                    // 梯子状态下直接向上移动
                    float climbSpeed = 3f;
                    velocity = Vector3.up * climbSpeed;

                    // 阻止重力
                    ySpeed = 0f;
                    isGrounded = true;
                }
            }
            if (isGrounded)
            {
                ySpeed = -1f; // 只保留轻微负值
            }

            charactercontroller.Move(velocity * Time.deltaTime);
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

        // 计算翻滚方向
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

        // 无输入翻滚
        if (wishDir.sqrMagnitude < 0.1f)
        {
            wishDir = isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f
                ? lockedTargetDir
                : transform.forward;
        }

        wishDir.y = 0;
        wishDir = wishDir.normalized;

        //有输入-翻滚前转向
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
            transform.rotation = targetRot; // 确保完全对齐
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
        LockRotation = true;  // 锁定旋转
        Debug.Log("Landing Begin, InAction set to true.");
    }
    public void OnLandComplete()
    {
         isMovementEnabled = true;
        LockRotation = false;  // 解锁旋转
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
        // 判断目标高度
        float targetHeight = isCrouching || !headChecker.CanStandUpFromCrouch()
            ? crouchHeight
            : standHeight;

        // 平滑调整 CharacterController 高度
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