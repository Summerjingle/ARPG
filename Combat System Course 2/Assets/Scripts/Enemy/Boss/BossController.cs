using UnityEngine;
using UnityEngine.AI;

public class BossController : MonoBehaviour
{
    private State<BossController> currentState;

    [Header("State References")]
    public BossIdleState idleState;
    public BossChaseState chaseState;
    public BossStunnedState stunnedState;
    public BossMeleeState meleeState;
    public BossRangeState rangeState;
    public BossDieState dieState;
    public BossTurnToTargetState turnToTargetState; 

    [Header("Targeting")]
    public Transform playerTarget; 
    public float detectRange = 10f;  
    public float attackRange = 2f;   

    [Header("Movement Stats")]
    [Range(1f, 10f)] public float walkSpeed = 2.0f; 
    [Range(1f, 15f)] public float runSpeed = 5.0f;  
    
    [HideInInspector] public float lastAttackTime; 
    public float attackCD = 0.5f; 

    [Header("视觉表现")]
    public Animator anim; 
    [HideInInspector] public NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.stoppingDistance = 0f; 
        }

        if (idleState != null) ChangeState(idleState);
    }

    void Update()
    {
        if (currentState != null) currentState.Execute();
        
        // Debug Keys
        if (Input.GetKeyDown(KeyCode.Alpha0)) ChangeState(idleState);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeState(chaseState);
        if (Input.GetKeyDown(KeyCode.K)) BreakPoise();
    }

    public void ChangeState(State<BossController> newState)
    {
        if (newState == null || newState == currentState) return;
        if (currentState != null) currentState.Exit();
        currentState = newState;
        currentState.Enter(this);
        Debug.Log($"<color=cyan>[FSM]</color> 状态切换至: <color=yellow>{newState.GetType().Name}</color>");
    }

    public void BreakPoise()
    {
        if (currentState == dieState) return;
        ChangeState(stunnedState);
    }

    public float GetFlatDistanceToPlayer()
    {
        if (playerTarget == null) return float.MaxValue;
        Vector3 playerPos = playerTarget.position;
        Vector3 bossPos = transform.position;
        playerPos.y = 0; bossPos.y = 0;
        return Vector3.Distance(playerPos, bossPos);
    }

    public void SetAnimFloat(string paramName, float value) { if (anim != null) anim.SetFloat(paramName, value); }
    public void PlayAnim(string stateName, float fade = 0.1f, int layer = 0) { if (anim != null) anim.CrossFade(stateName, fade, layer); }
}