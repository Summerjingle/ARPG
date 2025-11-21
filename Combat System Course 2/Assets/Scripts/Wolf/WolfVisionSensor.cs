using UnityEngine;

public class WolfVisionSensor : MonoBehaviour
{
    [SerializeField] private WolfController wolfController;

    private void Awake()
    {
        if (wolfController == null)
        {
            wolfController = GetComponentInParent<WolfController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player detected by wolf: {other.name}");
            wolfController.Mode = WolfMode.Combat;
            wolfController.ChangeState(WolfStates.Run);
            // 确保狼被添加到EnemyManager的检测列表中
            if (wolfController.EnemyController != null)
            {
                EnemyManager.i.AddEnemyInRange(wolfController.EnemyController);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 检查距离是否超过放弃距离
            float distanceToPlayer = Vector3.Distance(
                wolfController.transform.position,
                other.transform.position
            );

            if (distanceToPlayer > wolfController.giveUpDistance)
            {
                // 放弃战斗，回到巡逻模式
                wolfController.Mode = WolfMode.Patrol;
                wolfController.ChangeState(WolfStates.Idle);
            }

            // 从 EnemyManager 移除
            if (wolfController.EnemyController != null)
            {
                EnemyManager.i.RemoveEnemyInRange(wolfController.EnemyController);
            }
        }
    }
}