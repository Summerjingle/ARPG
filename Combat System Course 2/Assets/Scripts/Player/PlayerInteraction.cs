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

        if (interactablesInRange.Count == 0)
            UIManager.Instance?.HideInteractPrompt();
    }
}
