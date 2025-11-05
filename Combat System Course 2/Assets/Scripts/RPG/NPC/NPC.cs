using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPCª˘¥°…Ë÷√")]
    public string npcID;
    public bool isInteractable = true;

    public virtual void OnPlayerEnterRange()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInteractPrompt();
        }
    }

    public virtual void Interact()
    {
       
    }
}