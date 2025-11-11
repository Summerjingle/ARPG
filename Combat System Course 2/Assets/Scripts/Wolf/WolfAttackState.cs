using UnityEngine;
using System.Collections;

public class WolfAttackState : State<WolfController>
{
    private WolfController wolf;
    private bool isAttacking = false;

    public override void Enter(WolfController owner)
    {
        wolf = owner;
        isAttacking = true;
        wolf.Animator.SetTrigger("Attack");
        wolf.NavAgent.isStopped = true;

        // 面向玩家
        if (wolf.Player != null)
        {
            Vector3 direction = (wolf.Player.position - wolf.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                wolf.transform.rotation = Quaternion.LookRotation(direction);
            }

            // 关键：设置MeleeFighter的攻击目标
            var playerFighter = wolf.Player.GetComponent<PlayerFighter>();
            if (playerFighter != null)
            {
                wolf.Fighter.currTarget = playerFighter;
            }
        }

        // 设置攻击冷却
        wolf.AttackTimer = wolf.AttackCooldown;

        // 启动协程等待攻击完成
        wolf.StartCoroutine(WaitForAttackCompletion());
    }

    IEnumerator WaitForAttackCompletion()
    {
        // 等待攻击动画完成（这里需要根据你的实际动画长度调整）
        yield return new WaitForSeconds(1f); // 假设攻击动画大约1.5秒

        isAttacking = false;

        // 攻击完成后检查是否应该继续追击
        if (wolf.Player != null && !wolf.IsDead)
        {
            float distanceToPlayer = Vector3.Distance(wolf.transform.position, wolf.Player.position);

            if (distanceToPlayer > wolf.AttackDistance && distanceToPlayer <= wolf.ChaseDistance)
            {
                wolf.ChangeState(WolfStates.Run);
            }
            else if (distanceToPlayer > wolf.ChaseDistance)
            {
                wolf.Mode = WolfMode.Patrol;
                wolf.ChangeState(WolfStates.Idle);
            }
            else
            {
                // 玩家仍在攻击范围内，可以再次攻击或保持警戒
                wolf.ChangeState(WolfStates.Idle);
            }
        }
        else
        {
            wolf.ChangeState(WolfStates.Idle);
        }
    }

    public override void Execute()
    {
        // 攻击过程中不执行其他逻辑，等待协程完成
        if (!isAttacking && !wolf.IsDead)
        {
            // 如果攻击意外结束，确保状态正确
            wolf.ChangeState(WolfStates.Idle);
        }
    }

    public override void Exit()
    {
        // 确保攻击碰撞体被禁用
        wolf.DisableAttackCollider();

        // 停止所有协程
        wolf.StopAllCoroutines();
    }
}