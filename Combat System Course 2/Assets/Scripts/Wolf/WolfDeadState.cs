using System.Collections;
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

        
        // 通知任务系统
        QuestManager.Instance.OnEnemyKilled("Wolf", wolf.wolfTypeID);
        Debug.Log($"已通知任务系统: Wolf, {wolf.wolfTypeID}");

        // 生成掉落物品
        owner.StartCoroutine(SpawnWolfLootWithDelay(wolf));

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

        wolf.Invoke("DisableWolf", 1.1f);
    }

    private IEnumerator SpawnWolfLootWithDelay(WolfController wolfController)
    {
        yield return new WaitForSeconds(1f); // 狼的掉落延迟

        if (wolfController != null && wolfController.wolfLootTable != null)
        {
            GameObject lootContainer = new GameObject($"Loot_Wolf_{wolfController.gameObject.name}");
            lootContainer.transform.position = wolfController.transform.position;

            LootSpawner.SpawnLootItems(wolfController.transform.position, wolfController.wolfLootTable, lootContainer.transform);
        }
    }
}