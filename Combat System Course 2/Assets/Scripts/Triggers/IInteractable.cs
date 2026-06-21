using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    event System.Action OnInteracted;
    void Interact();
    int Priority { get; }   //
    bool CanInteract { get; }
}
