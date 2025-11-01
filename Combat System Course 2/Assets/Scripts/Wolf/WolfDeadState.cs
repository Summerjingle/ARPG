using UnityEngine;

public class WolfDeadState : State<WolfController>
{
    private WolfController wolf;
    

    public override void Enter(WolfController owner)
    {
        wolf = owner;
       
        wolf.Animator.SetTrigger("Dead");
        wolf.NavAgent.isStopped = true;
        wolf.NavAgent.enabled = false;
        QuestManager.Instance.OnEnemyKilled("Wolf", wolf.wolfTypeID);
        Debug.Log($" 已通知任务系统: Wolf, {wolf.wolfTypeID}");
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
    

}