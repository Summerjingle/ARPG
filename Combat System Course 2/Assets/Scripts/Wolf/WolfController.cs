using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum WolfStates { Idle, Walk, Run, Attack, Impact, Dead }
public enum WolfMode { Patrol, Combat }

public class WolfController : MonoBehaviour
{
    [Header("受击设置")]
    [SerializeField] private float impactStunTime = 2f; // 眩晕时间
    public float ImpactStunTime => impactStunTime;

    [Header("狼的掉落设置")]
    public LootTable wolfLootTable;

    public string wolfTypeID = "Wolf";

    private bool isStunned = false;
    public bool IsStunned => isStunned;

    [Header("Wolf Settings")]
    [SerializeField] public float attackDistance = 2f;
    [SerializeField] public float chaseDistance = 10f;
    [SerializeField] public float giveUpDistance = 15f;
    [SerializeField] public float attackCooldown = 5f;

    [Header("Patrol Settings")]
    [SerializeField] public float patrolRadius = 10f;
    [SerializeField] public float minIdleTime = 2f;
    [SerializeField] public float maxIdleTime = 5f;
    [SerializeField] public float minWalkTime = 3f;
    [SerializeField] public float maxWalkTime = 8f;

    // Components
    private NavMeshAgent navAgent;
    private Animator animator;
    private BoxCollider attackCollider;

    // State management
    public WolfStates CurrentState { get; private set; }
    public WolfMode CurrentMode { get; private set; }
    private StateMachine<WolfController> stateMachine;
    private Dictionary<WolfStates, State<WolfController>> stateDict;

    // References
    private Transform player;
    private Vector3 spawnPosition;
    private bool isDead = false;

    // Timers
    private float attackTimer = 0f;
    private float stateTimer = 0f;

    // EnemyController 相关
    private EnemyController enemyController;
    private WolfFighter wolfFighter;
    private HealthSystem healthSystem;

    public void SetStunned(bool stunned)
    {
        isStunned = stunned;
    }
    void Awake()
    {
        spawnPosition = transform.position;
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        healthSystem = GetComponent<HealthSystem>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        wolfFighter = GetComponent<WolfFighter>();

    }


    // 初始化战斗组件
    void InitializeCombatComponents()
    {

        if (GetComponentInChildren<WolfWeapon>() == null)
        {
            var wolfWeapon = gameObject.AddComponent<WolfWeapon>();
            Debug.Log("为狼添加了 WolfWeapon 组件");
        }
        else
        {
            Debug.Log("狼的武器已找到");
        }

        // 确保有 EnemyController 组件
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = gameObject.AddComponent<EnemyController>();
            Debug.Log("为狼添加了 EnemyController 组件");
        }

        // 设置 EnemyController 的必要引用（通过反射）
        SetEnemyControllerReferences();

