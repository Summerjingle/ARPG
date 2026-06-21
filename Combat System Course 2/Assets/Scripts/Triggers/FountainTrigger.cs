using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FountainTrigger : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

   public FountainDirection direction;
    public FountainTurner turner;
    private Animator triggerAnim;
    private bool isActivated = false;
    public bool CanInteract => !isActivated && !turner.IsRotating;

    public virtual int Priority => 10;
    private void Start()
{
        triggerAnim = GetComponent<Animator>();
    }
    public void Interact()
{
        triggerAnim.SetTrigger("Activate");
        OnInteracted?.Invoke();
        turner.SetDirection(direction);
        
    }
}
