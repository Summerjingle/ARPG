using UnityEngine;
using TMPro;

public class NPC : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    [Header("NPC基础设置")]
    public string npcID;
    public TextMeshProUGUI myName;

    
    public bool isInteractable = true;

   
    public int Priority => 50;

    
    public bool CanInteract => isInteractable;

    private Camera mainCamera;

    private void Start()
{
        mainCamera = Camera.main;
    }

    private void Update()
{
        if (myName != null)
            myName.transform.rotation = mainCamera.transform.rotation;
    }

    public virtual void Interact()
{
        if (!isInteractable) return;
        OnInteracted?.Invoke();

        // 原有对话逻辑
        
    }

    // 可选：在对话中把 NPC 标记不可交互
    public void SetInteractable(bool value)
{
        isInteractable = value;
    }
}
