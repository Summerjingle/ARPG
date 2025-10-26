using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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

    private CameraController cameracontroller;
    public Animator animator;
    private CharacterController charactercontroller;
    public CombatController combatController;
    public bool isGrounded;
    private float ySpeed;

    // 控制是否启用玩家控制器
    private bool controllerEnabled = false;
    // 标记是否已经开始站起动画
    private bool isStandingUp = false;

    private void Awake()
    {
        i = this;

        cameracontroller = Camera.main.GetComponent<CameraController>();
        animator = GetComponent<Animator>();
        charactercontroller = GetComponent<CharacterController>();
        meeleFighter = GetComponent<MeleeFighter>();
        combatController = GetComponent<CombatController>();

        // 开始时禁用控制器
        SetControllerEnabled(false);

        StartCoroutine(DelayedRegistration());
        RegisterToHUD();
    }

    // 控制启用/禁用控制器的方法
    public void SetControllerEnabled(bool enabled)
    {
        controllerEnabled = enabled;

        // 禁用CharacterController的移动
        if (charactercontroller != null)
        {
            charactercontroller.enabled = enabled;
        }
    }

    // 检测任何输入
    private bool CheckForAnyInput()
    {
        // 检测任何按键按下（包括所有键盘按键和鼠标按键）
        return Input.anyKeyDown;
    }

    private IEnumerator DelayedRegistration()
    {
        yield return new WaitUntil(() => PlayerHUDUI.Instance != null);

        PlayerProperty property = GetComponent<PlayerProperty>();
        MeleeFighter fighter = GetComponent<MeleeFighter>();

        if (property != null && fighter != null)
        {
            PlayerHUDUI.Instance.RegisterPlayerComponents(property, fighter);
        }
    }

    private void Update()
    {
        // 如果还没有开始站起，检测任何输入
        if (!isStandingUp && !controllerEnabled)
        {
            if (CheckForAnyInput())
            {
                StartStandUpAnimation();
            }
            else
            {
                // 没有输入时保持静止
                animator.SetFloat("forwardSpeed", 0f);
                animator.SetFloat("strafeSpeed", 0f);
                return;
            }
        }

        // 如果正在站起但控制器还未启用，直接返回
        if (isStandingUp && !controllerEnabled)
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
            return;
        }

        // 如果控制器被禁用，直接返回
        if (!controllerEnabled)
        {
            animator.SetFloat("forwardSpeed", 0f);
            animator.SetFloat("strafeSpeed", 0f);
            return;
        }

        // 如果在对话中，禁用移动和旋转
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
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

        GroundCheck();
        if (isGrounded)
        {
            ySpeed = -0.5f;
        }
        else
        {
            ySpeed += Physics.gravity.y * Time.deltaTime;
        }

        var velocity = moveDir * moveSpeed;

        if (combatController.CombatMode)
        {
            velocity /= 4;
            var targetVec = combatController.TargetEnemy.transform.position - transform.position;
            targetVec.y = 0f;
            if (moveAmount > 0)
            {
                targetRotation = Quaternion.LookRotation(targetVec);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

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
        charactercontroller.Move(velocity * Time.deltaTime);
    }

    // 开始站起动画
    private void StartStandUpAnimation()
    {
        isStandingUp = true;

        // 触发站起动画
        animator.SetTrigger("StandUp");
    }

    // 动画事件方法：在站起动画的最后一帧调用
    public void OnStandUpAnimationEnd()
    {
        // 启用玩家控制器
        SetControllerEnabled(true);
        isStandingUp = false;
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
        MeleeFighter fighter = GetComponent<MeleeFighter>();

        if (PlayerHUDUI.Instance != null && property != null && fighter != null)
        {
            PlayerHUDUI.Instance.RegisterPlayerComponents(property, fighter);
        }
    }

    private void OnDestroy()
    {
        if (PlayerHUDUI.Instance != null)
        {
            PlayerHUDUI.Instance.UnregisterPlayerComponents();
        }
    }
}