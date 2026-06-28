using UnityEngine;

public class WolfDamageHandler : MonoBehaviour
{
    private WolfController wolfController;
    private float lastDamageTime = -1f;
    private const float DAMAGE_COOLDOWN = 0.5f;
    private int lastDamageFrame = -1;
    private Collider lastDamageCollider;

    void Awake()
    {
        wolfController = GetComponent<WolfController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (wolfController.IsDead) return;

        // ���ظ��߼����ֲ���...
        if (Time.frameCount == lastDamageFrame) return;
        if (lastDamageCollider == other && lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN) return;
        if (lastDamageTime >= 0 && Time.time - lastDamageTime < DAMAGE_COOLDOWN) return;

        if (other.CompareTag("Hitbox") && other.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox"))
        {
            // ֱ��ʹ�� ICombatSystem �ӿ�
            var attacker = other.GetComponentInParent<ICombatSystem>();
            if (attacker == null) return;

            // ֱ��ͨ���ӿڼ�鹥��״̬
            if (attacker.Attackstate != AttackStates.Windup &&
                attacker.Attackstate != AttackStates.Impact)
            {
                Debug.Log($"WolfDamageHandler: �����߲�����Ч����״̬�������˺���״̬: {attacker.Attackstate}");
                return;
            }

            // ȷ�Ϲ���Ŀ��
            if ((attacker.currTarget as UnityEngine.Object) != wolfController.Fighter &&
            (attacker.currTarget as UnityEngine.Object) != null)
            {
                Debug.Log("WolfDamageHandler: ����Ŀ�겻ƥ�䣬�����˺�");
                return;
            }

            // ʹ�ýӿڻ�ȡ�˺�ֵ
            float damage = attacker.GetWeaponDamage();
            Debug.Log($"WolfDamageHandler: ��Ч�˺� {damage}, ������: {attacker.GetType().Name}, ֡: {Time.frameCount}");

            // ���·��ظ����
            lastDamageTime = Time.time;
            lastDamageFrame = Time.frameCount;
            lastDamageCollider = other;

            wolfController.TakeDamage(damage);

            // 通知攻击方：成功造成伤害（用于命中转向等）
            attacker.NotifyDamageDealt(gameObject);
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