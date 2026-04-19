using UnityEditor.Animations;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    float timePassed;
    float clipLength;//
    float clipSpeed;
    bool attack;

    private WeaponEquipmentManager weaponEquipmentManager;
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
        if(weaponEquipmentManager.GetCurrentWeapon()!=null&& weaponEquipmentManager.isWeaponDrawn)
        {
            float animSpeed=animator.GetFloat("Speed");
            if (animSpeed < 1.9f)
            {
                Debug.Log("MeleeAttack!");
            }
            else
                animator.SetTrigger("attack");
            
        }
        else
            Debug.Log("没有装备/拔出武器，无法攻击");

    }
}