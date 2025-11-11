//using UnityEngine;

//public class GeminiPhase1Validator : MonoBehaviour
//{
//    private void Start()
//    {
//        Debug.Log("=== 双子星计划阶段1验证 ===");

//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            Debug.Log($"玩家MeleeFighter: {meleeFighter != null}");
//            Debug.Log($"玩家PlayerFighter: {playerFighter != null}");
           
//        }

//        // 延迟执行武器伤害测试，确保所有组件初始化完成
//        Invoke(nameof(TestWeaponDamageMigration), 1f);
//        Invoke(nameof(TestWeaponAvailabilitySeparation), 1.2f);
//        Invoke(nameof(TestAttackConditionSeparation), 1.4f);
//        Invoke(nameof(TestPlayerAttackSelection), 1.6f); 
//        Invoke(nameof(TestEnemyAttackSeparation), 1.8f);
//        Invoke(nameof(TestAttackStateSeparation), 2.0f);
//        Invoke(nameof(TestHitboxControlSeparation), 2.2f);
//    }

//    private void TestWeaponDamageMigration()
//    {
//        Debug.Log("=== 武器伤害迁移验证 ===");

//        // 测试玩家
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null)
//            {
//                float oldDamage = meleeFighter.GetWeaponDamage();
//                float newDamage = playerFighter.GetWeaponDamage();

//                Debug.Log($"玩家武器伤害 - 旧系统: {oldDamage}, 新系统: {newDamage}, 一致: {Mathf.Approximately(oldDamage, newDamage)}");
//            }
//        }

//        // 测试敌人 - 通过查找所有EnemyFighter组件
//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        Debug.Log($"找到 {allEnemyFighters.Length} 个敌人进行测试");

//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                float oldDamage = meleeFighter.GetWeaponDamage();
//                float newDamage = enemyFighter.GetWeaponDamage();

//                Debug.Log($"敌人({enemyFighter.gameObject.name})武器伤害 - 旧系统: {oldDamage}, 新系统: {newDamage}, 一致: {Mathf.Approximately(oldDamage, newDamage)}");
//            }
//        }
//    }
//    private void TestWeaponAvailabilitySeparation()
//    {
//        Debug.Log("=== 武器可用性分离验证 ===");

//        // 测试玩家
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null)
//            {
//                bool oldWay = meleeFighter.HasUsableWeapon();
//                bool newWay = playerFighter.PlayerHasUsableWeapon();

//                Debug.Log($"玩家武器可用性 - 旧系统: {oldWay}, 新系统: {newWay}, 一致: {oldWay == newWay}");

//                // 额外信息：当前武器状态
//                var currentWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
//                Debug.Log($"玩家当前武器: {(currentWeapon != null ? currentWeapon.name : "无")}");
//            }
//        }

//        // 测试敌人
//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        Debug.Log($"找到 {allEnemyFighters.Length} 个敌人进行武器可用性测试");

//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                bool oldWay = meleeFighter.HasUsableWeapon();
//                bool newWay = enemyFighter.EnemyHasUsableWeapon();

//                Debug.Log($"敌人({enemyFighter.gameObject.name})武器可用性 - 旧系统: {oldWay}, 新系统: {newWay}, 一致: {oldWay == newWay}");

//                // 额外信息：敌人武器状态
//                var enemyWeapon = enemyFighter.GetComponentInChildren<Weapon>();
//                Debug.Log($"敌人武器: {(enemyWeapon != null ? enemyWeapon.name : "无")}");
//            }
//        }
//    }
//    private void TestAttackConditionSeparation()
//    {
//        Debug.Log("=== 攻击条件分离验证 ===");

//        // 测试玩家攻击条件
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null)
//            {
//                bool oldCondition = !meleeFighter.InAction && meleeFighter.HasUsableWeapon();
//                bool newCondition = playerFighter.PlayerCanAttack();

//                Debug.Log($"玩家攻击条件 - 旧系统: {oldCondition}, 新系统: {newCondition}, 一致: {oldCondition == newCondition}");
//                Debug.Log($"玩家状态 - InAction: {meleeFighter.InAction}, HasWeapon: {meleeFighter.HasUsableWeapon()}, AttackState: {meleeFighter.Attackstate}");

