using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    private ICombatSystem combatSystem;
    private Animator animator;
    private EnemyLockSystem lockSystem;

    public bool CombatMode => lockSystem != null && lockSystem.IsLocked;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        combatSystem = GetComponent<ICombatSystem>();
        lockSystem = GetComponent<EnemyLockSystem>();
    }

  

    private void OnAnimatorMove()
    {
        // 只处理旋转，位移由各自主体的移动系统（如 PlayerController）负责，
        // 避免多个 OnAnimatorMove 重复应用 animator.deltaPosition 导致抖动
        transform.rotation *= animator.deltaRotation;
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