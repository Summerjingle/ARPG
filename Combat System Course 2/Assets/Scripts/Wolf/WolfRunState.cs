using UnityEngine;

public class WolfRunState : State<WolfController>
{
    private WolfController wolf;

    public override void Enter(WolfController owner)
    {
        wolf = owner;

        // 确保设置正确的动画触发器
        wolf.Animator.ResetTrigger("Idle");
        wolf.Animator.ResetTrigger("Walk");
        wolf.Animator.SetTrigger("Run");

        wolf.NavAgent.isStopped = false;
        wolf.NavAgent.speed = 3.5f; // Run speed

        // 设置奔跑动画的混合参数
        wolf.Animator.SetFloat("Speed", 1f); // 奔跑速度对应1.0

        Debug.Log("Wolf entering Run state");
    }

    public override void Execute()
    {
        if (wolf.IsStunned) return;

        if (wolf.Player == null)
        {
            wolf.Mode = WolfMode.Patrol;
            wolf.ChangeState(WolfStates.Idle);
            return;
        }
        if (wolf.Player == null)
        {
            wolf.Mode = WolfMode.Patrol;
            wolf.ChangeState(WolfStates.Idle);
            return;
        }

        float distanceToPlayer = Vector3.Distance(wolf.transform.position, wolf.Player.position);

        // 确保动画状态正确
        if (wolf.NavAgent.velocity.magnitude > 0.1f)
        {
            wolf.Animator.SetFloat("Speed", 1f);
        }

        // Check if player is too far away
        if (distanceToPlayer > wolf.GiveUpDistance)
        {
            wolf.Mode = WolfMode.Patrol;
            wolf.ChangeState(WolfStates.Idle);
            return;
        }

        // Set destination to player
        wolf.NavAgent.SetDestination(wolf.Player.position);

        // Check attack conditions
        if (distanceToPlayer <= wolf.AttackDistance && wolf.AttackTimer <= 0)
        {
            wolf.ChangeState(WolfStates.Attack);
        }
    }

    public override void Exit()
    {
        wolf.NavAgent.ResetPath();
        // 重置动画参数
        wolf.Animator.SetFloat("Speed", 0f);
        Debug.Log("Wolf exiting Run state");
    }
}