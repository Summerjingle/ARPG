using UnityEngine;

public class PickableObject : InteractableObject
{
    public override int Priority => 100;

    public override void Interact()
    {
        InventoryManager.Instance.AddItem(itemSO);
        UIManager.Instance.ShowPickupToast(itemSO);
        Destroy(gameObject);
    }
}
