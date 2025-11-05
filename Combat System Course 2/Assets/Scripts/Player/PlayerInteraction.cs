using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private NPC currentNPC; // 当前可交互的NPC

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"触发进入: {other.gameObject.name}, Tag: {other.tag}, Layer: {other.gameObject.layer}");

        if (other.CompareTag("NPC"))
        {
            currentNPC = other.GetComponent<NPC>();
            if (currentNPC != null && currentNPC.isInteractable)
            {
                currentNPC.OnPlayerEnterRange();
            }
        }//碰到NPC
        else if (other.CompareTag("Interactable"))
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
                Debug.Log($"成功获取PickableObject组件，物品: {po.itemSO?.nameOfItem}");
                InventoryManager.Instance.AddItem(po.itemSO);
                Destroy(po.gameObject);
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
    }

    void Update()
    {
        // 如果在对话中，不处理交互
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && currentNPC != null && currentNPC.isInteractable)
        {
            currentNPC.Interact();
        }
    }
}