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
    public List<MeleeFighter> TargetsInRange { get; set; } = new List<MeleeFighter>();
    public MeleeFighter Target { get; set; }
    public float combatMovementTimer { get; set; } = 0F;
    public StateMachine<EnemyController> StateMachine { get; private set; }

    Dictionary<EnemyStates, State<EnemyController>> stateDict;

    public NavMeshAgent NavAgent { get; private set; }
    public Animator Animator { get; private set; }
    public MeleeFighter Fighter { get; private set; }
    public VisionSensor VisionSensor { get; set; }

    public SkinnedMashHighlighter MeshHighlighter { get; private set; }

    public CharacterController CharacterController { get; private set; }
    private void Start()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Fighter = GetComponent<MeleeFighter>();
        CharacterController = GetComponent<CharacterController>();
        MeshHighlighter = GetComponent<SkinnedMashHighlighter>();
        healthBar = GetComponentInChildren<EnemyHeathBar>();
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



        StateMachine = new StateMachine<EnemyController>(this);
        StateMachine.ChangeState(stateDict[EnemyStates.Idle]);

        Fighter.OnGotHit += (MeleeFighter attacker) =>
        {
            HealthSystem healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null && healthSystem.Health > 0)
                ChangerState(EnemyStates.GettingHit);
            else
                ChangerState(EnemyStates.Dead);
        };

        MeleeFighter fighter = GetComponent<MeleeFighter>();
        if (fighter != null)
        {
            fighter.HealthSystem.OnDeath += HandleDeath;
        }
    }
    private void HandleDeath(HealthSystem healthSystem)
    {
        var fighter = healthSystem.GetComponent<MeleeFighter>();
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
        return StateMachine.CurrentState == stateDict[state];
    }
    Vector3 prePos;
    private void Update()
    {
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

    public MeleeFighter FindTarget()
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