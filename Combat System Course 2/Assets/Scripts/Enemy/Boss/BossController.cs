using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum BossPositionZone { Front, Back, Left, Right }

public class BossController : MonoBehaviour
{
    public string BossName="请输入Boss的名字";
    private State<BossController> currentState;

    [Header("State References")]
    public BossIdleState idleState;
    public BossChaseState chaseState;
    public BossAttackState attackState;
    public BossRangedAttackState rangedAttackState;
    public BossStunnedState stunnedState;
    public BossDieState dieState;

    [Header("Targeting")]
    public Transform playerTarget;
    public Transform lockOnPoint;
    public float lockOnMinFreezeDistance = 1.5f;
    public float detectRange = 15f;
    public float attackRange = 3f;

    [Header("Movement")]
    [Range(1f, 10f)] public float walkSpeed = 2f;
    [Range(1f, 20f)] public float runSpeed = 5f;
    [Range(90f, 720f)] public float rotationSpeed = 360f;

    [Header("Attack")]
    public float attackCD = 2f;
    [HideInInspector] public float lastAttackTime = -999f;

    [Header("Ranged Attack")]
    public float rangedAttackCD = 4f;
    [HideInInspector] public float lastRangedAttackTime = -999f;
    public float rangedChaseTimeThreshold = 6f;
    public GameObject boulderPrefab;
    public Transform boulderSpawnPoint;
    public float boulderFlySpeed = 15f;

    [Header("Stun")]
    [Range(0f, 1f)] public float stunHealthThreshold = 0.5f;
    [HideInInspector] public bool hasTriggeredStun = false;

    [Header("Attack Groups (按方位)")]
    public List<AttackData> frontAttacks;
    public List<AttackData> backAttacks;
    public List<AttackData> leftAttacks;
    public List<AttackData> rightAttacks;

    [Header("Components")]
    public Animator anim;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyFighter fighter;
    private HealthSystem healthSystem;

    public event System.Action OnBossFightEnter;

    public event System.Action OnBossFightExit;
    private bool hasStarted = false;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        fighter = GetComponent<EnemyFighter>();
        if (anim == null) anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.updateRotation = false; // 我们自己控制旋转，不让 NavAgent 跟 root motion 打架
        }
        if (anim != null)
            anim.applyRootMotion = false; // 非攻击期间关闭 root motion

        if (fighter != null)
        {
            fighter.OnGotHit += OnGotHit;
            healthSystem = GetComponent<HealthSystem>();
            if (healthSystem != null) healthSystem.OnDeath += OnDeath;
        }

        ChangeState(idleState);
    }

    private void OnGotHit(ICombatSystem attacker)
    {
        if (hasTriggeredStun) return;
        if (healthSystem == null) return;

        float hpPercent = healthSystem.HealthPercent;
        if (hpPercent <= stunHealthThreshold && hpPercent > 0f)
        {
            hasTriggeredStun = true;
            ChangeState(stunnedState);
        }
    }

    private void OnDeath(HealthSystem hs)
    {
        ChangeState(dieState);
    }

    private void OnDestroy()
    {
        if (fighter != null) fighter.OnGotHit -= OnGotHit;
        if (healthSystem != null) healthSystem.OnDeath -= OnDeath;
    }

    void Update()
    {
        currentState?.Execute();
    }

    public void ChangeState(State<BossController> newState)
    {
        if (newState == null || newState == currentState) return;
        currentState?.Exit();
        if(currentState is BossIdleState && !hasStarted)
            OnBossFightEnter?.Invoke();
        currentState = newState;
        currentState.Enter(this);
        if(currentState is BossDieState)
            OnBossFightExit?.Invoke();
    }

    /// <summary>每帧调用，平滑面向玩家</summary>
    public void FacePlayer()
    {
        if (playerTarget == null) return;
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public BossPositionZone DetectPlayerZone()
    {
        if (playerTarget == null) return BossPositionZone.Front;

        Vector3 toPlayer = playerTarget.position - transform.position;
        toPlayer.y = 0;

        if (toPlayer.magnitude < 0.5f) return BossPositionZone.Front;

        Vector3 forward = transform.forward;
        float dot = Vector3.Dot(forward, toPlayer.normalized);
        float crossY = Vector3.Cross(forward, toPlayer.normalized).y;

        if (dot < -0.3f) return BossPositionZone.Back;
        if (crossY > 0.3f) return BossPositionZone.Right;
        if (crossY < -0.3f) return BossPositionZone.Left;
        return BossPositionZone.Front;
    }

    public List<AttackData> GetAttackGroup(BossPositionZone zone)
    {
        switch (zone)
        {
            case BossPositionZone.Back:  return backAttacks;
            case BossPositionZone.Left:  return leftAttacks;
            case BossPositionZone.Right: return rightAttacks;
            default:                     return frontAttacks;
        }
    }

    public float GetFlatDistanceToPlayer()
    {
        if (playerTarget == null) return float.MaxValue;
        Vector3 a = transform.position, b = playerTarget.position;
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b);
    }
    public void OnStunnedEnd()  //给Stun_end动画末帧调用
    {
        if (currentState is BossStunnedState)
        {
            ChangeState(idleState);
        }
    }

    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCD;
    }

    public bool CanRangedAttack()
    {
        return Time.time - lastRangedAttackTime >= rangedAttackCD;
    }
}
