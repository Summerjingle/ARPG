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

    public Vector3 InputDir { get; private set; }
    public static PlayerController i { get; private set; }

    Quaternion targetRotation;
    MeleeFighter meeleFighter;
    private HealthSystem healthSystem;

    private CameraController cameracontroller;
    public Animator animator;
    private CharacterController charactercontroller;
    public CombatController combatController;
    public bool isGrounded;
    private float ySpeed;
    private bool isMovementEnabled = true;
    private bool isRolling = false;

    private void Awake()
    {
        i = this;
        cameracontroller = Camera.main.GetComponent<CameraController>();//找到场景中的主摄像机，并获取其身上的cameracontroller组件
        animator = GetComponent<Animator>();//从脚本负载对象身上获取其动画控制机
        charactercontroller = GetComponent<CharacterController>();//从脚本负载对象身上获取其charactercontroller，目的在于通过该组件控制对象移动
        meeleFighter = GetComponent<MeleeFighter>();
        combatController = GetComponent<CombatController>();
        healthSystem = GetComponent<HealthSystem>();
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
        UIStateManager.SetUIActive(false);
    }
    private void Update()
    {
        if (meeleFighter.InAction || isRolling)
        {
            // 可以保留翻滚结束检测，或者移到协程中
            return;
        }

        // 翻滚输入检测
        if (!UIStateManager.IsAnyUIActive && Input.GetKeyDown(KeyCode.Space) &&
           !meeleFighter.InAction && isGrounded && !isRolling)
        {
            StartRoll();
            return;
        }

        if (!isMovementEnabled ||
            (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive))
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
            return;
        }

        if (meeleFighter.InAction)
        {
            targetRotation = transform.rotation;
            animator.SetFloat("forwardSpeed", 0f);
            ySpeed = 0;
            return;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float moveAmount = Mathf.Clamp01(MathF.Abs(h) + MathF.Abs(v));

        var moveInput = (new Vector3(h, 0, v)).normalized;

        var moveDir = cameracontroller.PlanarRotation * moveInput;

        InputDir = moveDir;

        GroundCheck();//在一帧中多次检测玩家是否处于着地状态
        if (isGrounded)
        {
            ySpeed = -0.5f;//如果在地面，则赋予一个黏在地上的力
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;//如果不在地面，则赋予下落的重力
        }

        var velocity = moveDir * moveSpeed;

        if (combatController.CombatMode)
        {
            velocity /= 4;
            //在战斗状态时，玩家需要一直面对敌人
            var targetVec = combatController.TargetEnemy.transform.position - transform.position;
            targetVec.y = 0f;
            if (moveAmount > 0)
            {
                targetRotation = Quaternion.LookRotation(targetVec);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            //分割玩家速率
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);

            animator.SetFloat("forwardSpeed", forwardSpeed / moveSpeed, 0.2f, Time.deltaTime);

            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);

            float Stradespeed = Mathf.Sin(angle * Mathf.Deg2Rad);
            animator.SetFloat("strafeSpeed", Stradespeed, 0.2f, Time.deltaTime);
        }
        else
        {
            if (moveAmount > 0)
            {
                targetRotation = Quaternion.LookRotation(moveDir);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            animator.SetFloat("forwardSpeed", moveAmount, 0.2f, Time.deltaTime);
        }
        velocity.y = ySpeed;
        charactercontroller.Move(velocity * Time.deltaTime);//通过CharacteController来控制玩家移动
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
        if (meeleFighter != null)
        {
            meeleFighter.InAction = true;
        }

        animator.SetFloat("forwardSpeed", 0f);
        animator.SetFloat("strafeSpeed", 0f);

        // 翻滚时临时退出战斗模式
        if (combatController != null && combatController.CombatMode)
        {
            combatController.CancelCombatForDodge();
        }

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
        if (meeleFighter != null)
        {
            meeleFighter.InAction = false;
        }

        if (!UIStateManager.IsAnyUIActive)
        {
            isMovementEnabled = true;
        }

        // 翻滚结束后的战斗模式恢复
        if (combatController.TargetEnemy != null &&
            !combatController.CombatMode &&
            Vector3.Distance(transform.position, combatController.TargetEnemy.transform.position) < 10f)
        {
            combatController.CombatMode = true;
            combatController.TargetEnemy.MeshHighlighter?.HighlightMesh(true);
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
}