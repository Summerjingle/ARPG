using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestOpener : MonoBehaviour
{
    public LootTable lootTable;
    private bool isOpened = false;
    private Animator chestAnim;
    private bool isPlayerInTrigger = false;
    public GameObject openChestTipText;
    [Header("机关系统")]
    public SwitchMechanism chestMechanism;
    [Header("碰撞器设置")]
    public BoxCollider triggerCollider; // 触发检测的碰撞器

    private void Start()
    {
        chestAnim = GetComponent<Animator>();
        Debug.Log($"ChestOpener Start - chestMechanism: {chestMechanism != null}");

        // 延迟一帧检查，确保 SwitchMechanism 已经初始化完成
        StartCoroutine(DelayedActivationCheck());
    }
    private IEnumerator DelayedActivationCheck()
    {
        // 等待一帧，确保所有组件的 Start 方法都执行完毕
        yield return null;

        Debug.Log($"延迟检查 - chestMechanism: {chestMechanism != null}, IsActivated: {chestMechanism?.IsActivated()}");

        if (chestMechanism != null && chestMechanism.IsActivated())
        {
            SetChestAsOpened();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isOpened)
        {
            isPlayerInTrigger = true;
            openChestTipText.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
         if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
            openChestTipText.SetActive(false);
        }
    }
    private void Update()
    {
        if (!isOpened && isPlayerInTrigger && Input.GetKeyDown(KeyCode.E))
        {
            chestAnim.SetTrigger("ChestOpen");
            openChestTipText.SetActive(false);
            isOpened = true;
            if (triggerCollider != null)
                triggerCollider.enabled = false;
            StartCoroutine(SpawItem());
            chestMechanism.Activate(); // 记录到存档
            Debug.Log("宝箱状态已保存到存档");
        }
    }
    private void SetChestAsOpened()
    {
        // 直接从存档加载开启状态
        chestAnim.Play("ChestOpen", -1, 1f); // 直接播放到动画最后一帧
        openChestTipText.SetActive(false);
        isOpened = true;
        // 禁用触发器碰撞器
        if (triggerCollider != null)
            triggerCollider.enabled = false;
        Debug.Log("宝箱已从存档加载开启状态");
    }
    private IEnumerator SpawItem()
    {
        yield return new WaitForSeconds(1f);
        Vector3 chestForward = transform.forward;
        Vector3 spawnCenter = transform.position + chestForward * 0.5f + Vector3.up * 1f;

        LootSpawner.SpawnLootItems(spawnCenter, lootTable, transform);

    }

}
