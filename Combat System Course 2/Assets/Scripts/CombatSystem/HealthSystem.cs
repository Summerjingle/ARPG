using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [field: SerializeField] public float MaxHealth { get;  set; } = 25f;
    [field: SerializeField] public float Health { get;  set; } = 25f;

    public event Action<HealthSystem> OnHealthChanged;
    public event Action<HealthSystem> OnDeath;
    public event Action<HealthSystem> OnDeathComplete;

    public bool IsDead { get; private set; } = false;

    private void Awake()
    {
        Health = MaxHealth;
    }

    public void TakeDamage(float damage, int armor = 0)
    {
        if (IsDead) return;

        // 计算护甲减免
        float damageReduction = armor * 0.005f;
        float reducedDamage = damage * (1 - Mathf.Clamp(damageReduction, 0, 0.8f));

        float previousHealth = Health;
        Health = Mathf.Clamp(Health - reducedDamage, 0, MaxHealth);

        // 触发血量变化事件
        OnHealthChanged?.Invoke(this);

        // 检查死亡
        if (Health <= 0 && !IsDead)
        {
            IsDead = true;
            OnDeath?.Invoke(this);        // 死亡开始事件
            OnDeathComplete?.Invoke(this); // 死亡完成事件
        }
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead) return;

        Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
        OnHealthChanged?.Invoke(this);
    }

    public void SetMaxHealth(float maxHealth, bool restoreToFull = false)
    {
        MaxHealth = maxHealth;
        if (restoreToFull)
        {
            Health = MaxHealth;
        }
        OnHealthChanged?.Invoke(this);
    }

    public void ResetHealth()
    {
        IsDead = false;
        Health = MaxHealth;
        OnHealthChanged?.Invoke(this);
    }

    // 便捷属性
    public float HealthPercent => MaxHealth > 0 ? Health / MaxHealth : 0f;
    public bool IsFullHealth => Health >= MaxHealth;
    public bool IsAlive => !IsDead;
}