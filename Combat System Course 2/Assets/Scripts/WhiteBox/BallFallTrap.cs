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

    public LayerMask groundLayer; // ���� Layer�����ڴ����ж�

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

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

        // // 再等 2 秒后销毁
        
        yield return new WaitForSeconds(2f);
        foreach (var rb in bricks)
        {
            if (rb != null)
            {
                Destroy(rb.gameObject);
            }
        }
    }
}

