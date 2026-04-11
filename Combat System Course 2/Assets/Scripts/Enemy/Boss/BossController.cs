using UnityEngine;

public class BossController : MonoBehaviour
{
    // 当前正在运行的状态
    private State<BossController> currentState;

    // 为了方便在 Inspector 里配置和引用，我们将所有状态实例序列化
    [Header("State References")]
    public BossIdleState idleState;
    public BossChaseState chaseState;
    public BossStunnedState stunnedState;
    public BossMeleeState meleeState;
    public BossRangeState rangeState;
    public BossDieState dieState;
    public BossTurnToTargetState turnToTargetState; // 新增转身状态
    public Transform playerTarget; // 记得在 Inspector 里把玩家拖进来，或者在 Start 里用 GameObject.Find 找到
    public float detectRange = 10f;  // 发现玩家的距离
    public float attackRange = 2f;   // 停止追击开始攻击的距离
    [Range(1f, 10f)] public float walkSpeed = 2.0f; // 走路速度
    [Range(1f, 15f)] public float runSpeed = 5.0f;  // 跑步速度

    [Header("视觉表现")]
    public Animator anim; // 拖入 Boss 的 Animator 组件
    [HideInInspector] 
    public UnityEngine.AI.NavMeshAgent agent;

    void Awake()
    {
        // 获取挂在自己身上的导航组件
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
    void Start()
    {
        detectRange=10;
        // 初始状态进入 Idle
        if (idleState != null)
        {
            ChangeState(idleState);
        }
        else
        {
            Debug.LogError("未在 BossController 中分配 Idle 状态！");
        }
        
        if (agent != null)
        {
        agent.speed = runSpeed; // 默认追击用跑步速度
        }
    }

    void Update()
    {
        // 每一帧执行当前状态的逻辑
        if (currentState != null)
        {
            currentState.Execute();
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) ChangeState(idleState);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeState(chaseState);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeState(turnToTargetState);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeState(stunnedState);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeState(dieState);
        if (Input.GetKeyDown(KeyCode.K))
        {
            BreakPoise();
        }
    
    }

    public void ChangeState(State<BossController> newState)
    {
        if (newState == null || newState == currentState) return;

        // 1. 退出旧状态
        if (currentState != null)
        {
            currentState.Exit();
        }

        // 2. 切换状态
        currentState = newState;

        // 3. 进入新状态
        currentState.Enter(this);
        
        Debug.Log($"<color=cyan>[FSM]</color> 状态切换至: <color=yellow>{newState.GetType().Name}</color>");
    }
    // 这个方法可以挂在你的受伤脚本里，或者被玩家的武器碰撞器调用
    public void BreakPoise()
    {
        // 如果 Boss 已经死了，就不管了
        if (currentState == dieState) return;

        // 无论当前 Boss 是在追击、发呆还是在挥大剑，强制打断！
        ChangeState(stunnedState);
    }
    public void SetAnimFloat(string paramName, float value)
    {
        if (anim != null) anim.SetFloat(paramName, value);
    }

    public void SetAnimBool(string paramName, bool value)
    {
        if (anim != null) anim.SetBool(paramName, value);
    }
    public void PlayAnim(string stateName, float fade = 0.1f, int layer = 0)
{
    if (anim != null) anim.CrossFade(stateName, fade, layer);
}
    
}