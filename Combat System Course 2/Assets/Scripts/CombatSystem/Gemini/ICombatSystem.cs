using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackStates { Idle, Windup, Impact, Cooldown }
public interface ICombatSystem
{
    // 核心战斗
    float GetWeaponDamage();
    HealthSystem HealthSystem { get; }

    // 战斗状态
    bool InAction { get; set; }
    bool IsInPassiveAction { get; }
    bool IsTakingHit { get; }
    bool InCounter { get; set; }

    // 目标管理
    ICombatSystem currTarget { get; set; }

    // 组件引用
    Animator animator { get; }

    // 特殊受击动画（Animation Event 设置，null/空 = 使用默认受击动画）
    string CurrentSpecialHitReaction { get; set; }

    // 命中记录（每刀清空，防止同一刀命中同一目标多次）
    bool RegisterHit(GameObject target);

    // 重武器
    bool IsUsingHeavyWeapon();

    // 暴击
    float CritRate { get; }
    float CritDamage { get; }

    // 事件
    event System.Action<ICombatSystem> OnGotHit;
    event System.Action OnHitComplete;
    event System.Action<GameObject> OnDamageDealt;
    void NotifyDamageDealt(GameObject target);

    Transform transform { get; }
    GameObject gameObject { get; }
    void TakeDamage(float damage, bool isCrit = false);

    IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null, bool isKnockdown = false);
    void PlayDeathAnimation(ICombatSystem attacker);
}
