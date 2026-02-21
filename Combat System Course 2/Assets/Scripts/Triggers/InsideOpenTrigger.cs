using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsideOpenTrigger : MonoBehaviour, IInteractable
{
    public LockedMachine lockedDoor;
    public virtual int Priority => 10;
    private bool isActivated = false;
    public bool CanInteract => !isActivated;
    

    public void Interact()
    {
        lockedDoor.OpenMachine();
        isActivated = true;
        Destroy(this);
    }
    

   
}
