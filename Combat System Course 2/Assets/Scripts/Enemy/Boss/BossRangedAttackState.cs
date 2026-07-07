using System.Collections;
using UnityEngine;

public class BossRangedAttackState : State<BossController>
{
    [Header("Ranged Attack Config")]
    public float damage = 30f;
    public string animName = "RangedAttack";
    public string specialHitReaction = "RockHit";

    private bool isAttacking;
    private bool shouldTrackPlayer;
    private GameObject spawnedBoulder;

    public override void Enter(BossController owner)
    {
        base.Enter(owner);

        if (owner.agent != null && owner.agent.isOnNavMesh)
            owner.agent.isStopped = true;

        owner.anim?.SetFloat("Speed", 0);
        owner.fighter.InCounter = false;
        isAttacking = false;
        shouldTrackPlayer = true;
        spawnedBoulder = null;
    }

    public override void Execute()
    {
        if (isAttacking) return;
        if (owner.playerTarget == null) return;

        if (!owner.CanRangedAttack())
        {
            owner.ChangeState(owner.idleState);
            return;
        }

        owner.lastRangedAttackTime = Time.time;
        StartCoroutine(DoRangedAttack());
    }

    private IEnumerator DoRangedAttack()
    {
        isAttacking = true;
        shouldTrackPlayer = true;

        owner.anim.CrossFade(animName, 0.2f, 1);
        yield return null;

        var animState = owner.anim.GetNextAnimatorStateInfo(1);
        float animLength = animState.length;

        float timer = 0f;
        while (timer < animLength)
        {
            timer += Time.deltaTime;

            if (shouldTrackPlayer && owner.playerTarget != null)
                owner.FacePlayer();

            yield return null;
        }

        // 清理残留巨石
        if (spawnedBoulder != null)
        {
            Destroy(spawnedBoulder);
            spawnedBoulder = null;
        }

        isAttacking = false;
        owner.ChangeState(owner.idleState);
    }

    /// <summary>动画事件：在手中生成巨石</summary>
    public void AnimEvent_SpawnBoulder()
    {
        if (owner.playerTarget == null) return;

        Transform spawnPoint = owner.boulderSpawnPoint;
        if (spawnPoint == null)
        {
            Debug.LogError("[BossRangedAttack] boulderSpawnPoint 未设置！");
            return;
        }

        if (owner.boulderPrefab == null)
        {
            Debug.LogError("[BossRangedAttack] boulderPrefab 未设置！");
            return;
        }

        spawnedBoulder = Instantiate(owner.boulderPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

        var projectile = spawnedBoulder.GetComponent<BoulderProjectile>();
        if (projectile != null)
        {
            projectile.damage = damage;
            projectile.specialHitReaction = specialHitReaction;
            projectile.ownerFighter = owner.fighter;
        }
    }

    /// <summary>动画事件：将巨石扔向玩家，之后停止追踪朝向</summary>
    public void AnimEvent_ThrowBoulder()
    {
        shouldTrackPlayer = false;

        if (spawnedBoulder == null || owner.playerTarget == null) return;

        spawnedBoulder.transform.SetParent(null);

        var projectile = spawnedBoulder.GetComponent<BoulderProjectile>();
        if (projectile != null)
        {
            // 设置玩家为 Hitbox 管线的目标
            var playerFighter = owner.playerTarget.GetComponent<PlayerFighter>();
            if (playerFighter != null)
                projectile.currTarget = playerFighter;

            projectile.Launch(owner.playerTarget.position, owner.boulderFlySpeed);
        }

        spawnedBoulder = null;
    }

    public override void Exit()
    {
        StopAllCoroutines();
        isAttacking = false;
        shouldTrackPlayer = false;

        if (spawnedBoulder != null)
        {
            Destroy(spawnedBoulder);
            spawnedBoulder = null;
        }

        owner.fighter.DisableHitboxes();
        if (owner.agent != null && owner.agent.isOnNavMesh)
        {
            owner.agent.isStopped = false;
            owner.agent.ResetPath();
        }
    }
}
