using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    private List<IInteractable> interactablesInRange = new();
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void OnTriggerEnter(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null && !interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
            UIManager.Instance?.ShowInteractPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var interactable = other.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactablesInRange.Remove(interactable);
            if (interactablesInRange.Count == 0)
                UIManager.Instance?.HideInteractPrompt();
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        // 对话中禁止交互
        if (DialogueManager.Instance != null &&
            DialogueManager.Instance.IsDialogueActive)
            return;


        interactablesInRange.RemoveAll(i => i == null || !i.CanInteract);

        if (interactablesInRange.Count == 0)
            UIManager.Instance?.HideInteractPrompt();

        // 选取最大优先级对象
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
        yield return null; // 等一帧

        interactablesInRange.RemoveAll(i => i == null || !i.CanInteract);

        if (interactablesInRange.Count == 0)
            UIManager.Instance?.HideInteractPrompt();
    }
}
