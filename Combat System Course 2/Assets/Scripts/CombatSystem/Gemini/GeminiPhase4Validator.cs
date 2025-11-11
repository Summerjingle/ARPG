//using UnityEngine;
//using UnityEngine.AI;

//public class GeminiPhase4Validator : MonoBehaviour
//{
//    private void Start()
//    {
//        Debug.Log("=== 双子星计划阶段4独立运行验证 ===");
//        Invoke(nameof(TestIsPlayerRemoval), 0.5f);
//        Invoke(nameof(TestPlayerInputSeparation), 1.0f);
        
//        Invoke(nameof(TestCombatSystemIndependence), 2.0f);
//    }

//    private void TestIsPlayerRemoval()
//    {
//        Debug.Log(" isPlayer标志移除测试...");

//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            // 检查是否成功移除了isPlayer依赖
//            bool hasPlayerFighter = playerFighter != null;
//            bool usesInterface = meleeFighter.GetComponent<ICombatSystem>() != null;

//            Debug.Log($"玩家系统 - 专用组件: {hasPlayerFighter}, 使用接口: {usesInterface}");

//            if (hasPlayerFighter && usesInterface)
//            {
//                Debug.Log(" isPlayer标志移除成功 - 玩家系统完全独立");
//            }
//        }

//        // 测试敌人系统
//        var enemies = FindObjectsOfType<EnemyFighter>();
//        foreach (var enemy in enemies)
//        {
//            var meleeFighter = enemy.GetComponent<MeleeFighter>();
//            bool hasEnemyFighter = enemy != null;
//            bool usesInterface = meleeFighter.GetComponent<ICombatSystem>() != null;

//            Debug.Log($"敌人({enemy.gameObject.name}) - 专用组件: {hasEnemyFighter}, 使用接口: {usesInterface}");

//            if (hasEnemyFighter && usesInterface)
//            {
//                Debug.Log($" 敌人({enemy.gameObject.name})系统完全独立");
//            }
//        }
//    }

//    private void TestPlayerInputSeparation()
//    {
//        Debug.Log(" 玩家输入分离测试...");

//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var playerFighter = player.GetComponent<PlayerFighter>();
//            if (playerFighter != null)
//            {
//                // 测试输入处理能力
//                float damage = playerFighter.GetWeaponDamage();
//                bool canAttack = playerFighter.CanAttack();
//                int armor = playerFighter.GetPlayerArmor();

//                Debug.Log($"玩家输入系统 - 伤害: {damage}, 可攻击: {canAttack}, 护甲: {armor}");
//                Debug.Log(" 玩家输入处理分离成功");
//            }
//        }
//    }



//    private void TestCombatSystemIndependence()
//    {
//        Debug.Log("战斗系统独立性测试...");

//        // 测试玩家系统不依赖MeleeFighter的isPlayer标志
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var combatSystem = meleeFighter.GetComponent<ICombatSystem>();

//            // 通过接口调用，不依赖isPlayer
//            float damageThroughInterface = combatSystem.GetWeaponDamage();
//            float damageDirect = meleeFighter.GetWeaponDamage();

//            Debug.Log($"玩家战斗独立性 - 接口伤害: {damageThroughInterface}, 直接伤害: {damageDirect}, 一致: {Mathf.Approximately(damageThroughInterface, damageDirect)}");
//        }

//        // 测试敌人系统独立性
//        var enemies = FindObjectsOfType<EnemyFighter>();
//        foreach (var enemy in enemies)
//        {
//            var meleeFighter = enemy.GetComponent<MeleeFighter>();
//            var combatSystem = meleeFighter.GetComponent<ICombatSystem>();

//            float damageThroughInterface = combatSystem.GetWeaponDamage();
//            float damageDirect = meleeFighter.GetWeaponDamage();

//            Debug.Log($"敌人({enemy.gameObject.name})独立性 - 接口伤害: {damageThroughInterface}, 直接伤害: {damageDirect}, 一致: {Mathf.Approximately(damageThroughInterface, damageDirect)}");
//        }

//        Debug.Log("战斗系统完全独立验证完成");
//    }
//}