using UnityEngine;
using TMPro;

public class NPC : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    [Header("NPC��������")]
    public string npcID;
    public TextMeshProUGUI myName;

    
    public bool isInteractable = true;

   
    public int Priority => 50;

    
    public bool CanInteract => isInteractable;
    public string PlayerAnimationTrigger => null;

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

        // ԭ�жԻ��߼�
        
    }

    // ��ѡ���ڶԻ��а� NPC ��ǲ��ɽ���
    public void SetInteractable(bool value)
{
        isInteractable = value;
    }
}
