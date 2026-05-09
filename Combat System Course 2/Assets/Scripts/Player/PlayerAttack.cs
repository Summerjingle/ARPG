
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    float timePassed;
    float clipLength;//
    float clipSpeed;
    bool attack;

    private WeaponEquipmentManager weaponEquipmentManager;
    private bool canCombo = true;
    private Animator animator;
    void Start()
    {
        weaponEquipmentManager=GetComponent<WeaponEquipmentManager>();
        animator=GetComponent<Animator>();
    }
    private void OnEnable()
    {
        InputManager.Instance.OnAttack += TryAttack;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnAttack -= TryAttack;
    }

    private void TryAttack()
    {
        if (!canCombo) return;
        
        if(weaponEquipmentManager.GetCurrentWeapon()!=null&& weaponEquipmentManager.isWeaponDrawn)
        {
            // 清理可能残留的 Trigger，确保动画机状态干净
            animator.ResetTrigger("MeleeAttack");
            animator.ResetTrigger("attack");

            float animSpeed=animator.GetFloat("Speed");
            if (animSpeed < 1.9f)
            {
                animator.SetTrigger("MeleeAttack");
                Debug.Log("MeleeAttack!");
            }
            else
                animator.SetTrigger("attack");
            canCombo = false;
            animator.applyRootMotion = true;
           if (PlayerController.i != null && PlayerController.i.combatSystem != null)
            {
                PlayerController.i.combatSystem.InAction = true;
            }
            
        }
        else
            Debug.Log("没有装备/拔出武器，无法攻击");

    }
    public void OnAttackEnd()
    {
        if (!canCombo) 
        {
            return; 
        }
        // 动画结束，关闭 Root Motion，交还给代码控制位移
        animator.applyRootMotion = false;
        canCombo = true;
        
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = false; // 确保解锁
            if (PlayerController.i.combatSystem != null)
            {
                PlayerController.i.combatSystem.InAction = false;
            }

        }
    }
    // 停止转向：在动画开始突进或发力时调用
    public void StartRotationLock()
    {
        
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = true;
          
        }
    }

    // 恢复转向：在动画收招或允许玩家微调方向时调用
    public void StopRotationLock()
    {
        if (PlayerController.i != null)
        {
            PlayerController.i.LockRotation = false;
            canCombo = true; 
        }
        
    }
    
}