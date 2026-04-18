using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private WeaponEquipmentManager weaponEquipmentManager;
    void Start()
    {
        weaponEquipmentManager=GetComponent<WeaponEquipmentManager>();
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
        if(weaponEquipmentManager.GetCurrentWeapon()!=null)
            Debug.Log("Attack!");
        else
            Debug.Log("没有装备武器，无法攻击");
        
    }
}