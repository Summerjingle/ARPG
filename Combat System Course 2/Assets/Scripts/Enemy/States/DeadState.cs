using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DeadState : State<EnemyController>
{
    public override void Enter(EnemyController owner)
    {
        if (owner.GetComponent<WolfController>() != null)
        {
            Debug.Log($"�� {owner.gameObject.name} ������ͨ������������");
            return;
        }
        // 1. ȡ������
        if (owner.MeshHighlighter != null)
        {
            owner.MeshHighlighter.HighlightMesh(false);
        }

        // 2. ֹͣ����Э��
        owner.StopAllCoroutines();
        if (owner.Fighter is MonoBehaviour fighterMb)
            fighterMb.StopAllCoroutines();  // EnemyFighter �ϵ� ExecuteEnemyAttack Э��

        // 3. ֹͣNavAgent
        if (owner.NavAgent != null && owner.NavAgent.isActiveAndEnabled)
        {
            owner.NavAgent.isStopped = true;
            owner.NavAgent.ResetPath();
        }

        // 4. ���ô�����
        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.i.RemoveEnemyInRange(owner);

        // 5. ����CharacterController
        if (owner.CharacterController != null)
        {
            owner.CharacterController.enabled = false;
        }

        // 6. ֪ͨ����ϵͳ
        if (!string.IsNullOrEmpty(owner.enemyTypeID))
        {
            QuestManager.Instance.OnEnemyKilled(owner.gameObject.name, owner.enemyTypeID);
            Debug.Log($"֪ͨ����ϵͳ: {owner.gameObject.name}, {owner.enemyTypeID}");
        }

        // 7. ���ɵ�����Ʒ
        owner.StartCoroutine(SpawnLootWithDelay(owner));

        // 8. �ӳٽ���NavAgent
        if (owner.NavAgent != null)
        {
            owner.StartCoroutine(DisableNavAgentAfterFrame(owner.NavAgent));
        }

        // 9. �ر�MapEntity���������ͼͼ��
        MapEntity mapEntity = owner.GetComponent<MapEntity>();
        if (mapEntity != null)
            mapEntity.enabled = false;
    }

    private IEnumerator SpawnLootWithDelay(EnemyController enemy)
    {
        // �ȴ�ָ���ӳ�ʱ��
        yield return new WaitForSeconds(enemy.lootSpawnDelay);

        if (enemy != null && enemy.lootTable != null)
        {
            // ������������������ѡ��������֯��νṹ��
            GameObject lootContainer = new GameObject($"Loot_{enemy.gameObject.name}");
            lootContainer.transform.position = enemy.transform.position;

            LootSpawner.SpawnLootItems(enemy.transform.position, enemy.lootTable, lootContainer.transform);
        }
        else
        {
            Debug.LogWarning($"���� {enemy.gameObject.name} û������LootTable");
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