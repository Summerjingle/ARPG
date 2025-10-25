using UnityEngine;

public class WolfDeadState : State<WolfController>
{
    private WolfController wolf;
    

    public override void Enter(WolfController owner)
    {
        wolf = owner;
        if (wolf.WolfPointer != null)//隐藏标记狼的mark
        {
            wolf.WolfPointer.SetActive(false);
        }
        CheckAndUpdateWolfKillQuest();//检查是否接取了杀狼任务？完成：不处理
        wolf.Animator.SetTrigger("Dead");
        wolf.NavAgent.isStopped = true;
        wolf.NavAgent.enabled = false;

        // 从EnemyManager中移除
        if (wolf.EnemyController != null)
        {
            EnemyManager.i.RemoveEnemyInRange(wolf.EnemyController);
        }

        // 取消高亮
        if (wolf.EnemyController?.MeshHighlighter != null)
        {
            wolf.EnemyController.MeshHighlighter.HighlightMesh(false);
        }

        // 禁用所有碰撞器
        foreach (var collider in wolf.GetComponents<Collider>())
        {
            collider.enabled = false;
        }
        foreach (var collider in wolf.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        wolf.DisableAttackCollider();

        // 禁用相关组件
        var visionSensor = wolf.GetComponentInChildren<VisionSensor>();
        if (visionSensor != null) visionSensor.enabled = false;

        var damageHandler = wolf.GetComponent<WolfDamageHandler>();
        if (damageHandler != null) damageHandler.enabled = false;

        wolf.Invoke("DisableWolf", 5f);

    }
    private void CheckAndUpdateWolfKillQuest()
    {
        if (GameManager.Instance == null || wolf.relatedQuest == null) return;

        QuestState state = GameManager.Instance.GetQuestState(wolf.relatedQuest);
        if (state == QuestState.InProgress)
        {
            GameManager.Instance.SetQuestState(wolf.relatedQuest, QuestState.CanComplete);
            Debug.Log($"杀狼任务 {wolf.relatedQuest.questName} 已更新为可完成状态");

            if (QuestPanelController.Instance != null)
            {
                QuestPanelController.Instance.UpdateAllPanels();
            }
        }
    }

}