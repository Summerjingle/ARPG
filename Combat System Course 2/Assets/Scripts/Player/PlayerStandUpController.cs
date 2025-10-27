using System.Collections;
using UnityEngine;

public class PlayerStandUpController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    // 控制是否启用玩家控制器
    private bool controllerEnabled = false;
    // 标记是否已经开始站起动画
    private bool isStandingUp = false;
    // 标记是否已经初始化完成
    private bool isInitialized = false;

    // 事件：当站起动画完成时
    public System.Action OnStandUpComplete;

    private void Awake()
    {
        // 获取组件引用
        if (animator == null) animator = GetComponent<Animator>();
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        // 延迟禁用，确保SaveManager能先找到玩家
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // 等待几帧，确保SaveManager和其他系统已经完成初始化
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 现在才禁用控制器
        SetControllerEnabled(false);
        isInitialized = true;

        Debug.Log("PlayerStandUpController初始化完成，控制器已禁用");
    }

    private void Start()
    {
        // 如果还没有初始化，等待初始化完成
        if (!isInitialized)
        {
            StartCoroutine(DelayedStart());
        }
        else
        {
            StartStandUpProcess();
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitUntil(() => isInitialized);
        StartStandUpProcess();
    }

    private void StartStandUpProcess()
    {
        // 开始检测输入
        StartCoroutine(WaitForStandUpInput());
    }

    /// <summary>
    /// 检测任何输入
    /// </summary>
    private bool CheckForAnyInput()
    {
        // 检测任何按键按下（包括所有键盘按键和鼠标按键）
        return Input.anyKeyDown;
    }

    /// <summary>
    /// 等待站起输入协程
    /// </summary>
    private IEnumerator WaitForStandUpInput()
    {
        // 等待直到有输入
        yield return new WaitUntil(() => CheckForAnyInput());

        // 开始站起动画
        StartStandUpAnimation();
    }

    /// <summary>
    /// 开始站起动画
    /// </summary>
    private void StartStandUpAnimation()
    {
        if (isStandingUp) return;

        isStandingUp = true;

        // 触发站起动画
        animator.SetTrigger("StandUp");
    }

    /// <summary>
    /// 控制启用/禁用控制器的方法
    /// </summary>
    public void SetControllerEnabled(bool enabled)
    {
        controllerEnabled = enabled;

        // 通过禁用CharacterController来禁用移动
        if (characterController != null)
        {
            characterController.enabled = enabled;
        }
    }

    /// <summary>
    /// 动画事件方法：在站起动画的最后一帧调用
    /// </summary>
    public void OnStandUpAnimationEnd()
    {
        // 启用玩家控制器
        SetControllerEnabled(true);
        isStandingUp = false;

        // 触发完成事件
        OnStandUpComplete?.Invoke();
    }

    /// <summary>
    /// 获取是否正在站起
    /// </summary>
    public bool IsStandingUp()
    {
        return isStandingUp;
    }

    /// <summary>
    /// 获取控制器是否已启用
    /// </summary>
    public bool IsControllerEnabled()
    {
        return controllerEnabled;
    }

    /// <summary>
    /// 强制完成站起（用于跳过动画）
    /// </summary>
    public void ForceStandUp()
    {
        if (!controllerEnabled)
        {
            SetControllerEnabled(true);
            isStandingUp = false;
            OnStandUpComplete?.Invoke();
        }
    }

    /// <summary>
    /// 新增：检查是否已经初始化完成
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
}