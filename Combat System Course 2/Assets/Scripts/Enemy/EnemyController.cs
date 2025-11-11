using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public enum EnemyStates { Idle, CombatMovement, Attack, RetreatAfterAttack, Dead, GettingHit }
public class EnemyController : MonoBehaviour
{
    [Header("掉落设置")]
    public LootTable lootTable;
    public string enemyTypeID = "Enemy"; // 用于任务系统

    [Header("死亡设置")]
    public float lootSpawnDelay = 1f; // 掉落延迟时间
    public int EXP = 20;
    public EnemyHeathBar healthBar;

    [field: SerializeField] public float Fov { get; private set; } = 180f;
    public List<PlayerFighter> TargetsInRange { get; set; } = new List<PlayerFighter>();
    public ICombatSystem Target { get; set; }
    public float combatMovementTimer { get; set; } = 0F;
    public StateMachine<EnemyController> StateMachine { get; private set; }
    public ICombatSystem CombatSystem { get; private set; }

    Dictionary<EnemyStates, State<EnemyController>> stateDict;

    public NavMeshAgent NavAgent { get; private set; }
    public Animator Animator { get; private set; }
    public ICombatSystem Fighter { get; private set; }
    public VisionSensor VisionSensor { get; set; }

    public SkinnedMashHighlighter MeshHighlighter { get; private set; }

    public CharacterController CharacterController { get; private set; }
    private void Start()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Fighter = GetComponent<ICombatSystem>();
        CharacterController = GetComponent<CharacterController>();
        MeshHighlighter = GetComponent<SkinnedMashHighlighter>();
        healthBar = GetComponentInChildren<EnemyHeathBar>();
        CombatSystem = GetComponent<ICombatSystem>();
        HealthSystem healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("EnemyController 找不到 HealthSystem 组件");
        }

        // 1. 先创建字典
        stateDict = new Dictionary<EnemyStates, State<EnemyController>>();

        // 2. 再添加状态
        stateDict[EnemyStates.Idle] = GetComponent<IdleState>();
        stateDict[EnemyStates.CombatMovement] = GetComponent<CombatMovementState>();
        stateDict[EnemyStates.Attack] = GetComponent<AttackState>();
        stateDict[EnemyStates.RetreatAfterAttack] = GetComponent<RetreatAfterAttackState>();
        stateDict[EnemyStates.Dead] = GetComponent<DeadState>();
        stateDict[EnemyStates.GettingHit] = GetComponent<GettingHitState>();

        // 检查状态组件
        bool allStatesValid = true;
        foreach (var statePair in stateDict)
        {
            if (statePair.Value == null)
            {
                Debug.LogError($"状态 {statePair.Key} 为 NULL!");
                allStatesValid = false;
            }
        }

        if (!allStatesValid)
        {
            Debug.LogError("部分状态组件缺失，无法初始化状态机");
            return;
        }

        StateMachine = new StateMachine<EnemyController>(this);

        if (GetComponent<WolfController>() == null)
        { 
            StateMachine.ChangeState(stateDict[EnemyStates.Idle]); 
        }
            

        Fighter.OnGotHit += (ICombatSystem attacker) =>
        {
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null && healthSystem.Health > 0)
                ChangerState(EnemyStates.GettingHit);
            else
                ChangerState(EnemyStates.Dead);
        };
    }

    private void HandleDeath(HealthSystem healthSystem)
    {
        // 通知 CombatController 这个敌人已死亡
        var playerCombatController = FindObjectOfType<CombatController>();
        if (playerCombatController != null && playerCombatController.TargetEnemy == this)
        {
            playerCombatController.TargetEnemy = null;
            playerCombatController.CombatMode = false;
        }
        // 切换到死亡状态
        ChangerState(EnemyStates.Dead);
    }

    private void OnDestroy()
    {
        HealthSystem healthSystem = GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandleDeath;
        }
    }

    public void ChangerState(EnemyStates enemyStates)
    {
        
        // 防止在死亡状态下切换到其他状态
        if (IsInState(EnemyStates.Dead) && enemyStates != EnemyStates.Dead)
        {
            Debug.LogWarning($"敌人 {gameObject.name} 已死亡，不允许切换到 {enemyStates} 状态");
            return;
        }

        // 防止重复进入相同状态
        if (IsInState(enemyStates))
        {
            return;
        }


        StateMachine.ChangeState(stateDict[enemyStates]);
        Debug.Log($"{gameObject.name} 成功切换到 {enemyStates}");
    }

    public bool IsInState(EnemyStates state)
    {
        // 添加空检查
        if (StateMachine == null || stateDict == null || !stateDict.ContainsKey(state))
        {
            // 如果是狼，返回 false（狼有自己的状态系统）
            if (GetComponent<WolfController>() != null)
                return false;

            Debug.LogWarning($"{gameObject.name} 状态机未初始化，无法检查状态 {state}");
            return false;
        }

        return StateMachine.CurrentState == stateDict[state];
    }
    Vector3 prePos;
    private void Update()
    {
        // 添加空检查
        if (StateMachine == null || StateMachine.CurrentState == null)
            return;

        
        StateMachine.Execute();

        // 只在有相应参数时设置动画参数
        bool hasForwardSpeed = AnimatorHasParameter("forwardSpeed");
        bool hasStrafeSpeed = AnimatorHasParameter("strafeSpeed");

        if (hasForwardSpeed || hasStrafeSpeed)
        {
            var deltaPos = Animator.applyRootMotion ? Vector3.zero : transform.position - prePos;
            var velocity = deltaPos / Time.deltaTime;

            if (hasForwardSpeed)
            {
                float forwardSpeed = Vector3.Dot(velocity, transform.forward);
                Animator.SetFloat("forwardSpeed", forwardSpeed / NavAgent.speed, 0.2f, Time.deltaTime);
            }

            if (hasStrafeSpeed)
            {
                float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
                float stradeSpeed = Mathf.Sin(angle * Mathf.Deg2Rad);
                Animator.SetFloat("strafeSpeed", stradeSpeed, 0.2f, Time.deltaTime);
            }
        }

        prePos = transform.position;
    }

    public ICombatSystem FindTarget()
    {
        foreach (var target in TargetsInRange)
        {
            // 排除死亡目标和无效目标
            if (target == null || target.HealthSystem.IsDead || !target.isActiveAndEnabled)
                continue;

            var vecToTarget = target.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, vecToTarget);

            if (angle <= Fov / 2)
            {
                return target;
            }
        }
        return null;
    }
    private bool AnimatorHasParameter(string paramName)
    {
        if (Animator == null) return false;

        foreach (var param in Animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}