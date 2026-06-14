using System.Collections;
using UnityEngine;

public class Bonfire : MonoBehaviour, IInteractable
{
    [Header("Respawn Point")]
    [SerializeField] private Transform respawnPoint;

    private bool isResting;

    public int Priority => 200;
    public bool CanInteract => !isResting;

    public void Interact()
    {
        isResting = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var health = player.GetComponent<HealthSystem>();
        if (health != null)
            health.RestoreHealth(health.MaxHealth);

        var prop = player.GetComponent<PlayerProperty>();
        if (prop != null)
            prop.SetEnergy(prop.MaxEnergy);

        Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rot = respawnPoint != null ? respawnPoint.rotation : transform.rotation;
        SaveManager.Instance?.SetCheckpoint(pos, rot);
        SaveManager.Instance?.SaveGame(updatePosition: false);

        BonfirePanelCtrl.Instance?.Show(this);
    }

    public void OnPanelClosed()
    {
        isResting = false;
    }
}
