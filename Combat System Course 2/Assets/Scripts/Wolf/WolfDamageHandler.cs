using UnityEngine;

public class WolfDamageHandler : MonoBehaviour
{
    private WolfController wolfController;
    private float lastDamageTime = -1f;
    private const float DAMAGE_COOLDOWN = 1.0f; // 增加到1秒
    private int lastDamageFrame = -1;
    private Collider lastDamageCollider; // 记录上次造成伤害的碰撞器

    void Awake()
    {
        wolfController = GetComponent<WolfController>();
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果狼已经死亡，不处理伤害
        if (wolfController.IsDead) return;

        // 帧级防重复：同一帧内只处理一次
        if (Time.frameCount == lastDamageFrame)
        {
            Debug.Log($"WolfDamageHandler: 同一帧内重复碰撞，忽略。帧: {Time.frameCount}");
            return;
        }

        // 碰撞器防重复：同一个碰撞器在冷却期内不重复处理
        if (lastDamageCollider == other && lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN)
        {
            Debug.Log($"WolfDamageHandler: 同一碰撞器重复触发，忽略。碰撞器: {other.name}");
            return;
        }

        // 时间防重复
        if (lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN)
        {
            Debug.Log($"WolfDamageHandler: 伤害冷却中，忽略碰撞。冷却剩余: {DAMAGE_COOLDOWN - (Time.time - lastDamageTime)}秒");
            return;
        }

        if (other.CompareTag("Hitbox") && other.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox"))
        {
            // 不再直接获取Weapon组件，而是通过攻击者获取伤害
            var attacker = other.GetComponentInParent<MeleeFighter>();
            if (attacker == null) return;

            if (attacker.Attackstate != AttackStates.Windup && attacker.Attackstate != AttackStates.Impact)
            {
                Debug.Log($"WolfDamageHandler: 攻击者不在有效攻击状态，忽略伤害。状态: {attacker.Attackstate}");
                return;
            }

            // 确认攻击目标
            if (attacker.currTarget != wolfController.Fighter && attacker.currTarget != null)
            {
                Debug.Log("WolfDamageHandler: 攻击目标不匹配，忽略伤害");
                return;
            }

            //  通过攻击者获取伤害值
            float damage = attacker.GetWeaponDamage();
            Debug.Log($"WolfDamageHandler: 有效伤害 {damage}, 攻击状态: {attacker.Attackstate}, 帧: {Time.frameCount}");

            // 更新防重复标记
            lastDamageTime = Time.time;
            lastDamageFrame = Time.frameCount;
            lastDamageCollider = other; // 记录造成伤害的碰撞器

            wolfController.TakeDamage(damage);
        }
    }

    // 每帧结束时重置帧标记（可选）
    void LateUpdate()
    {
        // 确保下一帧可以重新处理伤害
        if (Time.frameCount != lastDamageFrame)
        {
            lastDamageCollider = null;
        }
    }
}