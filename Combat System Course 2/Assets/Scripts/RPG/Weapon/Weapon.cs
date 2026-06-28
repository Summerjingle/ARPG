using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public ItemSO itemSO; // ����ItemSO����
    public abstract float GetDamage();
    public virtual void Initialize(ItemSO weaponItem)
    {
        itemSO = weaponItem;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle"))
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);

        var playerFighter = GetComponentInParent<PlayerFighterNew>();
        if (playerFighter != null)
        {
            playerFighter.OnWeaponRebound(hitPoint);
            return;
        }

        var enemyFighter = GetComponentInParent<EnemyFighter>();
        if (enemyFighter != null)
        {
            enemyFighter.OnWeaponRebound(hitPoint);
        }
    }
}