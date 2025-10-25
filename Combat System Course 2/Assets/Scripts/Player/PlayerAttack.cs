using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Weapon weapon;//目前装备的武器

    private void Update()
    {
        
    }
    public void LoadWeapon(Weapon weapon)//装备武器
    {
        this.weapon = weapon; 
    }
    public void UnLoadWeapon()//卸下武器
    {
        weapon=null; 
    }
}
