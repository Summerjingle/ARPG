using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineTrigger : MonoBehaviour, IInteractable
{
    public LockedMachine Machine;
    private Animator triggerAnim;
    private bool isActivated = false;
    public bool CanInteract => !isActivated;

    public virtual int Priority => 10;
    private void Start()
    {
        triggerAnim = GetComponent<Animator>();
    }

    public void Interact()
    {
        triggerAnim.SetTrigger("Activate");
        isActivated = true;
    }
    public void ActivateTheGate()
    {
        Machine.OpenMachine();
        Destroy(this);
    }
}
