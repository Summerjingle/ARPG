using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        enemyController.NavAgent.updateRotation = false;
        enemyController.combatMovementTimer = 0;
        enemyController.Animator.SetBool("combatMode", true);
    }
    public override void Execute()
    {
        // ���ȼ���Ƿ�����
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
        //ֻҪ��ҳ���Χ��״̬�ľ����⣬ֱ�ӿ�ʼ׷��
        if (Vector3.Distance(enemyController.Target.transform.position, enemyController.transform.position) > distanceToStand + adjustDistanceThreshold)
        {
            StartChase();
        }

        //״̬�л�
        if (state == AICombatStates.Idle)//1.����״̬
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
        else if (state == AICombatStates.Chase) //2.׷��״̬
        {

            if (Vector3.Distance(enemyController.Target.transform.position, enemyController.transform.position) <= distanceToStand + 0.03f)
            {
                StartIdle();//ֻҪ��ҳ���׷��״̬�ķ�Χ�ڣ�ֱ�ӽ������״̬
            }

            enemyController.NavAgent.SetDestination(enemyController.Target.transform.position);
        }
        else if (state == AICombatStates.Circling)//3.Χ��״̬
        {
            if (timer <= 0)
            {
                StartIdle();
                return;
            }

            var vecToTarget = enemyController.transform.position - enemyController.Target.transform.position;
            var rotatedPos = Quaternion.Euler(0, circlingSpeed * circlingDir * Time.deltaTime, 0) * vecToTarget;

            Vector3 movement = rotatedPos - vecToTarget;
            movement.y = 0f;

            enemyController.NavAgent.Move(movement);
            enemyController.transform.rotation = Quaternion.LookRotation(-rotatedPos);

            float horizontalSpeed = movement.magnitude / Time.deltaTime;
            enemyController.Animator.SetFloat("forwardSpeed", horizontalSpeed / enemyController.NavAgent.speed);
            enemyController.Animator.SetFloat("strafeSpeed", circlingDir);
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
        enemyController.Animator.SetFloat("forwardSpeed", 0f);
        enemyController.Animator.SetFloat("strafeSpeed", 0f);
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
        enemyController.NavAgent.updateRotation = true;
        enemyController.combatMovementTimer = 0f;
        enemyController.Animator.SetFloat("forwardSpeed", 0f);
        enemyController.Animator.SetFloat("strafeSpeed", 0f);
    }
}