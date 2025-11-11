#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CombatSystemValidator : EditorWindow
{
    [MenuItem("Tools/战斗系统/验证所有战斗实体")]
    public static void ValidateAllCombatEntities()
    {
        var meleeFighters = FindObjectsOfType<MeleeFighter>();
        int errorCount = 0;
        int warningCount = 0;

        foreach (var fighter in meleeFighters)
        {
            var combatSystem = fighter.GetComponent<ICombatSystem>();
            if (combatSystem == null)
            {
                Debug.LogError($" 战斗实体缺失: {fighter.gameObject.name} 有 MeleeFighter 但无 ICombatSystem 实现", fighter.gameObject);
                errorCount++;
            }
            else
            {
                // 检查必要的组件
                var animator = fighter.GetComponent<Animator>();
                var healthSystem = fighter.GetComponent<HealthSystem>();

                if (animator == null)
                {
                    Debug.LogWarning($" 缺少Animator: {fighter.gameObject.name}", fighter.gameObject);
                    warningCount++;
                }

                if (healthSystem == null)
                {
                    Debug.LogWarning($"缺少HealthSystem: {fighter.gameObject.name}", fighter.gameObject);
                    warningCount++;
                }

                Debug.Log($" 验证通过: {fighter.gameObject.name} ({combatSystem.GetType().Name})", fighter.gameObject);
            }
        }

        Debug.Log($"验证完成: {meleeFighters.Length} 个战斗实体, {errorCount} 个错误, {warningCount} 个警告");

        if (errorCount > 0)
        {
            EditorUtility.DisplayDialog("战斗系统验证",
                $"发现 {errorCount} 个错误！请查看控制台输出。", "确定");
        }
    }

    [MenuItem("Tools/战斗系统/快速修复缺失组件")]
    public static void QuickFixMissingComponents()
    {
        var meleeFighters = FindObjectsOfType<MeleeFighter>();
        int fixedCount = 0;

        foreach (var fighter in meleeFighters)
        {
            var combatSystem = fighter.GetComponent<ICombatSystem>();
            if (combatSystem == null)
            {
                // 根据对象名称或标签猜测类型
                if (fighter.CompareTag("Player"))
                {
                    fighter.gameObject.AddComponent<PlayerFighter>();
                    Debug.Log($"为玩家添加 PlayerFighter: {fighter.gameObject.name}", fighter.gameObject);
                    fixedCount++;
                }
                else if (fighter.CompareTag("Enemy"))
                {
                    fighter.gameObject.AddComponent<EnemyFighter>();
                    Debug.Log($"为敌人添加 EnemyFighter: {fighter.gameObject.name}", fighter.gameObject);
                    fixedCount++;
                }
            }
        }

        Debug.Log($"快速修复完成: 修复了 {fixedCount} 个实体");
    }   
}
#endif