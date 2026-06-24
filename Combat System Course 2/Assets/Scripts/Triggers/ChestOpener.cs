using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestOpener : MonoBehaviour, IInteractable
{
    public event System.Action OnInteracted;

    public LootTable lootTable;
    private bool isOpened = false;
    public Animator chestAnim;
    [Header("Save System")]
    public SwitchMechanism chestMechanism;
    [Header("Collider Reference")]
    public BoxCollider triggerCollider;

    public int Priority => 90;
    public bool CanInteract => !isOpened;
    public string PlayerAnimationTrigger => "Kick";

    private void Start()
    {
        
        Debug.Log($"ChestOpener Start - chestMechanism: {chestMechanism != null}");

        // Delay one frame to ensure SwitchMechanism has initialized
        StartCoroutine(DelayedActivationCheck());
    }
    private IEnumerator DelayedActivationCheck()
    {
        yield return null;

        Debug.Log($"Delayed check - chestMechanism: {chestMechanism != null}, IsActivated: {chestMechanism?.IsActivated()}");

        if (chestMechanism != null && chestMechanism.IsActivated())
        {
            SetChestAsOpened();
        }
    }
    public void Interact()
    {
        if (isOpened) return;

        OnInteracted?.Invoke();

        if (chestAnim != null && chestAnim.runtimeAnimatorController != null)
            chestAnim.SetTrigger("ChestOpen");
        isOpened = true;
        StartCoroutine(SpawnItem());
        chestMechanism?.Activate();
        Debug.Log("Chest state saved to save file");
    }

    private void SetChestAsOpened()
    {
        // SwitchMechanism.Awake() 已把 Animator 跳到 restoreStateName 最后一帧
        // 这里只设置逻辑状态
        isOpened = true;
        Debug.Log("Chest restored to opened state from save");
    }
    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(1f);
        Vector3 chestForward = transform.forward;
        Vector3 spawnCenter = transform.position + chestForward * 0.5f + Vector3.up * 1f;

        LootSpawner.SpawnLootItems(spawnCenter, lootTable, transform, ejectFromChest: true);
        yield return new WaitForSeconds(0.1f); 
        Destroy(this);
    }

}
