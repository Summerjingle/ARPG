using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialOpenBag : MonoBehaviour
{
    public GameObject OpenBagTips;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;

            // 激活提示并开始监听按键
            OpenBagTips.SetActive(true);

            // 开始协程监听按键
            StartCoroutine(WaitForInput());

            // 禁用碰撞体而不是销毁，让脚本继续运行
            GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator WaitForInput()
    {
        // 等待玩家按下I键
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.I));

        // 隐藏提示
        OpenBagTips.SetActive(false);

        // 现在才销毁整个对象
        Destroy(gameObject);
    }
}