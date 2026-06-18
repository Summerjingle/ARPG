
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public enum EnemyStates { Idle, CombatMovement, Attack, RetreatAfterAttack, Dead, GettingHit }
public class EnemyController : MonoBehaviour
{
    [Header("��������")]
    public LootTable lootTable;
    public string enemyTypeID = "Enemy"; 

    [Header("��������")]
    public float lootSpawnDelay = 1f; 
    public int provideSoulAmount = 20;
    public EnemyHeathBar healthBar;
    public bool IsUncounterable= false;

    [field: SerializeField] public float Fov { get; private set; } = 180f;
    public List<ICombatSystem> TargetsInRange { get; set; } = new List<ICombatSystem>();
    public ICombatSystem Target { get; set; }
    public float combatMovementTimer { get; set; } = 0F;
    public StateMachine<EnemyController> StateMachine { get; private set; }
    public ICombatSystem CombatSystem { get; private set; }

    Dictionary<EnemyStates, State<EnemyController>> stateDict;

    public NavMeshAgent NavAgent { get; private set; }
    public Animator Animator { get; private set; }
    public ICombatSystem Fighter { get; private set; }
    public VisionSensor VisionSensor { get; set; }

    public CharacterController CharacterController { get; private set; }
    private void Start()
    {
        NavAgent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        Fighter = GetComponent<ICombatSystem>();
        CharacterController = GetComponent<CharacterController>();
        healthBar = GetComponentInChildren<EnemyHeathBar>();
        CombatSystem = GetComponent<ICombatSystem>();
        HealthSystem healthSystem = GetComponent<HealthSystem>();

        if (healthSystem != null)
        {
            healthSystem.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("EnemyController �Ҳ��� HealthSystem ���");
        }

        // 1. �ȴ����ֵ�
        stateDict = new Dictionary<EnemyStates, State<EnemyController>>();

        // 2. ������״̬
        stateDict[EnemyStates.Idle] = GetComponent<IdleState>();
        stateDict[EnemyStates.CombatMovement] = GetComponent<CombatMovementState>();
        stateDict[EnemyStates.Attack] = GetComponent<AttackState>();
        stateDict[EnemyStates.RetreatAfterAttack] = GetComponent<RetreatAfterAttackState>();
        stateDict[EnemyStates.Dead] = GetComponent<DeadState>();
        stateDict[EnemyStates.GettingHit] = GetComponent<GettingHitState>();

        // ���״̬���
        bool allStatesValid = true;
        foreach (var statePair in stateDict)
        {
            if (statePair.Value == null)
            {
                Debug.LogError($"״̬ {statePair.Key} Ϊ NULL!");
                allStatesValid = false;
            }
        }

        if (!allStatesValid)
        {
            Debug.LogError("����״̬���ȱʧ���޷���ʼ��״̬��");
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
        
        // ��ֹ������״̬���л�������״̬
        if (IsInState(EnemyStates.Dead) && enemyStates != EnemyStates.Dead)
        {
            Debug.LogWarning($"���� {gameObject.name} ���������������л��� {enemyStates} ״̬");
            return;
        }

        // ��ֹ�ظ�������ͬ״̬
        if (IsInState(enemyStates))
        {
            return;
        }


        StateMachine.ChangeState(stateDict[enemyStates]);
        Debug.Log($"{gameObject.name} �ɹ��л��� {enemyStates}");
    }

    public bool IsInState(EnemyStates state)
    {
        // ���ӿռ��
        if (StateMachine == null || stateDict == null || !stateDict.ContainsKey(state))
        {
            // ������ǣ����� false�������Լ���״̬ϵͳ��
            if (GetComponent<WolfController>() != null)
                return false;

            Debug.LogWarning($"{gameObject.name} ״̬��δ��ʼ�����޷����״̬ {state}");
            return false;
        }

        return StateMachine.CurrentState == stateDict[state];
    }
    Vector3 prePos;
    private void Update()
    {
        // ���ӿռ��
        if (StateMachine == null || StateMachine.CurrentState == null)
            return;

        
        StateMachine.Execute();

        // ֻ������Ӧ����ʱ���ö�������
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
            // �ų�����Ŀ�����ЧĿ��
            if (target == null || target.HealthSystem.IsDead || target.gameObject == null || !target.gameObject.activeInHierarchy)
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