using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    EnemyController targetEnemy;
    private CameraController cam;
    private MeleeFighter meleeFighter;
    private Animator animator;

    public EnemyController TargetEnemy
    {
        get => targetEnemy;
        set
        {
            targetEnemy = value;
            if (targetEnemy == null) combatMode = false;
        }
    }

    bool combatMode;
    public bool CombatMode
    {
        get => combatMode;
        set
        {
            combatMode = value;
            if (TargetEnemy == null)
            {
                combatMode = false;
            }
            animator.SetBool("combatMode", combatMode);
        }
    }

    private void Awake()
    {
        meleeFighter = GetComponent<MeleeFighter>();
        animator = GetComponent<Animator>();
        cam = Camera.main.GetComponent<CameraController>();
    }

    private void Start()
    {
        meleeFighter.OnGotHit += (MeleeFighter attacker) =>
        {
            // 自动进入战斗模式
            CombatMode = true;

            // 尝试获取攻击者的EnemyController
            EnemyController attackerEnemy = attacker.GetComponent<EnemyController>();
            WolfController attackerWolf = attacker.GetComponent<WolfController>();

            EnemyController targetToSet = null;

            if (attackerEnemy != null)
            {
                targetToSet = attackerEnemy;
            }
            else if (attackerWolf != null && attackerWolf.EnemyController != null)
            {
                targetToSet = attackerWolf.EnemyController;
            }

            // 如果有有效的攻击者，设置为目标
            if (targetToSet != null)
            {
                // 取消之前目标的高亮
                if (TargetEnemy != null && TargetEnemy != targetToSet)
                {
                    TargetEnemy.MeshHighlighter?.HighlightMesh(false);
                }

                // 设置新目标并高亮
                TargetEnemy = targetToSet;
                TargetEnemy.MeshHighlighter?.HighlightMesh(true);
            }
            else
            {
                // 如果没有特定目标，让EnemyManager自动选择最近的敌人
                var closestEnemy = EnemyManager.i.GetClosestEnemyToDirection(GetTargetingDir());
                if (closestEnemy != null)
                {
                    if (TargetEnemy != null && TargetEnemy != closestEnemy)
                    {
                        TargetEnemy.MeshHighlighter?.HighlightMesh(false);
                    }
                    TargetEnemy = closestEnemy;
                    TargetEnemy.MeshHighlighter?.HighlightMesh(true);
                }
            }
        };
    }

    private void Update()
    {
        // 新增：检查目标敌人是否死亡或无效
        if (combatMode && (TargetEnemy == null || TargetEnemy.Fighter.IsDead || !TargetEnemy.gameObject.activeInHierarchy))
        {
            ExitCombatMode();
        }

        // 执行攻击
        if (Input.GetButtonDown("Attack") && !meleeFighter.IsTakingHit)
        {
            var enemy = EnemyManager.i.GetAttackingEnemy();
            if (enemy != null && enemy.Fighter.IsCounterable && !meleeFighter.InAction)
            {
                StartCoroutine(meleeFighter.PerfromCounterAttack(enemy));
            }
            else
            {
                var enemyToAttack = EnemyManager.i.GetClosestEnemyToDirection(PlayerController.i.GetIntentDirection());

                meleeFighter.TryToAttack(enemyToAttack?.Fighter);

                CombatMode = true;
            }
        }

        if (Input.GetButtonDown("LockOn"))
        {
            CombatMode = !CombatMode;
        }
    }

    private void OnAnimatorMove()
    {
        if (!meleeFighter.InCounter)
        {
            transform.position += animator.deltaPosition;
        }

        transform.rotation *= animator.deltaRotation;
    }

    public Vector3 GetTargetingDir()
    {
        if (!combatMode)
        {
            var vecForCam = transform.position - cam.transform.position;
            vecForCam.y = 0;
            return vecForCam.normalized;
        }
        else
        {
            return transform.forward;
        }
    }
    private void ExitCombatMode()
    {
        // 清理高亮显示
        if (TargetEnemy != null)
        {
            var highlighter = TargetEnemy.GetComponent<SkinnedMashHighlighter>();
            if (highlighter != null)
                highlighter.HighlightMesh(false);
        }

        // 重置目标
        TargetEnemy = null;
        CombatMode = false;
    }
}