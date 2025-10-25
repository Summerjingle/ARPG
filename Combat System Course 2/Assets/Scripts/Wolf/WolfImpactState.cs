using System.Collections;
using UnityEngine;

public class WolfImpactState : State<WolfController>
{
    private WolfController wolf;
    private Coroutine returnCoroutine;
    private bool isInImpact = false;

    public override void Enter(WolfController owner)
    {
        wolf = owner;

        if (wolf.IsDead) return;
        if (isInImpact) return;

        isInImpact = true;

        // 使用公共方法设置眩晕状态
        wolf.SetStunned(true);

        wolf.Animator.SetTrigger("Impact");
        wolf.NavAgent.isStopped = true;

        if (returnCoroutine != null)
        {
            wolf.StopCoroutine(returnCoroutine);
        }

        returnCoroutine = wolf.StartCoroutine(WaitForStunCompletion());
    }

    IEnumerator WaitForStunCompletion()
    {
        yield return new WaitForSeconds(wolf.ImpactStunTime);

        isInImpact = false;
        wolf.SetStunned(false); // 清除眩晕状态

        if (!wolf.IsDead)
        {
            if (wolf.Mode == WolfMode.Combat)
            {
                wolf.ChangeState(WolfStates.Run);
            }
            else
            {
                wolf.ChangeState(WolfStates.Idle);
            }
        }
    }

    public override void Exit()
    {
        isInImpact = false;
        wolf.SetStunned(false); // 确保退出时清除状态

        if (returnCoroutine != null)
        {
            wolf.StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
}