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

        
        // ֪ͨ����ϵͳ
        QuestManager.Instance.OnEnemyKilled("Wolf", wolf.wolfTypeID);
        Debug.Log($"��֪ͨ����ϵͳ: Wolf, {wolf.wolfTypeID}");

        // ���ɵ�����Ʒ
        owner.StartCoroutine(SpawnWolfLootWithDelay(wolf));

        // ��EnemyManager���Ƴ�
        if (wolf.EnemyController != null)
        {
            EnemyManager.i.RemoveEnemyInRange(wolf.EnemyController);
        }

        // ȡ������
        // 禁用所有碰撞体 ����������ײ��
        foreach (var collider in wolf.GetComponents<Collider>())
        {
            collider.enabled = false;
        }
        foreach (var collider in wolf.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        wolf.DisableAttackCollider();

        // ����������
        var visionSensor = wolf.GetComponentInChildren<VisionSensor>();
        if (visionSensor != null) visionSensor.enabled = false;

        var damageHandler = wolf.GetComponent<WolfDamageHandler>();
        if (damageHandler != null) damageHandler.enabled = false;

        wolf.Invoke("DisableWolf", 1.1f);
    }

    private IEnumerator SpawnWolfLootWithDelay(WolfController wolfController)
    {
        yield return new WaitForSeconds(1f); // �ǵĵ����ӳ�

        if (wolfController != null && wolfController.wolfLootTable != null)
        {
            GameObject lootContainer = new GameObject($"Loot_Wolf_{wolfController.gameObject.name}");
            lootContainer.transform.position = wolfController.transform.position;

            LootSpawner.SpawnLootItems(wolfController.transform.position, wolfController.wolfLootTable, lootContainer.transform);
        }
    }
}