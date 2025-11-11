using System.Linq;
using UnityEngine;

public class GeminiPhase3Validator : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== 双子星计划阶段3引力同步验证 ===");
        Invoke(nameof(TestCombatInterfaceUnification), 0.5f);
        Invoke(nameof(TestInterfaceImplementation), 1.0f);
        Invoke(nameof(TestBackwardCompatibility), 1.5f);
    }

    private void TestCombatInterfaceUnification()
    {
        Debug.Log(" 战斗接口统一性测试...");

        // 测试玩家接口实现
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerCombat = player.GetComponent<ICombatSystem>();
            var playerFighter = player.GetComponent<PlayerFighter>();

            Debug.Log($"玩家ICombatSystem接口: {playerCombat != null}");
            Debug.Log($"玩家PlayerFighter组件: {playerFighter != null}");
            Debug.Log($"玩家接口一致性: {playerCombat != null && playerFighter != null}");

            if (playerCombat != null)
            {
                bool canAttack = playerCombat.CanAttack();
                float damage = playerCombat.GetWeaponDamage();
                bool hasWeapon = playerCombat.HasUsableWeapon();

                Debug.Log($"玩家战斗状态 - 可攻击: {canAttack}, 伤害: {damage}, 有武器: {hasWeapon}");
            }
        }

        // 测试敌人接口实现
        var enemies = FindObjectsOfType<EnemyFighter>();
        foreach (var enemy in enemies)
        {
            var enemyCombat = enemy.GetComponent<ICombatSystem>();
            Debug.Log($"敌人({enemy.gameObject.name}) ICombatSystem接口: {enemyCombat != null}");

            if (enemyCombat != null)
            {
                bool canAttack = enemyCombat.CanAttack();
                float damage = enemyCombat.GetWeaponDamage();
                bool hasWeapon = enemyCombat.HasUsableWeapon();

                Debug.Log($"敌人({enemy.gameObject.name})战斗状态 - 可攻击: {canAttack}, 伤害: {damage}, 有武器: {hasWeapon}");
            }
        }
    }

    private void TestInterfaceImplementation()
    {
        Debug.Log(" 接口实现完整性测试...");

        var allCombatSystems = FindObjectsOfType<MonoBehaviour>().OfType<ICombatSystem>();
        Debug.Log($"找到 {allCombatSystems.Count()} 个战斗系统实现");

        foreach (var system in allCombatSystems)
        {
            var monoBehaviour = system as MonoBehaviour;
            if (monoBehaviour != null)
            {
                Debug.Log($"战斗系统: {monoBehaviour.gameObject.name} - 类型: {system.GetType().Name}");

                // 测试基本方法
                try
                {
                    float damage = system.GetWeaponDamage();
                    bool hasWeapon = system.HasUsableWeapon();
                    bool canAttack = system.CanAttack();

                    Debug.Log($"   基本方法测试通过 - 伤害: {damage}, 有武器: {hasWeapon}, 可攻击: {canAttack}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"   基本方法测试失败: {e.Message}");
                }
            }
        }
    }

    private void TestBackwardCompatibility()
    {
        Debug.Log("向后兼容性测试...");

        // 测试原有MeleeFighter功能是否正常
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var meleeFighter = player.GetComponent<MeleeFighter>();
            if (meleeFighter != null)
            {
                // 测试原有方法
                float oldDamage = meleeFighter.GetWeaponDamage();
                bool oldHasWeapon = meleeFighter.HasUsableWeapon();

                Debug.Log($"原有系统 - 伤害: {oldDamage}, 有武器: {oldHasWeapon}");
                Debug.Log($"MeleeFighter状态 - InAction: {meleeFighter.InAction}, AttackState: {meleeFighter.Attackstate}");

                // 测试接口访问
                var combatSystem = meleeFighter.GetComponent<ICombatSystem>();
                if (combatSystem != null)
                {
                    float newDamage = combatSystem.GetWeaponDamage();
                    bool newHasWeapon = combatSystem.HasUsableWeapon();

                    Debug.Log($"新旧系统一致性 - 伤害: {Mathf.Approximately(oldDamage, newDamage)}, 武器状态: {oldHasWeapon == newHasWeapon}");
                }
            }
        }
    }
}