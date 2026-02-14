using UnityEngine;

public class PickableObject : InteractableObject
{
    public override int Priority => 100;

    public override void Interact()
    {
        if (!CanInteract) return; // 防止重复交互

        base.Interact(); // 设置 isActivated = true

        InventoryManager.Instance.AddItem(itemSO);
        UIManager.Instance.ShowPickupToast(itemSO);

        // 关键：禁用碰撞器，触发 OnTriggerExit
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // 这会触发 OnTriggerExit
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 确保标志被设置
        isActivated = true;
    }
}