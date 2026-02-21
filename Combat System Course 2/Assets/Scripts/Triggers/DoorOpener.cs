using UnityEngine;

public class DoorOpener : MonoBehaviour, IInteractable
{
    public ItemSO requiredItem;
    public SwitchMechanism doorMechanism;

    public int Priority => 5;
    public bool CanInteract => !isOpened;

    private bool isOpened = false;
    private Animator animator;
    private BoxCollider doorCollider;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider>();
    }

    public void Interact()
    {
        if (isOpened) return;

        if (InventoryManager.Instance.HasItem(requiredItem))
        {
            InventoryManager.Instance.RemoveItem(requiredItem);
            OpenDoor();

            doorMechanism?.Activate();
        }
        else
        {
            MessageUI.Instance.Show("ÐèÒªÔ¿³×½âËø");
        }
    }

    private void OpenDoor()
    {
        animator.SetTrigger("Open");
        isOpened = true;
        doorCollider.enabled = false;
    }
}