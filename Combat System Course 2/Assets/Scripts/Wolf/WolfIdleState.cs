using UnityEngine;

public class WolfIdleState : State<WolfController>
{
    private WolfController wolf;

    public override void Enter(WolfController owner)
    {
        wolf = owner;


        // 确保设置正确的动画触发器
        wolf.Animator.ResetTrigger("Walk");
        wolf.Animator.ResetTrigger("Run");
        wolf.Animator.ResetTrigger("Attack");
        wolf.Animator.SetTrigger("Idle");

        wolf.NavAgent.isStopped = true;

        // 设置空闲动画的混合参数
        wolf.Animator.SetFloat("Speed", 0f);

        // Set random idle time
        wolf.StateTimer = Random.Range(wolf.minIdleTime, wolf.maxIdleTime);

       
    }

    public override void Execute()
    {
        if (wolf.IsStunned) return;

        if (wolf.Mode == WolfMode.Combat)
        {
            HandleCombatMode();
            return;
        }
        if (wolf.Mode == WolfMode.Combat)
        {
            HandleCombatMode();
            return;
        }

        // Patrol mode - count down idle timer
        wolf.StateTimer -= Time.deltaTime;

        if (wolf.StateTimer <= 0)
        {
            // Switch to walking after idle
            wolf.ChangeState(WolfStates.Walk);
        }
    }

    public override void Exit()
    {
        
    }

    void HandleCombatMode()
    {
        if (wolf.Player == null)
        {
            wolf.Mode = WolfMode.Patrol;
            return;
        }

        float distanceToPlayer = Vector3.Distance(wolf.transform.position, wolf.Player.position);

        // Check if player is too far away
        if (distanceToPlayer > wolf.giveUpDistance)
        {
            wolf.Mode = WolfMode.Patrol;
            return;
        }

        // Check if player is in attack range
        if (distanceToPlayer <= wolf.attackDistance && wolf.AttackTimer <= 0)
        {
            wolf.ChangeState(WolfStates.Attack);
        }
        else if (distanceToPlayer > wolf.attackDistance)
        {
            // Move towards player
            wolf.ChangeState(WolfStates.Run);
        }
    }
}