        // 注册死亡事件
        if (healthSystem != null)
        {
            healthSystem.OnDeath += OnWolfDeath;
        }
    }

    // 通过反射设置 EnemyController 的私有字段
    void SetEnemyControllerReferences()
    {
        // 设置 Fighter 引用
        var fighterField = typeof(EnemyController).GetField("Fighter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fighterField != null && wolfFighter != null)
        {
            fighterField.SetValue(enemyController, wolfFighter);
        }

        // 设置 Animator 引用
        var animatorField = typeof(EnemyController).GetField("Animator",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (animatorField != null && animator != null)
        {
            animatorField.SetValue(enemyController, animator);
        }

        // 设置 NavAgent 引用（如果需要）
        var navAgentField = typeof(EnemyController).GetField("NavAgent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (navAgentField != null && navAgent != null)
        {
            navAgentField.SetValue(enemyController, navAgent);
        }
    }

    void Start()
    {
        WolfWeapon wolfWeapon = GetComponentInChildren<WolfWeapon>();

        attackCollider = wolfWeapon.GetComponent<BoxCollider>();

        if (attackCollider == null)
            Debug.LogWarning("Head collider not found! Make sure there's a child object with collider tagged 'HitBox'");
        else
            Debug.Log("AttackCollider已经找到");


        InitializeCombatComponents();
        InitializeStateMachine();
        ChangeState(WolfStates.Idle);
        CurrentMode = WolfMode.Patrol;
        // Ensure head collider is disabled initially
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    void Update()
    {
        if (isDead) return;

        // 如果处于眩晕状态，不执行状态机逻辑
        if (!IsStunned)
        {
            CheckAttackRange();
            UpdateAnimations();
            if (CurrentMode == WolfMode.Combat)
            {
                // 战斗模式下检查是否放弃追逐
                CheckGiveUpCombat();
            }
            stateMachine?.Execute();
        }

        // 攻击冷却计时
        if (AttackTimer > 0)
        {
            AttackTimer -= Time.deltaTime;
        }
    }

    void InitializeStateMachine()
    {
        stateDict = new Dictionary<WolfStates, State<WolfController>>();

        stateDict[WolfStates.Idle] = GetComponent<WolfIdleState>();
        stateDict[WolfStates.Walk] = GetComponent<WolfWalkState>();
        stateDict[WolfStates.Run] = GetComponent<WolfRunState>();
        stateDict[WolfStates.Attack] = GetComponent<WolfAttackState>();
        stateDict[WolfStates.Impact] = GetComponent<WolfImpactState>();
        stateDict[WolfStates.Dead] = GetComponent<WolfDeadState>();

        stateMachine = new StateMachine<WolfController>(this);
    }
    void UpdateAnimations()
    {

        float speed = navAgent.velocity.magnitude / navAgent.speed;
        animator.SetFloat("Speed", speed);
    }

    public void ChangeState(WolfStates newState)
    {
        if (isDead && newState != WolfStates.Dead) return;

        CurrentState = newState;
        stateMachine.ChangeState(stateDict[newState]);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || CurrentState == WolfStates.Impact) return;

        // 直接调用 MeleeFighter 的 TakeDamage，并检查是否已死亡
        if (healthSystem != null && !healthSystem.IsDead)
        {
            healthSystem.TakeDamage(damage, 0); // 狼没有护甲
        }
    }

    public void OnWolfDeath(HealthSystem healthSystem)
    {
        if (!isDead)
        {
            isDead = true;
            ChangeState(WolfStates.Dead);

            QuestManager.Instance.OnEnemyKilled("Wolf", wolfTypeID);
            // 从EnemyManager中移除
            if (enemyController != null)
            {
                EnemyManager.i.RemoveEnemyInRange(enemyController);
            }
        }
    }

    // Called by animation events
    public void EnableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = true;
        }
    }

    public void DisableAttackCollider()
    {
        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }
    }

    // 清理事件
    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= OnWolfDeath;
        }
    }
    public void HandleWolfDeath()
    {
        if (!isDead)
        {
            isDead = true;  // 这里可以设置，因为在同一类中
            ChangeState(WolfStates.Dead);

            // 从EnemyManager中移除
            if (enemyController != null)
            {
                EnemyManager.i.RemoveEnemyInRange(enemyController);
            }
        }
    }
    void CheckAttackRange()
    {
        if (player == null || CurrentState != WolfStates.Run || AttackTimer > 0)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackDistance)
        {
            // 立即停止移动并攻击
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }

            ChangeState(WolfStates.Attack);
        }
    }
    void CheckGiveUpCombat()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > giveUpDistance)
        {
            // 放弃战斗，回到巡逻模式
            CurrentMode = WolfMode.Patrol;
            ChangeState(WolfStates.Idle);

            // 从 EnemyManager 移除
            if (enemyController != null)
            {
                EnemyManager.i.RemoveEnemyInRange(enemyController);
            }
        }
    }

    // Properties
    public NavMeshAgent NavAgent => navAgent;
    public Animator Animator => animator;
    public Transform Player => player;
    public Vector3 SpawnPosition => spawnPosition;
    public bool IsDead => isDead;
    public float StateTimer { get => stateTimer; set => stateTimer = value; }
    public float AttackTimer { get => attackTimer; set => attackTimer = value; }
    public WolfMode Mode { get => CurrentMode; set => CurrentMode = value; }

    // 健康值属性 - 完全使用 MeleeFighter 的健康值
    public float Health => GetComponent<HealthSystem>()?.Health ?? 0f;
    public float MaxHealth => GetComponent<HealthSystem>()?.MaxHealth ?? 0f;
    public bool IsAlive => GetComponent<HealthSystem>()?.IsAlive ?? false;


    // 战斗相关属性
    public EnemyController EnemyController => enemyController;
    public EnemyFighter Fighter => wolfFighter;

    public void DisableWolf()
    {

        Destroy(gameObject);
    }



}