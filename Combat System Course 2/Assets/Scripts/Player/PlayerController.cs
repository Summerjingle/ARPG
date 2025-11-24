    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class PlayerController : MonoBehaviour
    {

        [Header("Movement Settings")]
        [SerializeField] private float walkSpeedMultiplier = 0.5f; // 走路速度乘数
        private float currentRunBlend = 0f;

        [Header("Armed Mode")]
        [SerializeField] private bool Armed = false;
        [SerializeField] private KeyCode armedToggleKey = KeyCode.Tab;

        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float rotationSpeed = 500f;
        [SerializeField] float groundCheckRadius = 0.2f;
        [SerializeField] Vector3 groundCheckOffset;
        [SerializeField] LayerMask groundLayer;
        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget; // 摄像机跟随目标

        public float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        public Vector3 InputDir { get; private set; }
        public static PlayerController i { get; private set; }
        public bool LockRotation { get; set; } = false;


        public Quaternion targetRotation;
        public ICombatSystem combatSystem;

        public Animator animator;
        private CharacterController charactercontroller;
        public CombatController combatController;
        public bool isGrounded;
        private float ySpeed;
        public bool isMovementEnabled = true;
        public bool isRolling = false;

        [HideInInspector] public bool isLockedOn = false;
        [HideInInspector] public Vector3 lockedTargetDir;
         
    private Vector3 lastPlayerPos;


    private void Awake()
        {
            i = this;
            animator = GetComponent<Animator>();//从脚本负载对象身上获取其动画控制机
            charactercontroller = GetComponent<CharacterController>();//从脚本负载对象身上获取其charactercontroller，目的在于通过该组件控制对象移动
            combatSystem = GetComponent<ICombatSystem>();
            StartCoroutine(DelayedRegistration());
            RegisterToHUD();
            UIStateManager.OnUIActiveStateChanged += OnUIActiveStateChanged;
        }
        private IEnumerator DelayedRegistration()
        {
            // 等待 HUD 实例化
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
        lastPlayerPos = transform.position;



        // 初始化摄像机旋转角度
        if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }
            UIStateManager.SetUIActive(false);
        if (CinemachineCameraTarget != null)
        {
            CinemachineCameraTarget.transform.rotation = transform.rotation;
            _cinemachineTargetYaw = transform.rotation.eulerAngles.y;
            _cinemachineTargetPitch = 0;
        }
    }
        private void Update()
        {

            if (combatSystem.InAction) return;
            if (!isMovementEnabled || (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
            {
                animator.SetFloat("forwardSpeed", 0f);
                animator.SetFloat("strafeSpeed", 0f);
                return;
            }
        // 武装模式切换输入检测
        if (!UIStateManager.IsAnyUIActive && Input.GetKeyDown(armedToggleKey)&& !isRolling)
        {
            ToggleArmedMode();
        }

        // 翻滚输入检测
        if (!UIStateManager.IsAnyUIActive && Input.GetKeyDown(KeyCode.Space) && isGrounded && !isRolling)
            {
                StartRoll();
                return;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 moveInput = new Vector3(h, 0, v).normalized;
            float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

            bool targetRunning = Input.GetKey(KeyCode.LeftShift) && moveAmount > 0.1f && !isRolling;
            currentRunBlend = Mathf.MoveTowards(currentRunBlend, targetRunning ? 1f : 0f, Time.deltaTime * 5f);
            float currentMoveSpeed = Mathf.Lerp(moveSpeed * walkSpeedMultiplier, moveSpeed, currentRunBlend);
        
        // --- 移动方向 ---
        Vector3 moveDir;
            if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
            {
                // 锁定敌人时，以锁定方向为基准
                Vector3 right = Vector3.Cross(Vector3.up, lockedTargetDir);
                moveDir = lockedTargetDir * moveInput.z + right * moveInput.x;
            }
            else
            {
                // 自由移动
                moveDir = GetCameraPlanarRotation() * moveInput;
            }

            InputDir = moveDir.normalized;

            // --- GroundCheck ---
            GroundCheck();
            if (!isGrounded) ySpeed += Physics.gravity.y * Time.deltaTime;
            else ySpeed = 0f;

        // --- velocity ---
        Vector3 velocity = moveDir * currentMoveSpeed;

        // --- 动画同步 ---
        if (Armed)
        {
            // --- ArmedMode: 使用 StrafeSpeed 和 ForwardSpeed ---
            Vector3 localVelocity = transform.InverseTransformDirection(velocity);

            // 根据你的动画配置设置参数
            float forwardSpeed = localVelocity.z / moveSpeed; // 标准化到 -1 到 1 范围
            float strafeSpeed = localVelocity.x / moveSpeed;  // 标准化到 -1 到 1 范围

            if (Mathf.Abs(forwardSpeed) > 0.1f)
                forwardSpeed = Mathf.Sign(forwardSpeed) * Mathf.Lerp(0.2f, 1.0f, currentRunBlend);

            if (Mathf.Abs(strafeSpeed) > 0.1f)
                strafeSpeed = Mathf.Sign(strafeSpeed) * Mathf.Lerp(0.2f, 1.0f, currentRunBlend);
            animator.SetFloat("forwardSpeed", forwardSpeed, 0.2f, Time.deltaTime);
            animator.SetFloat("strafeSpeed", strafeSpeed, 0.2f, Time.deltaTime);

            // 武装模式下保持面向移动方向
            if (moveAmount > 0 && !LockRotation)
            {
                targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
        {
            // --- LockOnMode (保持你现有的逻辑) ---
            float baseSpeed = Mathf.Lerp(moveSpeed * walkSpeedMultiplier, moveSpeed, currentRunBlend);
            velocity /= 3f;

            var targetVec = lockedTargetDir;
            targetRotation = Quaternion.LookRotation(lockedTargetDir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            animator.SetFloat("forwardSpeed", forwardSpeed / (moveSpeed / 2f), 0.2f, Time.deltaTime);

            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            animator.SetFloat("strafeSpeed", Mathf.Sin(angle * Mathf.Deg2Rad), 0.2f, Time.deltaTime);
        }
        else
        {
            // 自由移动
            if (moveAmount > 0 && !LockRotation)
            {
                targetRotation = Quaternion.LookRotation(moveDir);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            float animationSpeed = Mathf.Lerp(moveAmount * walkSpeedMultiplier, moveAmount, currentRunBlend);
            animator.SetFloat("forwardSpeed", animationSpeed, 0.2f, Time.deltaTime);
            animator.SetFloat("strafeSpeed", 0f, 0.2f, Time.deltaTime);
        }

        // --- CharacterController 移动 ---
        velocity.y = ySpeed;
       
        charactercontroller.Move(velocity * Time.deltaTime);
        }


    private void LateUpdate()
    {
        if (UIStateManager.IsAnyUIActive) return;


        
        CameraRotation(); // 原来的 LateUpdate 摄像机逻辑
    }

    private void CameraRotation()
        {
            if (UIStateManager.IsAnyUIActive) return;

            if (LockCameraPosition)
            {
                // 相机锁定时不允许鼠标输入
                return;
            }

            // if there is an input and camera position is not fixed
            if (Input.GetAxis("Mouse X") != 0 && Input.GetAxis("Mouse Y") != 0 && !LockCameraPosition)
            {
                // Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = 1.0f; // 假设总是使用鼠标

                _cinemachineTargetYaw += Input.GetAxis("Mouse X") * deltaTimeMultiplier;
                _cinemachineTargetPitch += Input.GetAxis("Mouse Y") * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                    _cinemachineTargetYaw, 0.0f);
            }
        }
        // --- LockOn 系统用：让相机瞬间对齐某个敌人 ---
        public void LookAtTargetInstant(Transform target)
        {
            if (target == null) return;

            Vector3 dir = target.position - transform.position;
            dir.y = 0;

            // 水平角度
            _cinemachineTargetYaw = Quaternion.LookRotation(dir).eulerAngles.y;

            // 垂直角度（可根据需要算）
            _cinemachineTargetPitch = 10f;

            LockCameraPosition = true;
            targetRotation = Quaternion.LookRotation(dir);
        }

        // --- LockOn 退出时恢复自由相机 ---
        public void UnlockCamera()
        {
            LockCameraPosition = false;
        }
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        private void GroundCheck()
        {
            isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius, groundLayer);
        }
        public Vector3 GetIntentDirection()
        {
            return InputDir != Vector3.zero ? InputDir : transform.forward;
        }

        private void OnDrawGizmosSelected()//这是GIZMOS地面检测的方法
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

            // 立即停止移动动画
            if (isUIActive)
            {
                animator.SetFloat("forwardSpeed", 0f);
                animator.SetFloat("strafeSpeed", 0f);
            }

            Debug.Log($"玩家移动: {(isMovementEnabled ? "启用" : "禁用")}");
        }
    private void ToggleArmedMode()
    {
        Armed = !Armed;

        // 更新动画器参数
        animator.SetBool("Armed", Armed);

        // 重置移动参数以确保平滑过渡
        if (!Armed)
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
        }

        Debug.Log($"武装模式: {(Armed ? "开启" : "关闭")}");
    }

    // 公共方法供其他系统调用
    public void SetArmedMode(bool armed)
    {
        if (Armed != armed)
        {
            ToggleArmedMode();
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        isMovementEnabled = false;

        if (combatSystem != null)
        {
            combatSystem.InAction = true;
        }

        animator.SetFloat("forwardSpeed", 0f);
        animator.SetFloat("strafeSpeed", 0f);
        string rollAnimation = Armed ? "ArmedRoll" : "Rolling";
        animator.Play(rollAnimation);


        StartCoroutine(PerformRoll());
    }
    private IEnumerator PerformRoll()
    {
        float rollDistance = 5.5f;
        float rollDuration = 0.75f;
        float rollSpeed = rollDistance / rollDuration;

       
        Vector3 rollDirection = transform.forward;
        rollDirection.y = 0;
        rollDirection.Normalize();

        Vector3 startPosition = transform.position;
        float timer = 0f;

        while (timer < rollDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / rollDuration;

            Vector3 targetPosition = startPosition + rollDirection * rollDistance;
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);

            Vector3 moveDelta = newPosition - transform.position;
            charactercontroller.Move(moveDelta);

            yield return null;
        }

        isRolling = false;
        if (combatSystem != null) combatSystem.InAction = false;

        if (!UIStateManager.IsAnyUIActive)
            isMovementEnabled = true;
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
            // 清除任何可能缓存的方向数据
            InputDir = transform.forward; // 重置为当前面对方向


        }
        private Quaternion GetCameraPlanarRotation()
        {
            if (CinemachineCameraTarget != null)
            {
                Vector3 cameraForward = CinemachineCameraTarget.transform.forward;
                cameraForward.y = 0; // 只取水平方向
                return Quaternion.LookRotation(cameraForward.normalized);
            }
            return Quaternion.identity;
        }
   
        public float RotationSpeed => rotationSpeed;
        public Quaternion PlanarRotation => GetCameraPlanarRotation();
        public bool IsArmed => Armed;
}