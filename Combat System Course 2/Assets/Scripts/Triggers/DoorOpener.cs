using UnityEngine;

public class DoorOpener : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;
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

        OnInteracted?.Invoke();

        if (InventoryManager.Instance.HasItem(requiredItem))
        {
            InventoryManager.Instance.RemoveItem(requiredItem);
            OpenDoor();

            doorMechanism?.Activate();
        }
        else
        {
            MessageUI.Instance.Show("需要钥匙解锁");
        }
    }

    private void OpenDoor()
    {
        animator.SetTrigger("Open");
        isOpened = true;
        doorCollider.enabled = false;
    }
}
