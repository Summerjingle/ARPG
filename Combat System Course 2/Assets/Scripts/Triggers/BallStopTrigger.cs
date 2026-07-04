using UnityEngine;

public class BallStopTrigger : MonoBehaviour
{
    public Rigidbody ballRb;
    public AudioSource audioSource;
    public AudioClip hitSound;

    public PlayerFighter player;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != ballRb) return;

        // 播放音效
        if (audioSource != null && hitSound != null)
            audioSource.PlayOneShot(hitSound);

        // 停止球
        ballRb.isKinematic = true;
        ballRb.useGravity = false;
        ballRb.velocity = Vector3.zero;
        player.InAction=false;
    }
}