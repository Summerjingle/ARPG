using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfFighter : EnemyFighter
{
    private WolfController wolfController;

    protected override void Awake()
    {
        wolfController = GetComponent<WolfController>();
        base.Awake(); // 调用父类初始化基础组件
    }

    // 只需要重写碰撞器初始化 - 狼不需要骨骼碰撞器
    protected override void InitializeEnemyBodyColliders()
    {
        // 狼跳过骨骼碰撞器初始化
        Debug.Log($"狼 {gameObject.name} 跳过骨骼碰撞器初始化");

        // 只初始化武器碰撞器（如果有的话）
        if (enemyWeapon != null)
        {
            WeaponCollider = enemyWeapon.GetComponentInChildren<BoxCollider>();
            if (WeaponCollider != null)
            {
                Debug.Log($"初始化狼武器碰撞器: {WeaponCollider.name}");
            }
        }
    }
    public override void TakeDamage(float damage)
    {
        if (HealthSystem.IsDead) return;

        base.TakeDamage(damage);

        // 检测死亡并通知 WolfController
        if (HealthSystem.IsDead && wolfController != null)
        {
            wolfController.HandleWolfDeath();
        }
    }
}
