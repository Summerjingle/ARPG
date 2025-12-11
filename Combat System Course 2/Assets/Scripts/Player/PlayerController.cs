using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class PlayerController : MonoBehaviour
{
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




    private bool isSprinting = false; // 当前是否正在冲刺（消耗能量）

    [SerializeField] private bool Armed = false;
    private bool isFalling = false;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 500f;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] Vector3 groundCheckOffset;
    [SerializeField] LayerMask groundLayer;

    public PlayerCameraController cameraController;

    public Vector3 InputDir { get; private set; }
    public static PlayerController i { get; private set; }
    public bool LockRotation { get; set; } = false;

    public Quaternion targetRotation;
    public ICombatSystem combatSystem;

    public Animator animator;
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
        
        isCrouching = false;
        StartCoroutine(DelayedRegistration());
        RegisterToHUD();
        UIStateManager.OnUIActiveStateChanged += OnUIActiveStateChanged;
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
        if (isGrounded && !isDrinking &&!isRolling&&Input.GetKeyDown(KeyCode.Alpha1))//在地上，还没喝，没滚
        {
            PlayerProperty.Instance.UseDrag(testHealthPotion);
            isDrinking = true;
        }
        
        if (combatSystem.InAction) return;
      

        if (!isMovementEnabled || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
            return;
        }

        // ====================== 空格输入，可以攀爬？攀爬：翻滚 ======================
        if (!isDrinking&&!UIStateManager.IsAnyUIActive && Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling)
        {
            // 先尝试执行攀爬
            if (parkourController != null && parkourController.TryClimb())
                return; // 成功攀爬 → 直接 return，不进行翻滚

            // 失败 → 允许翻滚
            if (Time.time >= lastRollTime + rollCooldown)
            {
                StartRoll();
                return;
            }
        }
        // ====================== 输入 & 能量系统 ======================
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(h, 0, v).normalized;
        float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && moveAmount > 0.1f && !isRolling;
        isSprinting = wantsToSprint && PlayerProperty.Instance.EnergyValue > 15;

        if (isSprinting)
        {
            float costPerSecond = PlayerProperty.Instance.GetSprintCostPerSecond();
            if (!PlayerProperty.Instance.ConsumeEnergy(Mathf.CeilToInt(costPerSecond * Time.deltaTime)))
                isSprinting = false;
        }
        if (PlayerProperty.Instance.EnergyValue <= 15)
        {
            currentRunBlend = 0f;
        }
        else
        {
            currentRunBlend = Mathf.MoveTowards(currentRunBlend, isSprinting ? 1f : 0f, Time.deltaTime * 5f);
        }
        if (!isSprinting && !isRolling && PlayerProperty.Instance.EnergyValue < PlayerProperty.Instance.MaxEnergy)
        {
            float regenRate = moveAmount < 0.1f ? PlayerProperty.Instance.idleRegenRate : PlayerProperty.Instance.walkRegenRate;
            PlayerProperty.Instance.RestoreEnergy(regenRate * Time.deltaTime);
        }


        float baseSpeed = Mathf.Lerp(moveSpeed * walkSpeedMultiplier, moveSpeed, currentRunBlend);

        if (isCrouching)
        {
            baseSpeed *= crouchSpeedMultiplier;
        }

        float currentMoveSpeed = baseSpeed;
        UpdateCrouchState(moveInput);
        // ====================== 移动方向 ======================
        Vector3 moveDir;
        if (isLockedOn)
        {
            Vector3 lockedDir = cameraController != null ? cameraController.GetLockedDirection() : lockedTargetDir;
            if (lockedDir.sqrMagnitude > 0.001f)
            {
                Vector3 right = Vector3.Cross(Vector3.up, lockedDir);
                moveDir = lockedDir * moveInput.z + right * moveInput.x;
            }
            else
            {
                moveDir = GetCameraPlanarRotation() * moveInput;
            }
        }
        else
        {
            moveDir = GetCameraPlanarRotation() * moveInput;
        }
        InputDir = moveDir.normalized;

        // ====================== 重力 ======================
        GroundCheck();

        ySpeed = isGrounded ? 0f : ySpeed + Physics.gravity.y * Time.deltaTime;
        if (!isGrounded && ySpeed < -5f && !isFalling && !isRolling)   // -2f 防小台阶误触
        {
            isFalling = true;
            animator.SetBool("Falling", true);
        } 
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
            animator.SetFloat("forwardSpeed", forwardSpeed / (moveSpeed / 2f), 0.2f, Time.deltaTime);

            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            animator.SetFloat("strafeSpeed", Mathf.Sin(angle * Mathf.Deg2Rad), 0.2f, Time.deltaTime);
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

            animator.SetFloat("forwardSpeed", forwardSpeed, 0.2f, Time.deltaTime);
            animator.SetFloat("strafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);

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

            float animationSpeed = Mathf.Lerp(moveAmount * walkSpeedMultiplier, moveAmount, currentRunBlend);
            animator.SetFloat("forwardSpeed", animationSpeed, 0.2f, Time.deltaTime);
            animator.SetFloat("strafeSpeed", 0f, 0.2f, Time.deltaTime);
        }

        // ====================== 应用移动 ======================
        velocity.y = ySpeed;

        //爬梯子逻辑
        Vector3 rayOrigin =
         transform.position + Vector3.up * 0.4f+ transform.forward * (charactercontroller.radius + 0.05f);

        Vector3 rayDir = transform.forward;

        Debug.DrawRay(rayOrigin, rayDir *0.4f, Color.red);

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, 0.4f))
        {
            if(hit.transform.TryGetComponent<Ladder>(out Ladder ladder)){
                Debug.Log("碰到梯子了");

                // 梯子状态下直接向上移动
                float climbSpeed = 3f;
                velocity = Vector3.up * climbSpeed;

                // 阻止重力
                ySpeed = 0f;
                isGrounded = true;
            }
        }

        charactercontroller.Move(velocity * Time.deltaTime);
    }

    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
    }

    public Vector3 GetIntentDirection()
    {
        return InputDir != Vector3.zero ? InputDir : transform.forward;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
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

    private void UpdateCrouchState(Vector3 moveInput)
    {
        bool pressingCrouch = Input.GetKey(KeyCode.LeftControl);
        if (pressingCrouch) 
        {
            headChecker.EnableHeadCheck();
        }
        else if (headChecker.CanStandUpFromCrouch())
        {
            headChecker.DisableHeadCheck();
        }
        bool canCrouchNow = isGrounded && !isRolling && !combatSystem.InAction;
        isCrouching = pressingCrouch && canCrouchNow;
        shouldCrouch = isCrouching || !headChecker.CanStandUpFromCrouch();
        
        animator.SetBool("IsCrouching", shouldCrouch);

        // 蹲下时才判断是否移动
        if (shouldCrouch)
        {
            bool moving = moveInput.sqrMagnitude > 0.01f;
            animator.SetBool("CrouchMoving", moving);
        }
        else
        {
            animator.SetBool("CrouchMoving", false);
        }
    }

    // 添加蹲下条件检查方法


    private void LateUpdate()
    {
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