using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    public GameObject openDoorTipText;
    public ItemSO requiredItem;
    public AudioClip OpenSound;
    public AudioClip CanNotOpenSound;

    private bool isPlayerInTrigger = false;
    private bool isOpened = false;
    private Animator animator;
    private BoxCollider doorCollider;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider>();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpened)
        {
            isPlayerInTrigger = true;
            openDoorTipText.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            openDoorTipText.SetActive(false);
        }
    }
    private void Update()
    {
        if(!isOpened && isPlayerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            TryToOpen();
        }
    }
    private void TryToOpen()//尝试开门
    {
        bool hasRequiredItem = false;
        hasRequiredItem = InventoryManager.Instance.HasItem(requiredItem);//检查背包内是否有指定物品，返回bool值
        if (hasRequiredItem)
        {
            InventoryManager.Instance.RemoveItem(requiredItem);
            Debug.Log("指定的任务道具已移除");
            AudioSource.PlayClipAtPoint(OpenSound, transform.position);
            animator.SetTrigger("Open");
            openDoorTipText.SetActive(false);
            isOpened = true;
            doorCollider.enabled = false;
        }
        else
        {
            AudioSource.PlayClipAtPoint(CanNotOpenSound, transform.position);
            MessageUI.Instance.Show("需要钥匙解锁");
        }
    }
}
