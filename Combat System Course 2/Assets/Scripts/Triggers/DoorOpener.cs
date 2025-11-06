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

    [Header("机关系统")]
    public SwitchMechanism doorMechanism;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider>();
    }
    private void Start()
    {
        Debug.Log($"DoorOpener Start - doorMechanism: {doorMechanism != null}");

        // 延迟一帧检查，确保 SwitchMechanism 已经初始化完成
        StartCoroutine(DelayedActivationCheck());
    }
    private IEnumerator DelayedActivationCheck()
    {
        // 等待一帧，确保所有组件的 Start 方法都执行完毕
        yield return null;

        Debug.Log($"延迟检查 - doorMechanism: {doorMechanism != null}, IsActivated: {doorMechanism?.IsActivated()}");

        if (doorMechanism != null && doorMechanism.IsActivated())
        {
            SetDoorAsOpened();
        }
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
            OpenDoor();
            
            if (doorMechanism != null)
            {
                doorMechanism.Activate();// 记录机关状态
            }
            Debug.Log("门已开启并记录状态");
        }
        else
        {
            AudioSource.PlayClipAtPoint(CanNotOpenSound, transform.position);
            MessageUI.Instance.Show("需要钥匙解锁");
        }
    }

    private void OpenDoor()
    {
        AudioSource.PlayClipAtPoint(OpenSound, transform.position);
        animator.SetTrigger("Open");
        openDoorTipText.SetActive(false);
        isOpened = true;
        doorCollider.enabled = false;
    }

    private void SetDoorAsOpened()
    {
        animator.Play("OpenDoor", -1, 1f);
        openDoorTipText.SetActive(false);
        isOpened = true;
        doorCollider.enabled = false;
        Debug.Log("门已从存档加载开启状态");
    }
}
