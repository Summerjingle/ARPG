using TMPro;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("NPC»ù´¡ÉèÖÃ")]
    public string npcID;
    public TextMeshProUGUI myName;
    [Header("Î»ÖÃÆ«ÒÆ")]
    public Vector3 positionOffset = new Vector3(0, 2f, 0);
    public bool faceCamera = true;
    private Camera mainCamera;
    public bool isInteractable = true;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        myName.transform.rotation = mainCamera.transform.rotation;
    }

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