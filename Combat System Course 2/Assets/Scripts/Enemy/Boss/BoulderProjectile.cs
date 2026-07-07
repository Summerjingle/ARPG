using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderProjectile : MonoBehaviour, ICombatSystem
{
    [HideInInspector] public float damage = 30f;
    [HideInInspector] public string specialHitReaction = "RockHit";
    [HideInInspector] public ICombatSystem ownerFighter;

    [Header("Config")]
    public float lifetime = 8f;

    private Vector3 velocity;
    private float gravity;
    private float timer;
    private bool launched;

    // ==========================================
    // ICombatSystem —— 让玩家 Hitbox 管线识别 Boulder
    // ==========================================
    private HashSet<int> hitTargets = new HashSet<int>();
    public ICombatSystem currTarget { get; set; }
    public HealthSystem HealthSystem => ownerFighter?.HealthSystem;
    public bool InAction { get; set; }
    public bool IsInPassiveAction => false;
    public bool IsTakingHit => false;
    public bool InCounter { get; set; }
    public float CritRate => ownerFighter?.CritRate ?? 0f;
    public float CritDamage => ownerFighter?.CritDamage ?? 1f;
    public string CurrentSpecialHitReaction { get => specialHitReaction; set { } }
    public bool IsCurrentAttackKnockdown => true; // 巨石命中 = 击倒
    public Animator animator => null;

    public event System.Action<ICombatSystem> OnGotHit;
    public event System.Action OnHitComplete;
    public event System.Action<GameObject> OnDamageDealt;

    Transform ICombatSystem.transform => transform;
    GameObject ICombatSystem.gameObject => gameObject;

    private void Awake()
    {
        gravity = Physics.gravity.y;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 标记为 Hitbox，触发玩家的 OnTriggerEnter 管线
        var col = GetComponent<Collider>();
        if (col != null) col.tag = "Hitbox";
    }

    /// <summary>由 BossRangedAttackState 在投掷瞬间调用</summary>
    public void Launch(Vector3 targetPos, float speed)
    {
        Vector3 startPos = transform.position;

        Vector3 horizontalDisplacement = new Vector3(
            targetPos.x - startPos.x,
            0f,
            targetPos.z - startPos.z
        );
        float horizontalDist = horizontalDisplacement.magnitude;

        if (horizontalDist < 0.01f)
        {
            velocity = (targetPos - startPos).normalized * speed;
            launched = true;
            return;
        }

        float timeOfFlight = horizontalDist / speed;
        timeOfFlight = Mathf.Clamp(timeOfFlight, 0.3f, 3f);

        float vx = horizontalDisplacement.x / timeOfFlight;
        float vz = horizontalDisplacement.z / timeOfFlight;
        float vy = (targetPos.y - startPos.y - 0.5f * gravity * timeOfFlight * timeOfFlight) / timeOfFlight;

        velocity = new Vector3(vx, vy, vz);
        launched = true;
    }

    private void Update()
    {
        if (!launched) return;

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y += gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        Vector3 flatVel = new Vector3(velocity.x, 0f, velocity.z);
        if (flatVel.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(flatVel.normalized);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched) return;

        var otherCombat = other.GetComponentInParent<ICombatSystem>();
        if (otherCombat == null || otherCombat == ownerFighter) return;

        // 伤害 + 受击反应由目标方的 OnTriggerEnter（Hitbox 管线）处理
        // 这里只负责接触后销毁
        Destroy(gameObject);
    }

    // ==========================================
    // ICombatSystem 方法
    // ==========================================
    public float GetWeaponDamage() => damage;
    public bool IsUsingHeavyWeapon() => false;

    public bool RegisterHit(GameObject target)
    {
        int id = target.GetInstanceID();
        if (hitTargets.Contains(id)) return false;
        hitTargets.Add(id);
        return true;
    }

    public void TakeDamage(float dmg, bool isCrit = false) { }
    public void NotifyDamageDealt(GameObject target) { }

    public IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null, bool isKnockdown = false)
    {
        yield break;
    }

    public void PlayDeathAnimation(ICombatSystem attacker) { }
}
