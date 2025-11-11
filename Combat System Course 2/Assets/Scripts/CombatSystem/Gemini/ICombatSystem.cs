using System.Collections;
using UnityEngine;

// 统一战斗系统接口
public interface ICombatSystem
{
    // 基础战斗能力
    bool CanAttack();
    void TryToAttack(MeleeFighter target = null);
    float GetWeaponDamage();
    bool HasUsableWeapon();

    // 状态管理
    void UpdateAttackState(float normalizedTime, AttackData attack);
    void ResetAttackState();

    // Hitbox控制
    void EnableHitbox(AttackData attack);
    void DisableHitboxes();

    // 攻击数据选择
    AttackData SelectAttack(MeleeFighter target, int comboCount);
    Vector3 CalculateAttackDirection(MeleeFighter target);
    Vector3 CalculateAttackPosition(MeleeFighter target, AttackData attack, Vector3 attackDir, Vector3 startPos);

    // 战斗生命周期
    void PrepareAttack(MeleeFighter target);
    void FinishAttack();

    // 连击系统
    bool CheckComboCondition();
    IEnumerator ExecuteAttack(MeleeFighter target, int comboCount);
}