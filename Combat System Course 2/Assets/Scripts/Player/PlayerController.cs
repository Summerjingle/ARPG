    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class PlayerController : MonoBehaviour
    {

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
    private bool firstFrameAfterLoad = true;

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

            // 翻滚输入检测
            if (!UIStateManager.IsAnyUIActive && Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isRolling)
            {
                StartRoll();
                return;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 moveInput = new Vector3(h, 0, v).normalized;
            float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

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
            Vector3 velocity = moveDir * moveSpeed;

            if (isLockedOn && lockedTargetDir.sqrMagnitude > 0.001f)
            {
                // --- LockOnMode 
                velocity /= 3f; // 缓慢移动（原来是 /4f）

                var targetVec = lockedTargetDir;
                targetRotation = Quaternion.LookRotation(lockedTargetDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // --- 动画同步 ---
                // forwardSpeed 除以实际速度 * 原来比例，保持动画匹配移动
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
                animator.SetFloat("forwardSpeed", moveAmount, 0.2f, Time.deltaTime);
                animator.SetFloat("strafeSpeed", 0f, 0.2f, Time.deltaTime);
            }

            // --- CharacterController 移动 ---
            velocity.y = ySpeed;
            charactercontroller.Move(velocity * Time.deltaTime);
        }


    private void LateUpdate()
    {
        if (UIStateManager.IsAnyUIActive) return;

        if (firstFrameAfterLoad)
        {
            // 第一次 LateUpdate，直接同步位置和旋转
            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.position = transform.position + new Vector3(0, 1.41f, 0); // 你的肩膀偏移
                CinemachineCameraTarget.transform.rotation = transform.rotation;
            }

            // 刷新 CharacterController 内部状态
            if (charactercontroller != null)
                charactercontroller.Move(Vector3.zero);

            firstFrameAfterLoad = false;
            return; // 第一帧不处理鼠标旋转，避免偏差
        }

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

        private void StartRoll()
        {
            isRolling = true;
            isMovementEnabled = false;

            // 关键：像攻击一样设置 InAction
            if (combatSystem != null)
            {
                combatSystem.InAction = true;
            }

            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);


            // 直接使用InputDir，没有输入就保持原方向
            if (InputDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(InputDir.normalized);
            }

            animator.Play("Rolling");

            // 启动协程控制翻滚过程（像攻击那样）
            StartCoroutine(PerformRoll());
        }
        private IEnumerator PerformRoll()
        {
            float rollDistance = 5.5f; // 翻滚距离
            float rollDuration = 0.75f; // 翻滚持续时间
            float rollSpeed = rollDistance / rollDuration; // 计算翻滚速度

            Vector3 rollDirection = InputDir != Vector3.zero ? InputDir.normalized : transform.forward;
            Vector3 startPosition = transform.position;
            float timer = 0f;

            // 翻滚移动循环
            while (timer < rollDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / rollDuration;

                // 计算当前位置（可以使用曲线让移动更自然）
                Vector3 targetPosition = startPosition + rollDirection * rollDistance;
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);

                // 使用CharacterController移动
                Vector3 moveDelta = newPosition - transform.position;
                charactercontroller.Move(moveDelta);

                yield return null;
            }

            // 恢复状态
            isRolling = false;
            if (combatSystem != null)
            {
                combatSystem.InAction = false;
            }

            if (!UIStateManager.IsAnyUIActive)
            {
                isMovementEnabled = true;
            }

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
    }