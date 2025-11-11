using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    EnemyController targetEnemy;
    private CameraController cam;
    private Animator animator;
    private ICombatSystem combatSystem;

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
        animator = GetComponent<Animator>();
        cam = Camera.main.GetComponent<CameraController>();
        combatSystem = GetComponent<ICombatSystem>();
    }

    private void Start()
    {
        combatSystem.OnGotHit += (ICombatSystem attacker) =>
        {
            // 自动进入战斗模式
            CombatMode = true;

            // 尝试获取攻击者的EnemyController
            EnemyController attackerEnemy = attacker.gameObject.GetComponent<EnemyController>();
            WolfController attackerWolf = attacker.gameObject.GetComponent<WolfController>();

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
        if (combatMode && (TargetEnemy == null || TargetEnemy.Fighter.HealthSystem.IsDead || !TargetEnemy.gameObject.activeInHierarchy))
        {
            ExitCombatMode();
        }

        // 执行攻击
        if (Input.GetButtonDown("Attack") && !combatSystem.IsTakingHit)
        {
            var enemy = EnemyManager.i.GetAttackingEnemy();
            if (enemy != null && enemy.Fighter.IsCounterable && !combatSystem.InAction)
            {
                StartCoroutine(PerformCounterAttack(enemy));
            }
            else
            {
                var enemyToAttack = EnemyManager.i.GetClosestEnemyToDirection(PlayerController.i.GetIntentDirection());

                combatSystem?.TryToAttack(enemyToAttack?.Fighter);

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
        if (!combatSystem.InCounter)
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
    public IEnumerator PerformCounterAttack(EnemyController opponent)
    {
        // 检查对手是否是狼，如果是狼则不执行处决动画
        if (opponent.GetComponent<WolfController>() != null)
        {
            Debug.LogWarning("Counterattack 对狼无效，改为普通攻击");
            // 对狼执行普通攻击
            combatSystem?.TryToAttack(opponent.Fighter);
            yield break;
        }

        combatSystem.InAction = true;
        combatSystem.InCounter = true;
        opponent.healthBar.healthBarBG.enabled = false;
        opponent.healthBar.healthBarFill.enabled = false;
        opponent.healthBar.myName.enabled = false;
        opponent.Fighter.InCounter = true;
        opponent.ChangerState(EnemyStates.Dead);

        var dispVec = opponent.transform.position - transform.position;
        dispVec.y = 0f;
        transform.rotation = Quaternion.LookRotation(dispVec);
        opponent.transform.rotation = Quaternion.LookRotation(-dispVec);

        var targetPos = opponent.transform.position - dispVec.normalized * 1f;

        animator.CrossFade("Counterattack", 0.2f);
        opponent.Animator.CrossFade("CounterattackVictim", 0.2f);

        yield return null;

        var animstate = animator.GetNextAnimatorStateInfo(1);

        float timer = 0f;
        while (timer <= animstate.length)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, 5 * Time.deltaTime);
            yield return null;
            timer += Time.deltaTime;
        }

        combatSystem.InCounter = false;
        opponent.Fighter.InCounter = false;
        combatSystem.InAction = false;
    }

    public void CancelCombatForDodge()
    {
        if (TargetEnemy != null)
        {
            TargetEnemy.MeshHighlighter?.HighlightMesh(false);
        }
        CombatMode = false;
        // 注意：这里不设置TargetEnemy为null，以便翻滚后可以快速重新锁定
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