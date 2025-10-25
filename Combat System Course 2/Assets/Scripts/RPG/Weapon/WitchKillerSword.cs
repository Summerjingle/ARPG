using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WitchKillerSword : Weapon
{
    [SerializeField] private float damage = 25f;
    public override float GetDamage()
    {
        return damage;
    }
}
