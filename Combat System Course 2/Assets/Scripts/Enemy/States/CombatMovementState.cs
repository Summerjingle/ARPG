using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public enum AICombatStates { Idle, Chase, Circling }
public class CombatMovementState : State<EnemyController>
{
    [SerializeField] private float distanceToStand = 3f;
    [SerializeField] private float adjustDistanceThreshold = 1f;
    [SerializeField] private float circlingSpeed = 20f;
    [SerializeField] private Vector2 idleTimeRange = new Vector2(2, 5);
    [SerializeField] private Vector2 circlingTimeRange = new Vector2(3, 6);

    private AICombatStates state;
    private EnemyController enemyController;

    private float timer = 0f;
    private int circlingDir = -1;
    public override void Enter(EnemyController owner)
    {
        enemyController = owner;
        enemyController.NavAgent.stoppingDistance = distanceToStand;
        enemyController.combatMovementTimer = 0;
        enemyController.Animator.SetBool("combatMode", true);
    }
    public override void Execute()
    {
        // Ê×ÏÈ¼ì²éÊÇ·ñËÀÍö
        if (enemyController.Fighter.HealthSystem.IsDead)
        {
            enemyController.ChangerState(EnemyStates.Dead);
            return;
        }
        if (enemyController.IsInState(EnemyStates.Dead) ||
         !enemyController.NavAgent.enabled ||
         !enemyController.NavAgent.isActiveAndEnabled)
        {
            return;
        }
        if (enemyController.Target == null)
        {
            enemyController.Target = enemyController.FindTarget();
            if (enemyController.Target == null)
            {
                enemyController.ChangerState(EnemyStates.Idle);
                return;
            }
        }
        //Ö»ÒªÍæ¼Ò³¬³öÎ§À§×´Ì¬µÄ¾àÀëÍâ£¬Ö±½Ó¿ªÊ¼×·Öð
        if (Vector3.Distance(enemyController.Target.transform.position, enemyController.transform.position) > distanceToStand + adjustDistanceThreshold)
        {
            StartChase();
        }

        //×´Ì¬ÇÐ»»
        if (state == AICombatStates.Idle)//1.´ý»ú×´Ì¬
        {
            if (timer <= 0)
            {
                if (Random.Range(0, 2) == 0)
                {
                    StartIdle();
                }
                else
                {
                    StartCircling();
                }
            }
        }
        else if (state == AICombatStates.Chase) //2.×·Öð×´Ì¬
        {

            if (Vector3.Distance(enemyController.Target.transform.position, enemyController.transform.position) <= distanceToStand + 0.03f)
            {
                StartIdle();//Ö»ÒªÍæ¼Ò³¬³ö×·Öð×´Ì¬µÄ·¶Î§ÄÚ£¬Ö±½Ó½øÈë´ý»ú×´Ì¬
            }

            enemyController.NavAgent.SetDestination(enemyController.Target.transform.position);
        }
        else if (state == AICombatStates.Circling)//3.Î§À§×´Ì¬
        {
            if (timer <= 0)
            {
                StartIdle();
                return;
            }

            var vecToTarget = enemyController.transform.position - enemyController.Target.transform.position;
            var rotatedPos = Quaternion.Euler(0, circlingSpeed * circlingDir * Time.deltaTime, 0) * vecToTarget;

            enemyController.NavAgent.Move(rotatedPos - vecToTarget);
            enemyController.transform.rotation = Quaternion.LookRotation(-rotatedPos);
        }

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        enemyController.combatMovementTimer += Time.deltaTime;

    }


    private void StartChase()
    {
        state = AICombatStates.Chase;

    }
    private void StartIdle()
    {
        state = AICombatStates.Idle;
        timer = Random.Range(idleTimeRange.x, idleTimeRange.y);



    }

    private void StartCircling()
    {
        state = AICombatStates.Circling;

        enemyController.NavAgent.ResetPath();
        timer = Random.Range(circlingTimeRange.x, circlingTimeRange.y);
        circlingDir = Random.Range(0, 2) == 0 ? 1 : -1;

    }

    public override void Exit()
    {
        enemyController.combatMovementTimer = 0f;
    }
}