using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class EnemyManager : MonoBehaviour
{
    [SerializeField] Vector2 timeRangeBetweenAttacks = new Vector2(1, 4);
    [SerializeField] CombatController player;
    public static EnemyManager i { get; private set; }

    private List<EnemyController> enemiesIsRange = new List<EnemyController>();
    private float notAttackingTimer = 2f;
    private float timer = 0f;


    private void Awake()
    {
        i = this;
    }

    private void Update()
    {
        if (enemiesIsRange.Count == 0) return;
        if (!enemiesIsRange.Any(e => e.IsInState(EnemyStates.Attack)))
        {
            if (notAttackingTimer > 0)
            {
                notAttackingTimer -= Time.deltaTime;
            }
            if (notAttackingTimer <= 0)
            {
                //Attack the player
                var attackingEnemy = SelectEnemyForAttack();
                if (attackingEnemy != null)
                {
                    attackingEnemy.ChangerState(EnemyStates.Attack);
                    notAttackingTimer = Random.Range(timeRangeBetweenAttacks.x, timeRangeBetweenAttacks.y);
                }

            }
        }
        if (timer >= 0.1f)
        {
            timer = 0f;
            
        }

        timer += Time.deltaTime;
    }

    private EnemyController SelectEnemyForAttack()
    {
        return enemiesIsRange.OrderByDescending(e => e.combatMovementTimer).FirstOrDefault(e => e.Target != null && e.IsInState(EnemyStates.CombatMovement));
    }

    public void AddEnemyInRange(EnemyController enemyController)
    {
        if (!enemiesIsRange.Contains(enemyController))
        {
            enemiesIsRange.Add(enemyController);
        }
    }
    public void RemoveEnemyInRange(EnemyController enemyController)
    {
        

        enemiesIsRange.Remove(enemyController);

    }

    public EnemyController GetAttackingEnemy()
    {
        return enemiesIsRange.FirstOrDefault(e => e.IsInState(EnemyStates.Attack));
    }

    public EnemyController  GetClosestEnemyToDirection(Vector3 direction)
    {
        float miniDistance = Mathf.Infinity;
        EnemyController closestEnemy = null;

        foreach (var enemy in enemiesIsRange)
        {
            var vecToEnemy = enemy.transform.position - player.transform.position;
            vecToEnemy.y = 0;

            //����
            float angle = Vector3.Angle(direction, vecToEnemy);
            float distance = vecToEnemy.magnitude * Mathf.Sin(angle * Mathf.Deg2Rad);

            if (distance < miniDistance)
            {
                miniDistance = distance;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }
}