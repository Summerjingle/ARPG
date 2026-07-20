using System.Collections;
using UnityEngine;

public class Bonfire : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    [Header("Respawn Point")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Animator bookAnimator;

    private bool isResting;

    public int Priority => 200;
    public bool CanInteract => !isResting;
    public string PlayerAnimationTrigger => null;

    public void Interact()
{
        isResting = true;
        OnInteracted?.Invoke();
        bookAnimator.SetTrigger("BookOpen");

    }
    public void OnCheckpointReached()//给动画事件末尾调用（书打开）
{
        Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rot = respawnPoint != null ? respawnPoint.rotation : transform.rotation;
        SaveManager.Instance?.SetCheckpoint(pos, rot);
        SaveManager.Instance?.SaveGame(updatePosition: false);

        BonfirePanelCtrl.Instance?.Show(this);
    }

    public void OnPanelClosed()
{
        isResting = false;
        bookAnimator.SetTrigger("BookClose");
    }
}
