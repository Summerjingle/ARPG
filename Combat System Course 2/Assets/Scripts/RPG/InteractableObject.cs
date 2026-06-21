using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;
    public ItemSO itemSO;
    public bool isActivated = false;
    public bool CanInteract => !isActivated;
    public virtual int Priority => 100;

    protected virtual void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    public virtual void Interact()
    {
        isActivated = true;
        OnInteracted?.Invoke();
    }
}
