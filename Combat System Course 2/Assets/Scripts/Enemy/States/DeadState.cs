using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DeadState : State<EnemyController>
{
    public override void Enter(EnemyController owner)
    {
        // 1. 取消高亮
        if (owner.MeshHighlighter != null)
        {
            owner.MeshHighlighter.HighlightMesh(false);
        }

        // 2. 停止所有协程
        owner.StopAllCoroutines();

        // 3. 停止NavAgent
        if (owner.NavAgent != null && owner.NavAgent.isActiveAndEnabled)
        {
            owner.NavAgent.isStopped = true;
            owner.NavAgent.ResetPath();
        }

        // 4. 禁用传感器
        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.i.RemoveEnemyInRange(owner);

        // 5. 禁用CharacterController
        if (owner.CharacterController != null)
        {
            owner.CharacterController.enabled = false;
        }

        // 6. 通知任务系统
        if (!string.IsNullOrEmpty(owner.enemyTypeID))
        {
            QuestManager.Instance.OnEnemyKilled(owner.gameObject.name, owner.enemyTypeID);
            Debug.Log($"通知任务系统: {owner.gameObject.name}, {owner.enemyTypeID}");
        }

        // 7. 生成掉落物品
        owner.StartCoroutine(SpawnLootWithDelay(owner));

        // 8. 延迟禁用NavAgent
        if (owner.NavAgent != null)
        {
            owner.StartCoroutine(DisableNavAgentAfterFrame(owner.NavAgent));
        }
    }

    private IEnumerator SpawnLootWithDelay(EnemyController enemy)
    {
        // 等待指定延迟时间
        yield return new WaitForSeconds(enemy.lootSpawnDelay);

        if (enemy != null && enemy.lootTable != null)
        {
            // 创建掉落物容器（可选，用于组织层次结构）
            GameObject lootContainer = new GameObject($"Loot_{enemy.gameObject.name}");
            lootContainer.transform.position = enemy.transform.position;

            LootSpawner.SpawnLootItems(enemy.transform.position, enemy.lootTable, lootContainer.transform);
        }
        else
        {
            Debug.LogWarning($"敌人 {enemy.gameObject.name} 没有设置LootTable");
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