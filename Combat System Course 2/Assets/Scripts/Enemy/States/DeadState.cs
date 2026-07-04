using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DeadState : State<EnemyController>
{
    public override void Enter(EnemyController owner)
    {
        // 1. 停止所有协程
        owner.StopAllCoroutines();
        if (owner.Fighter is MonoBehaviour fighterMb)
            fighterMb.StopAllCoroutines();  // 停止 EnemyFighter 上的 ExecuteEnemyAttack 协程

        // 如果敌人在攻击 Impact 阶段死亡，StopAllCoroutines 会提前杀掉 ExecuteEnemyAttack，
        // 导致 DisableEnemyHitboxes() 来不及执行，武器碰撞器残留在尸体上。这里手动补关。
        (owner.Fighter as EnemyFighter)?.DisableHitboxes();

        // 3. 停止NavMeshAgent导航
        if (owner.NavAgent != null && owner.NavAgent.isActiveAndEnabled)
        {
            owner.NavAgent.isStopped = true;
            owner.NavAgent.ResetPath();
        }

        // 4. 禁用视觉传感器，并从敌人管理器中移除
        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.i.RemoveEnemyInRange(owner);

        // 5. 禁用角色控制器
        if (owner.capsuleCollider != null)
        {
            owner.capsuleCollider.enabled = false;
        }

        // 6. 通知任务系统：敌人被击杀
        if (!string.IsNullOrEmpty(owner.enemyTypeID))
        {
            QuestManager.Instance.OnEnemyKilled(owner.gameObject.name, owner.enemyTypeID);
            Debug.Log($"通知任务系统: {owner.gameObject.name}, {owner.enemyTypeID}");
        }

        // 7. 生成战利品（延迟生成）
        owner.StartCoroutine(SpawnLootWithDelay(owner));

        
        // 8. 延迟一帧后禁用NavAgent（确保当前帧的所有导航操作完成）
        if (owner.NavAgent != null)
        {
            owner.StartCoroutine(DisableNavAgentAfterFrame(owner.NavAgent));
        }

        // 9. 关闭MapEntity（禁用小地图图标）
        MapEntity mapEntity = owner.GetComponent<MapEntity>();
        if (mapEntity != null)
            mapEntity.enabled = false;
    }

    /// <summary>
    /// 延迟生成战利品的协程
    /// </summary>
    private IEnumerator SpawnLootWithDelay(EnemyController enemy)
    {
        // 等待指定延迟时间
        yield return new WaitForSeconds(enemy.lootSpawnDelay);

        if (enemy != null && enemy.lootTable != null)
        {
            // 创建战利品容器对象，用于组织掉落物的层级结构
            GameObject lootContainer = new GameObject($"Loot_{enemy.gameObject.name}");
            lootContainer.transform.position = enemy.transform.position;

            LootSpawner.SpawnLootItems(enemy.transform.position, enemy.lootTable, lootContainer.transform);
        }
        else
        {
            Debug.LogWarning($"敌人 {enemy.gameObject.name} 没有配置LootTable");
        }
    }

    /// <summary>
    /// 等待当前帧结束后禁用NavMeshAgent
    /// </summary>
    private IEnumerator DisableNavAgentAfterFrame(NavMeshAgent agent)
    {
        yield return new WaitForEndOfFrame();
        if (agent != null)
        {
            agent.enabled = false;
        }
    }
}