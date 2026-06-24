using UnityEngine;

public class ElevatorLever : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    public ElevatorController elevator;
    public Animator leverAnimator;

    public int Priority => 10;
    public bool CanInteract => true;
    public string PlayerAnimationTrigger => null;

    public void Interact()
{
        if (elevator == null) return;

        leverAnimator.SetTrigger("Activate");
        OnInteracted?.Invoke();
    }

    // �����¼�
    public void ActivateElevator()
{
        elevator.RequestOperate();
    }
}