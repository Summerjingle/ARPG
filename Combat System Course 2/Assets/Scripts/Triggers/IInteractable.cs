using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    event System.Action OnInteracted;
    void Interact();
    int Priority { get; }   //
    bool CanInteract { get; }
    string PlayerAnimationTrigger { get; }  // 交互时玩家播放的动画名，null 或空 = 不播放
}
