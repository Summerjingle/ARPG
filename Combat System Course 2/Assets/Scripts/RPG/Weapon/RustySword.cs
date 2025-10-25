using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RustySword : Weapon
{
    [SerializeField] private float damage = 15f;
    public override float GetDamage()
    {
        return damage;
    }
}
