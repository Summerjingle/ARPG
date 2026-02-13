using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    void Interact();
    int Priority { get; }   // 用来解决重叠时谁优先
    bool CanInteract { get; }
}
