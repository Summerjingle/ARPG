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
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float giveUpDistance = 15f;
    [SerializeField] private float attackCooldown = 5f;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float minIdleTime = 2f;
    [SerializeField] private float maxIdleTime = 5f;
    [SerializeField] private float minWalkTime = 3f;
    [SerializeField] private float maxWalkTime = 8f;

    // Components
    private NavMeshAgent navAgent;
    private Animator animator;
    private Collider bodyCollider;
    private Collider headCollider;

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
    private MeleeFighter meleeFighter;
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

        
    }
    

    // 初始化战斗组件
    void InitializeCombatComponents()
    {
        // 确保有 MeleeFighter 组件
        meleeFighter = GetComponent<MeleeFighter>();
        if (meleeFighter == null)
        {
            meleeFighter = gameObject.AddComponent<MeleeFighter>();
            Debug.Log("为狼添加了 MeleeFighter 组件");
        }

        if (GetComponent<WolfWeapon>() == null)
        {
            var wolfWeapon = gameObject.AddComponent<WolfWeapon>();
            Debug.Log("为狼添加了 WolfWeapon 组件");
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
        if (fighterField != null && meleeFighter != null)
        {
            fighterField.SetValue(enemyController, meleeFighter);
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
        bodyCollider = GetComponent<Collider>();
        headCollider = transform.Find("AttackCollider")?.GetComponentInChildren<SphereCollider>();

        if (headCollider == null)
        {
            Debug.LogWarning("Head collider not found! Make sure there's a child object with collider tagged 'HitBox'");
        }
        else
        {
            Debug.Log("AttackCollider已经找到");
        }

        // 初始化 EnemyController 和 MeleeFighter
        InitializeCombatComponents();


        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        InitializeStateMachine();
        ChangeState(WolfStates.Idle);
        CurrentMode = WolfMode.Patrol;
        // Ensure head collider is disabled initially
        if (headCollider != null)
        {
            headCollider.enabled = false;
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
            CheckForPlayer();
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

        // Add state components if they don't exist
        gameObject.AddComponent<WolfIdleState>();
        gameObject.AddComponent<WolfWalkState>();
        gameObject.AddComponent<WolfRunState>();
        gameObject.AddComponent<WolfAttackState>();
        gameObject.AddComponent<WolfImpactState>();
        gameObject.AddComponent<WolfDeadState>();

        // Get states
        stateDict[WolfStates.Idle] = GetComponent<WolfIdleState>();
        stateDict[WolfStates.Walk] = GetComponent<WolfWalkState>();
        stateDict[WolfStates.Run] = GetComponent<WolfRunState>();
        stateDict[WolfStates.Attack] = GetComponent<WolfAttackState>();
        stateDict[WolfStates.Impact] = GetComponent<WolfImpactState>();
        stateDict[WolfStates.Dead] = GetComponent<WolfDeadState>();

        stateMachine = new StateMachine<WolfController>(this);
    }

    void CheckForPlayer()
    {
        if (player == null || CurrentMode == WolfMode.Combat) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= chaseDistance)
        {
            CurrentMode = WolfMode.Combat;
            ChangeState(WolfStates.Run);
        }
    }

    void UpdateAnimations()
    {
        // Update speed parameter based on nav agent velocity
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

    public  void OnWolfDeath(HealthSystem healthSystem)
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

            // 取消高亮
            if (enemyController != null && enemyController.MeshHighlighter != null)
            {
                enemyController.MeshHighlighter.HighlightMesh(false);
            }

            // 通知CombatController清理目标
            var playerCombatController = FindObjectOfType<CombatController>();
            if (playerCombatController != null && playerCombatController.TargetEnemy == enemyController)
            {
                playerCombatController.TargetEnemy = null;
                playerCombatController.CombatMode = false;
            }
        }
    }

    // Called by animation events
    public void EnableAttackCollider()
    {
        if (headCollider != null)
        {
            headCollider.enabled = true;
        }
    }

    public void DisableAttackCollider()
    {
        if (headCollider != null)
        {
            headCollider.enabled = false;
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

            // 取消高亮
            if (enemyController != null && enemyController.MeshHighlighter != null)
            {
                enemyController.MeshHighlighter.HighlightMesh(false);
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

    // Properties
    public NavMeshAgent NavAgent => navAgent;
    public Animator Animator => animator;
    public Transform Player => player;
    public Vector3 SpawnPosition => spawnPosition;
    public bool IsDead => isDead;
    public float AttackDistance => attackDistance;
    public float ChaseDistance => chaseDistance;
    public float GiveUpDistance => giveUpDistance;
    public float AttackCooldown => attackCooldown;
    public float PatrolRadius => patrolRadius;
    public float MinIdleTime => minIdleTime;
    public float MaxIdleTime => maxIdleTime;
    public float MinWalkTime => minWalkTime;
    public float MaxWalkTime => maxWalkTime;
    public float StateTimer { get => stateTimer; set => stateTimer = value; }
    public float AttackTimer { get => attackTimer; set => attackTimer = value; }
    public WolfMode Mode { get => CurrentMode; set => CurrentMode = value; }

    // 健康值属性 - 完全使用 MeleeFighter 的健康值
    public float Health => GetComponent<HealthSystem>()?.Health ?? 0f;
    public float MaxHealth => GetComponent<HealthSystem>()?.MaxHealth ?? 0f;
    public bool IsAlive => GetComponent<HealthSystem>()?.IsAlive ?? false;


    // 战斗相关属性
    public EnemyController EnemyController => enemyController;
    public MeleeFighter Fighter => meleeFighter;

    public void DisableWolf()
    {
        
        Destroy(gameObject);
    }

    
   
}