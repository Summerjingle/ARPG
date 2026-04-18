using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    void Interact();
    int Priority { get; }   // 
    bool CanInteract { get; }
}
