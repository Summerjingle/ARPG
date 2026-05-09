using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> interactablesInRange = new();
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = InputManager.Instance.Actions;
    }
    private void OnEnable()
    {
        InputManager.Instance.OnInteract += OnInteract;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnInteract -= OnInteract;
    }
   

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
            UpdatePromptTarget(); // 刷新目标
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
           interactablesInRange.Remove(interactable);
           UpdatePromptTarget(); // 刷新目标
        }
    }

    private void OnInteract()
    {
        
        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueActive)
            return;


        interactablesInRange.RemoveAll(i => i == null || !i.CanInteract);

        if (interactablesInRange.Count == 0)
            UIManager.Instance?.HideInteractPrompt();

        // ѡȡ������ȼ�����
        IInteractable target = null;
        int maxPriority = int.MinValue;

        foreach (var i in interactablesInRange)
        {
            if (!i.CanInteract) continue; 
            if (i.Priority > maxPriority)
            {
                maxPriority = i.Priority;
                target = i;
            }
        }

        target?.Interact();
        StartCoroutine(RefreshPromptNextFrame());
    }
    private System.Collections.IEnumerator RefreshPromptNextFrame()
    {
        yield return null; // ��һ֡

        interactablesInRange.RemoveAll(i => i == null || !i.CanInteract);

        UpdatePromptTarget(); // 交互完刷新目标
    }
    private void UpdatePromptTarget()
    {
        if (interactablesInRange.Count > 0)
        {
            var targetComponent = interactablesInRange[interactablesInRange.Count - 1] as Component;
            if (targetComponent != null)
            {
                // 【新增逻辑】：尝试去拿目标身上的 ItemSO
                ItemSO targetItem = null;
                var interactableObj = targetComponent.GetComponent<InteractableObject>();
                if (interactableObj != null)
                {
                    targetItem = interactableObj.itemSO; // 如果是战利品，这里就能拿到数据
                }

                // 把 Transform 和 ItemSO 一起传过去
                UIManager.Instance?.ShowInteractPrompt(targetComponent.transform, targetItem);
            }
        }
        else
        {
            UIManager.Instance?.HideInteractPrompt();
        }
    }
    
}

