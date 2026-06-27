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
        // 武器碰撞到任何物体的日志（确认 Trigger 事件能触发）
        Debug.Log($"[Weapon] OnTriggerEnter with: {other.name}, tag={other.tag}, frame={Time.frameCount}");

        if (!other.CompareTag("Obstacle"))
        {
            Debug.Log($"[Weapon] 忽略非 Obstacle 物体: {other.tag}");
            return;
        }

        var fighter = GetComponentInParent<PlayerFighterNew>();
        if (fighter == null)
        {
            Debug.LogError("[Weapon] GetComponentInParent<PlayerFighterNew> 返回 null！武器不在玩家子层级下？");
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Debug.Log($"[Weapon] 检测到 Obstacle，调用 OnWeaponRebound, hitPoint={hitPoint}, frame={Time.frameCount}");
        fighter.OnWeaponRebound(hitPoint);
    }
}