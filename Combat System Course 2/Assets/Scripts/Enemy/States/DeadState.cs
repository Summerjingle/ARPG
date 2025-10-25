using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class DeadState : State<EnemyController>
{
    public override void Enter(EnemyController owner)
    {
        // 确保取消高亮
        if (owner.MeshHighlighter != null)
        {
            owner.MeshHighlighter.HighlightMesh(false);
        }

        // 停止所有可能使用NavAgent的协程
        owner.StopAllCoroutines();

        // 先停止NavAgent，再禁用其他组件
        if (owner.NavAgent != null && owner.NavAgent.isActiveAndEnabled)
        {
            owner.NavAgent.isStopped = true;
            owner.NavAgent.ResetPath(); // 清除路径
        }

        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.i.RemoveEnemyInRange(owner);

        // 最后禁用CharacterController
        if (owner.CharacterController != null)
        {
            owner.CharacterController.enabled = false;
        }

        // 可选：延迟禁用NavAgent，确保完全停止
        if (owner.NavAgent != null)
        {
            owner.StartCoroutine(DisableNavAgentAfterFrame(owner.NavAgent));
        }
    }

    private IEnumerator DisableNavAgentAfterFrame(NavMeshAgent agent)
    {
        yield return new WaitForEndOfFrame();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }
}