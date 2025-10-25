using UnityEngine;

public class NPC : MonoBehaviour
{
    public string npcName = "NPC";
    public bool isInteractable = true;

    public virtual void OnPlayerEnterRange()
    {
        // 直接显示交互提示UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInteractPrompt();
        }
    }

    public virtual void Interact()
    {
        // 基类NPC没有交互逻辑，由子类实现
    }
}