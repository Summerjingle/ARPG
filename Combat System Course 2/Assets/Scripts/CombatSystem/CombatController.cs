using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    private CameraController cam;
    private ICombatSystem combatSystem;
    private Animator animator;
    private EnemyLockSystem lockSystem;

    public bool CombatMode => lockSystem != null && lockSystem.IsLocked;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        cam = Camera.main.GetComponent<CameraController>();
        combatSystem = GetComponent<ICombatSystem>();
        lockSystem = GetComponent<EnemyLockSystem>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Attack") && !combatSystem.IsTakingHit)
        {
            var enemy = EnemyManager.i.GetAttackingEnemy();
            if (enemy != null && !enemy.IsUncounterable && enemy.Fighter.IsCounterable && !combatSystem.InAction)
            {
                StartCoroutine(PerformCounterAttack(enemy));
            }
            else
            {
                ICombatSystem target = lockSystem?.currentTarget?.GetComponent<EnemyController>()?.Fighter;
                combatSystem?.TryToAttack(target);
            }
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
        if (!CombatMode)
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
        combatSystem.InAction = true;
        combatSystem.InCounter = true;

        if (opponent.healthBar?.healthBarBG != null)
            opponent.healthBar.healthBarBG.enabled = false;
        if (opponent.healthBar?.healthBarFill != null)
            opponent.healthBar.healthBarFill.enabled = false;
        if (opponent.healthBar?.myName != null)
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

   
}