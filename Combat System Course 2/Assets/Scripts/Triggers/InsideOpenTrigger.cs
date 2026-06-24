using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsideOpenTrigger : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    public LockedMachine lockedDoor;
    public virtual int Priority => 10;
    private bool isActivated = false;
    public bool CanInteract => !isActivated;
    public string PlayerAnimationTrigger => null;
    

    public void Interact()
{
        lockedDoor.OpenMachine();
        OnInteracted?.Invoke();
        isActivated = true;
        Destroy(this);
    }
    

   
}
