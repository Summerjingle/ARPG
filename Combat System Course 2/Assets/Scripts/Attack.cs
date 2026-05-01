using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    Animator animator;
    int hasAttackCount=Animator.StringToHash("AttackCount");
    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out animator);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public int AttackCount
    {
        get=>animator.GetInteger(hasAttackCount);
        set=>animator.SetInteger(hasAttackCount,value);
    }
}
