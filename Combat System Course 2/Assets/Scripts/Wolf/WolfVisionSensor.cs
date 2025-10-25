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
            // 玩家离开视野时从EnemyManager移除
            if (wolfController.EnemyController != null)
            {
                EnemyManager.i.RemoveEnemyInRange(wolfController.EnemyController);
            }
        }
    }
}