using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRangeState : State<BossController>
{
    public override void Enter(BossController owner)
    {
        base.Enter(owner);
        Debug.Log("<color=red>Boss 进入了 [range] 状态：远程攻击！</color>");
    }
}
