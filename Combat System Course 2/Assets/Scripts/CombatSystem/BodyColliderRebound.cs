using UnityEngine;

/// <summary>
/// 挂在身体部位碰撞器上（手/脚/身体），碰到 Obstacle 时触发攻击者的反弹。
/// 解决 Boss 没有 Weapon 组件导致反弹 VFX 不播放的问题。
/// </summary>
public class BodyColliderRebound : MonoBehaviour
{
    private EnemyFighter fighter;

    private void Awake()
    {
        fighter = GetComponentInParent<EnemyFighter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fighter == null) return;
        if (!other.CompareTag("Obstacle")) return;

        // 重武器穿透障碍物（和 Weapon.OnTriggerEnter 保持一致）
        if (fighter.IsUsingHeavyWeapon()) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        fighter.PlayReboundVfx(hitPoint);
    }
}
