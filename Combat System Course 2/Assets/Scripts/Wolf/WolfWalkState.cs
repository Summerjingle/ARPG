using UnityEngine;
using UnityEngine.AI;

public class WolfWalkState : State<WolfController>
{
    private WolfController wolf;
    private Vector3 targetPosition;

    public override void Enter(WolfController owner)
    {
        wolf = owner;

        // 确保设置正确的动画触发器
        wolf.Animator.ResetTrigger("Idle");
        wolf.Animator.ResetTrigger("Run");
        wolf.Animator.SetTrigger("Walk");

        wolf.NavAgent.isStopped = false;
        wolf.NavAgent.speed = 0.9f; // Walk speed

        // 设置行走动画的混合参数
        wolf.Animator.SetFloat("Speed", 0.5f); // 行走速度对应0.5

        // Find random position in patrol radius
        FindNewPatrolPosition();

        // Set random walk time
        wolf.StateTimer = Random.Range(wolf.MinWalkTime, wolf.MaxWalkTime);

        Debug.Log("Wolf entering Walk state");
    }

    public override void Execute()
    {
        if (wolf.Mode == WolfMode.Combat)
        {
            wolf.ChangeState(WolfStates.Run);
            return;
        }

        wolf.StateTimer -= Time.deltaTime;

        // 确保动画状态正确
        if (wolf.NavAgent.velocity.magnitude > 0.1f)
        {
            // 正在移动，确保播放行走动画
            wolf.Animator.SetFloat("Speed", 0.5f);
        }

        // Check if reached destination or time is up
        if (!wolf.NavAgent.pathPending && wolf.NavAgent.remainingDistance <= wolf.NavAgent.stoppingDistance ||wolf.StateTimer <= 0)
        {
            wolf.ChangeState(WolfStates.Idle);
            return;
        }

        // If stuck, find new position
        if (wolf.NavAgent.velocity.magnitude < 0.1f && !wolf.NavAgent.isStopped && wolf.StateTimer > 1f)
        {
            FindNewPatrolPosition();
        }
    }

    public override void Exit()
    {
        // 重置动画参数
        wolf.Animator.SetFloat("Speed", 0f);
        Debug.Log("Wolf exiting Walk state");
    }

    void FindNewPatrolPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wolf.PatrolRadius;
        randomDirection += wolf.SpawnPosition;
        randomDirection.y = wolf.transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wolf.PatrolRadius, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            wolf.NavAgent.SetDestination(targetPosition);
        }
    }
}
