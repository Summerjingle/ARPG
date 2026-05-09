using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [SerializeField] EnemyController enemyController;

    private void Awake()
    {
        enemyController.VisionSensor = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        var fighter = other.GetComponent<PlayerFighter>() ?? (ICombatSystem)other.GetComponent<PlayerFighterNew>();
        if (fighter != null)
        {
            enemyController.TargetsInRange.Add(fighter);
            EnemyManager.i.AddEnemyInRange(enemyController);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var fighter = other.GetComponent<PlayerFighter>() ?? (ICombatSystem)other.GetComponent<PlayerFighterNew>();
        if (fighter != null)
        {
            enemyController.TargetsInRange.Remove(fighter);
            EnemyManager.i.RemoveEnemyInRange(enemyController);
        }
    }
}
