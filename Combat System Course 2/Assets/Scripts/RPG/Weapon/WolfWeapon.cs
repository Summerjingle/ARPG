using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfWeapon : Weapon
{
    [SerializeField] private float damage = 10f;
    public override float GetDamage()
    {
        return damage;
    }

}
