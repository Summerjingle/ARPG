using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallFallTrap : MonoBehaviour
{
    public Rigidbody ballRb;
    public Rigidbody[] bricks;
    public float dropSpeed = 30f;
    public float brickAcceleration = 5f;
    private bool triggered;
    public GameObject[] gameObjects;
    public GameObject savedBall;// 载入存档的时候，如果已经触发过坠球陷阱的话，就删除gameObjects、bricks和ballRb，然后active该物体

    [Header("存档联动")]
    public SwitchMechanism trapMechanism;

    public LayerMask groundLayer; // 地面 Layer，用于坠落判断

    private void Start()
    {
        // 读档恢复：如果之前已经触发过，直接显示坠后状态
        if (trapMechanism != null && trapMechanism.IsActivated())
        {
            triggered = true;
            ShowPostTrapState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        other.GetComponent<ICombatSystem>().InAction = true;
        triggered = true;

        // 持久化到存档
        trapMechanism?.Activate();

        // 短暂关掉球的 Collider，让站在球上的人物失去地面接触 → 触发 Falling
        StartCoroutine(BallDropRoutine());

        // 地板消失
        foreach (var go in gameObjects)
        {
            go.SetActive(false);
        }

        // 砖块开启物理，加初始向下速度
        foreach (var rb in bricks)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.down * brickAcceleration;
        }

        StartCoroutine(BrickDestroy());
    }

    IEnumerator BallDropRoutine()
    {
        // 拿到球的所有 Collider，暂时关掉
        Collider[] ballCols = ballRb.GetComponents<Collider>();
        foreach (var col in ballCols)
            col.enabled = false;

        // 球开始下落
        ballRb.isKinematic = false;
        ballRb.useGravity = true;
        ballRb.velocity = Vector3.down * dropSpeed;

        // 等人物脱离接触，进入 Falling 状态（fallStartDelay = 0.15s）
        yield return new WaitForSeconds(0.15f);

        // 恢复球的 Collider，之后球落地人才能站上去
        foreach (var col in ballCols)
            col.enabled = true;
    }

    IEnumerator BrickDestroy()
    {
        // 等物理散落稳定（1.5秒），再变成 Trigger 避免堵路
        yield return new WaitForSeconds(1.5f);
        foreach (var rb in bricks)
        {
            if (rb != null)
            {
                Collider col = rb.GetComponent<Collider>();
                if (col != null)
                    col.isTrigger = true;
            }
        }

        // 再等 2 秒后销毁
        yield return new WaitForSeconds(2f);
        foreach (var rb in bricks)
        {
            if (rb != null)
            {
                Destroy(rb.gameObject);
            }
        }
    }

    /// <summary>
    /// 读档时直接显示坠落后的状态（跳过物理动画过程）
    /// </summary>
    private void ShowPostTrapState()
    {
        // 删除地板
        foreach (var go in gameObjects)
        {
            if (go != null)
                Destroy(go);
        }

        // 删除砖块
        foreach (var rb in bricks)
        {
            if (rb != null)
                Destroy(rb.gameObject);
        }

        // 删除空中原始球
        if (ballRb != null)
            Destroy(ballRb.gameObject);

        // 显示坠落后的球
        if (savedBall != null)
            savedBall.SetActive(true);

        Debug.Log($"BallFallTrap {trapMechanism.mechanismId} 已从存档恢复坠后状态");
    }
}
