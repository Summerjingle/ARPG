using TMPro;
using UnityEngine;

public class PickableObject : InteractableObject
{
    public override int Priority => 100;
    public System.Action onInteract;

    // 不设 isTrigger，Player 身上的 trigger collider 已足够检测
    protected override void Start() { }

    public override void Interact()
    {
        if (!CanInteract) return;

        base.Interact();

        onInteract?.Invoke();

        InventoryManager.Instance.AddItem(itemSO);
        UIManager.Instance.ShowPickupToast(itemSO);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        isActivated = true;
    }
}