//                // 测试连击状态
//                bool oldComboCondition = meleeFighter.Attackstate == AttackStates.Impact || meleeFighter.Attackstate == AttackStates.Cooldown;
//                Debug.Log($"玩家连击条件: {oldComboCondition} (AttackState: {meleeFighter.Attackstate})");
//            }
//        }

//        // 测试敌人攻击条件
//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        Debug.Log($"找到 {allEnemyFighters.Length} 个敌人进行攻击条件测试");

//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                bool oldCondition = !meleeFighter.InAction && meleeFighter.HasUsableWeapon();
//                bool newCondition = enemyFighter.EnemyCanAttack();

//                Debug.Log($"敌人({enemyFighter.gameObject.name})攻击条件 - 旧系统: {oldCondition}, 新系统: {newCondition}, 一致: {oldCondition == newCondition}");
//                Debug.Log($"敌人状态 - InAction: {meleeFighter.InAction}, HasWeapon: {meleeFighter.HasUsableWeapon()}, AttackState: {meleeFighter.Attackstate}");

//                // 测试敌人连击状态
//                bool oldComboCondition = meleeFighter.Attackstate == AttackStates.Impact || meleeFighter.Attackstate == AttackStates.Cooldown;
//                Debug.Log($"敌人连击条件: {oldComboCondition} (AttackState: {meleeFighter.Attackstate})");

//                // 额外检查敌人是否有 AttackState 组件
//                var attackState = enemyFighter.GetComponent<AttackState>();
//                Debug.Log($"敌人AttackState组件: {attackState != null}");
//            }
//        }
//    }

//    private void TestEnemyAttackSeparation()
//    {
//        Debug.Log("=== 敌人攻击逻辑分离验证 ===");

//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        Debug.Log($"找到 {allEnemyFighters.Length} 个敌人进行攻击逻辑验证");

//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                // 测试敌人攻击方向计算
//                var oldDirection = meleeFighter.transform.forward;
//                var newDirection = enemyFighter.CalculateEnemyAttackDirection(null);

//                Debug.Log($"敌人({enemyFighter.gameObject.name})攻击方向 - 旧: {oldDirection}, 新: {newDirection}, 近似: {Vector3.Dot(oldDirection, newDirection) > 0.9f}");

//                // 检查敌人特定组件
//                var enemyController = enemyFighter.GetComponent<EnemyController>();
//                var wolfController = enemyFighter.GetComponent<WolfController>();

//                Debug.Log($"敌人类型 - EnemyController: {enemyController != null}, WolfController: {wolfController != null}");

//                if (enemyController != null)
//                {
//                    Debug.Log($"敌人NavAgent状态 - 启用: {enemyController.NavAgent != null && enemyController.NavAgent.enabled}");
//                }
//            }
//        }
//    }
//    private void TestPlayerAttackSelection()
//    {
//        Debug.Log("=== 玩家攻击数据选择验证 ===");

//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null && meleeFighter.Attacks.Count > 0)
//            {
//                // 测试无目标情况下的攻击选择
//                var newAttack = playerFighter.SelectPlayerAttack(null, meleeFighter.Attacks, meleeFighter.longRangeAttacks, 0, meleeFighter.LongRangeAttackThreshold);
//                var expectedAttack = meleeFighter.Attacks[0];

//                Debug.Log($"玩家无目标攻击选择 - 期望: {expectedAttack.AttackName}, 实际: {newAttack.AttackName}, 一致: {expectedAttack == newAttack}");

//                // 测试连击攻击选择
//                var comboAttack = playerFighter.SelectPlayerAttack(null, meleeFighter.Attacks, meleeFighter.longRangeAttacks, 1, meleeFighter.LongRangeAttackThreshold);
//                var expectedComboAttack = meleeFighter.Attacks[1 % meleeFighter.Attacks.Count];

