using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private NPC currentNPC; // 当前可交互的NPC
    private PickableObject currentPickable;//当前可交互的Item
    void OnTriggerEnter(Collider other)
    {
       

        if (other.CompareTag("NPC"))//碰到NPC
        {
            currentNPC = other.GetComponent<NPC>();
            if (currentNPC != null && currentNPC.isInteractable)
            {
                currentNPC.OnPlayerEnterRange();
                Debug.Log("碰到npc");
            }
        }
        else if (other.CompareTag("Interactable"))//碰到物品
        {
            Debug.Log($"检测到Interactable物体: {other.gameObject.name}");

            // 优先处理静态场景物品
            StaticSceneItem staticItem = other.GetComponent<StaticSceneItem>();
            if (staticItem != null)
            {
                staticItem.PickUp();
                return;
            }

            // 然后是普通可拾取物品（敌人掉落等）
            PickableObject po = other.GetComponent<PickableObject>();
            if (po != null)
            {
                currentPickable = other.GetComponent<PickableObject>();
            }
            else
            {
                Debug.LogWarning("有Interactable标签但没有可拾取组件");
            }
        }//碰到物品
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            if (currentNPC != null)
            {
                currentNPC = null;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideInteractPrompt();
            }
        }
        else if (other.CompareTag("Interactable"))
        {
            if (currentPickable != null &&
                other.gameObject == currentPickable.gameObject)
            {
                currentPickable = null;
            }
        }
    }

    void Update()
    {
        // 如果在对话中，不处理交互
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentPickable != null)
            {
                // 优先处理可拾取物品
                InventoryManager.Instance.AddItem(currentPickable.itemSO);
                UIManager.Instance.ShowPickupToast(currentPickable.itemSO);
                Destroy(currentPickable.gameObject);
                currentPickable = null;
            }
            else if (currentNPC != null && currentNPC.isInteractable)
            {
                // 没有可拾取物品时再与 NPC 交互
                currentNPC.Interact();
            }
        }
    }
}