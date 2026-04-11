using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        Debug.Log("<color=red>Boss 进入了 [die] 状态：死了！</color>");
    }
}