//                Debug.Log($"玩家连击攻击选择 - 期望: {expectedComboAttack.AttackName}, 实际: {comboAttack.AttackName}, 一致: {expectedComboAttack == comboAttack}");

//                // 测试攻击方向计算
//                var direction = playerFighter.CalculatePlayerAttackDirection(null);
//                var expectedDirection = player.transform.forward;

//                Debug.Log($"玩家攻击方向 - 期望: {expectedDirection}, 实际: {direction}, 近似: {Vector3.Dot(expectedDirection, direction) > 0.9f}");
//            }
//        }
//    }

//    private void TestAttackStateSeparation()
//    {
//        Debug.Log("=== 攻击状态管理分离验证 ===");

//        // 测试玩家状态管理
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null)
//            {
//                // 测试状态重置
//                var oldState = meleeFighter.Attackstate;
//                var oldInAction = meleeFighter.InAction;

//                Debug.Log($"玩家当前状态 - AttackState: {oldState}, InAction: {oldInAction}");

//                // 测试连击条件检查
//                bool comboCondition = playerFighter.CheckPlayerComboCondition();
//                Debug.Log($"玩家连击条件: {comboCondition}");
//            }
//        }

//        // 测试敌人状态管理
//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                var oldState = meleeFighter.Attackstate;
//                var oldInAction = meleeFighter.InAction;

//                Debug.Log($"敌人({enemyFighter.gameObject.name})当前状态 - AttackState: {oldState}, InAction: {oldInAction}");

//                bool comboCondition = enemyFighter.CheckEnemyComboCondition();
//                Debug.Log($"敌人连击条件: {comboCondition}");
//            }
//        }
//    }

//    private void TestHitboxControlSeparation()
//    {
//        Debug.Log("=== Hitbox控制分离验证 ===");

//        // 测试玩家Hitbox控制
//        var player = GameObject.FindGameObjectWithTag("Player");
//        if (player != null)
//        {
//            var meleeFighter = player.GetComponent<MeleeFighter>();
//            var playerFighter = player.GetComponent<PlayerFighter>();

//            if (meleeFighter != null && playerFighter != null)
//            {
//                // 检查玩家Hitbox组件
//                bool hasLeftHand = meleeFighter.leftHandCollider != null;
//                bool hasRightHand = meleeFighter.rightHandCollider != null;
//                bool hasLeftFoot = meleeFighter.leftFootCollider != null;
//                bool hasRightFoot = meleeFighter.rightFootCollider != null;

//                Debug.Log($"玩家Hitbox组件 - 左手: {hasLeftHand}, 右手: {hasRightHand}, 左脚: {hasLeftFoot}, 右脚: {hasRightFoot}");

//                // 检查玩家武器
//                var playerWeapon = WeaponEquipmentManager.Instance?.GetCurrentWeapon();
//                Debug.Log($"玩家当前武器: {(playerWeapon != null ? playerWeapon.name : "无")}");
//            }
//        }

//        // 测试敌人Hitbox控制
//        var allEnemyFighters = FindObjectsOfType<EnemyFighter>();
//        foreach (var enemyFighter in allEnemyFighters)
//        {
//            var meleeFighter = enemyFighter.GetComponent<MeleeFighter>();
//            if (meleeFighter != null)
//            {
//                // 检查敌人Hitbox组件
//                bool hasLeftHand = meleeFighter.leftHandCollider != null;
//                bool hasRightHand = meleeFighter.rightHandCollider != null;
//                bool hasLeftFoot = meleeFighter.leftFootCollider != null;
//                bool hasRightFoot = meleeFighter.rightFootCollider != null;

//                Debug.Log($"敌人({enemyFighter.gameObject.name})Hitbox组件 - 左手: {hasLeftHand}, 右手: {hasRightHand}, 左脚: {hasLeftFoot}, 右脚: {hasRightFoot}");

//                // 检查敌人武器
//                var enemyWeapon = enemyFighter.GetComponentInChildren<Weapon>();
//                Debug.Log($"敌人武器: {(enemyWeapon != null ? enemyWeapon.name : "无")}");
//            }
//        }
//    }
//}