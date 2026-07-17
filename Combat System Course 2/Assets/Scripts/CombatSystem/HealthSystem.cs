using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [field: SerializeField] public float MaxHealth { get;  set; } = 100f;
    [field: SerializeField] public float Health { get;  set; } = 100f;

    public event Action<HealthSystem, HealthChangeInfo> OnHealthChanged;
    public event Action<HealthSystem> OnDeath;
    public event Action<HealthSystem> OnDeathComplete;

    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        Health = MaxHealth;
    }

    private void Start()
    {
        // 向飘字管理器注册，确保动态生成的敌人也能被监听到
        if (FloatingTextManager.Instance != null)
            FloatingTextManager.Instance.RegisterHealthSystem(this);
    }

    private void OnDestroy()
    {
        if (FloatingTextManager.Instance != null)
            FloatingTextManager.Instance.UnregisterHealthSystem(this);
    }

    public void TakeDamage(float damage, int armor = 0, bool isCrit = false)
    {
        if (IsDead) return;

        //���㻤�׼���
        float damageReduction = armor * 0.005f;
        float reducedDamage = damage * (1 - Mathf.Clamp(damageReduction, 0, 0.8f));

        float previousHealth = Health;
        Health = Mathf.Clamp(Health - reducedDamage, 0, MaxHealth);

        Debug.Log($"[HealthSystem] {gameObject.name} 受伤: rawDamage={damage}, armor={armor}, " +
                  $"reducedDamage={reducedDamage:F1}, MaxHealth={MaxHealth}, " +
                  $"Health: {previousHealth} → {Health}, delta={Health - previousHealth}, isCrit={isCrit}");

        var info = new HealthChangeInfo
        {
            delta = Health - previousHealth,
            isCrit = isCrit
        };
        OnHealthChanged?.Invoke(this, info);

        // �������
        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            OnDeath?.Invoke(this);        // ������ʼ�¼�
            OnDeathComplete?.Invoke(this); // ��������¼�
        }
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead) return;

        float previousHealth = Health;
        Health = Mathf.Clamp(Health + amount, 0, MaxHealth);

        var info = new HealthChangeInfo
        {
            delta = Health - previousHealth,
            isCrit = false
        };
        OnHealthChanged?.Invoke(this, info);
    }

    public void SetMaxHealth(float maxHealth, bool restoreToFull = false)
    {
        MaxHealth = maxHealth;
        float previousHealth = Health;
        if (restoreToFull)
        {
            Health = MaxHealth;
        }
        else
        {
            // 上限缩小或Health超限时，clamp血量到新上限
            Health = Mathf.Min(Health, MaxHealth);
        }

        var info = new HealthChangeInfo
        {
            delta = Health - previousHealth,
            isCrit = false
        };
        OnHealthChanged?.Invoke(this, info);
    }

    /// <summary>增减最大生命值（用于体力加成/装备加成等），自动clamp当前血量</summary>
    public void AddMaxHealth(float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;

        float previousHealth = Health;
        MaxHealth += delta;
        Health = Mathf.Min(Health, MaxHealth);

        var info = new HealthChangeInfo
        {
            delta = Health - previousHealth,
            isCrit = false
        };
        OnHealthChanged?.Invoke(this, info);
    }

    public void ResetHealth()
    {
        IsDead = false;
        float previousHealth = Health;
        Health = MaxHealth;

        var info = new HealthChangeInfo
        {
            delta = Health - previousHealth,
            isCrit = false
        };
        OnHealthChanged?.Invoke(this, info);
    }

    // �������
    public float HealthPercent => MaxHealth > 0 ? Health / MaxHealth : 0f;
    public bool IsFullHealth => Health >= MaxHealth;
    public bool IsAlive => !IsDead;
}