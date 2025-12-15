using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallFallTrap : MonoBehaviour
{
    public Rigidbody ballRb;
    public Rigidbody[] bricks;
    public float dropSpeed = 15f;
    private bool triggered;

    public LayerMask groundLayer; // 地面 Layer，用于触发判断

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        // 球体下落
        ballRb.isKinematic = false;
        ballRb.useGravity = true;
        ballRb.velocity = Vector3.down * dropSpeed;

        // 砖块下落
        foreach (var rb in bricks)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // 设置为 Trigger，避免碰撞
            Collider col = rb.GetComponent<Collider>();
            col.isTrigger = true;
        }
        StartCoroutine(BrickDstroy());
    }

    IEnumerator BrickDstroy()
    {
        yield return new WaitForSeconds(2);
        foreach (var rb in bricks)
        {
            if (rb != null)
            {
                Destroy(rb.gameObject);
            }

        }
    }
}


