using TMPro;
using UnityEngine;

public class PickableObject : InteractableObject
{
    public override int Priority => 100;
    

    
    public override void Interact()
    {
        if (!CanInteract) return; // ��ֹ�ظ�����

        base.Interact(); // ���� isActivated = true

        InventoryManager.Instance.AddItem(itemSO);
        UIManager.Instance.ShowPickupToast(itemSO);

        // �ؼ���������ײ�������� OnTriggerExit
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false; // ��ᴥ�� OnTriggerExit
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // ȷ����־������
        isActivated = true;
    }
}