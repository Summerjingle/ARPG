using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackStates { Idle, Windup, Impact, Cooldown }
public interface ICombatSystem
{
    // 基础战斗属性
    bool CanAttack();
    void TryToAttack(ICombatSystem target = null);
    float GetWeaponDamage();
    bool HasUsableWeapon();

    // 状态管理
    void UpdateAttackState(float normalizedTime, AttackData attack);
    void ResetAttackState();

    // Hitbox管理
    void EnableHitbox(AttackData attack);
    void DisableHitboxes();

    // 攻击数据选择
    AttackData SelectAttack(ICombatSystem target, int comboCount);
    Vector3 CalculateAttackDirection(ICombatSystem target);
    Vector3 CalculateAttackPosition(ICombatSystem target, AttackData attack, Vector3 attackDir, Vector3 startPos);

    // 战斗流程控制
    void PrepareAttack(ICombatSystem target);
    void FinishAttack();

    // 连击系统
    bool CheckComboCondition();
    IEnumerator ExecuteAttack(ICombatSystem target, int comboCount);

    // 生命系统
    HealthSystem HealthSystem { get; }

    // 战斗状态
    bool InAction { get; set; }
    bool IsTakingHit { get; }
    bool InCounter { get; set; }
    bool IsCounterable { get; }

    // 攻击状态
    AttackStates Attackstate { get; set; }
    bool docombo { get; set; }
    int comboCount { get; set; }

    // 目标管理
    ICombatSystem currTarget { get; set; }

    // 攻击数据
    List<AttackData> Attacks { get; }
    List<AttackData> LongRangeAttacks { get; }
    float LongRangeAttackThreshold { get; }

    // 组件引用
    Animator animator { get; }
    BoxCollider WeaponCollider { get; }
    SphereCollider leftHandCollider { get; }
    SphereCollider rightHandCollider { get; }
    SphereCollider leftFootCollider { get; }
    SphereCollider rightFootCollider { get; }

    // 特殊受击动画（Animation Event 设置，null/空 = 使用默认受击动画）
    string CurrentSpecialHitReaction { get; set; }

    // 命中记录（每刀清空，防止同一刀命中同一目标多次）
    bool RegisterHit(GameObject target);

    // 事件
    event System.Action<ICombatSystem> OnGotHit;
    event System.Action OnHitComplete;
    Transform transform { get; }
    GameObject gameObject { get; }
    void TakeDamage(float damage);

    IEnumerator PlayHitReaction(ICombatSystem attacker, string specialHitReaction = null);
    void PlayDeathAnimation(ICombatSystem attacker);
}