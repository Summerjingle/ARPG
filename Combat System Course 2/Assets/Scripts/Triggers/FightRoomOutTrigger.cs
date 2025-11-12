using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightRoomOutTrigger : MonoBehaviour
{
    private Animator animator;
    public ItemSO requiredItem;
    public GameObject openDoorTipText;
    private bool isDoorCanOpen = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        bool hasRequiredItem = false;
        hasRequiredItem = InventoryManager.Instance.HasItem(requiredItem);//检查背包内是否有指定物品，返回bool值
        if (other.CompareTag("Player")&&hasRequiredItem)
        {
            if (openDoorTipText != null) { 
                openDoorTipText.SetActive(true);
            }
            isDoorCanOpen=true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        bool hasRequiredItem = false;
        hasRequiredItem = InventoryManager.Instance.HasItem(requiredItem);//检查背包内是否有指定物品，返回bool值
        if (other.CompareTag("Player") && hasRequiredItem)
            {
                openDoorTipText.SetActive(false);
            }
    }
    private void Update()
    {
        if (isDoorCanOpen && Input.GetKeyDown(KeyCode.E)) 
        {
            animator.SetTrigger("Open");
        }
    }
}
