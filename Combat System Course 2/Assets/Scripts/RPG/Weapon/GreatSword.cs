using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreatSword : Weapon
{
    [SerializeField] private float damage = 100f;
    public override float GetDamage()
    {
        return damage;
    }
}
