using UnityEngine;

public class WolfDamageHandler : MonoBehaviour
{
    private WolfController wolfController;
    private float lastDamageTime = -1f;
    private const float DAMAGE_COOLDOWN = 1.0f;
    private int lastDamageFrame = -1;
    private Collider lastDamageCollider;

    void Awake()
    {
        wolfController = GetComponent<WolfController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (wolfController.IsDead) return;

        // 防重复逻辑保持不变...
        if (Time.frameCount == lastDamageFrame) return;
        if (lastDamageCollider == other && lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN) return;
        if (lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN) return;

        if (other.CompareTag("Hitbox") && other.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox"))
        {
            // 直接使用 ICombatSystem 接口
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null) return;

            // 直接通过接口检查攻击状态
            if (attacker.Attackstate != AttackStates.Windup &&
                attacker.Attackstate != AttackStates.Impact)
            {
                Debug.Log($"WolfDamageHandler: 攻击者不在有效攻击状态，忽略伤害。状态: {attacker.Attackstate}");
                return;
            }

            // 确认攻击目标
            if ((attacker.currTarget as UnityEngine.Object) != wolfController.Fighter &&
            (attacker.currTarget as UnityEngine.Object) != null)
            {
                Debug.Log("WolfDamageHandler: 攻击目标不匹配，忽略伤害");
                return;
            }

            // 使用接口获取伤害值
            float damage = attacker.GetWeaponDamage();
            Debug.Log($"WolfDamageHandler: 有效伤害 {damage}, 攻击者: {attacker.GetType().Name}, 帧: {Time.frameCount}");

            // 更新防重复标记
            lastDamageTime = Time.time;
            lastDamageFrame = Time.frameCount;
            lastDamageCollider = other;

            wolfController.TakeDamage(damage);
        }
    }

    void LateUpdate()
    {
        if (Time.frameCount != lastDamageFrame)
        {
            lastDamageCollider = null;
        }
    }
}