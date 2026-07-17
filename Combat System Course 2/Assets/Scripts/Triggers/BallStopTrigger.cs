using UnityEngine;

public class BallStopTrigger : MonoBehaviour
{
    public Rigidbody ballRb;
    public AudioSource audioSource;
    public AudioClip hitSound;

    public PlayerFighter player;

    void Start()
    {
        AudioManager.RouteToSFX(audioSource);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Rigidbody>() != ballRb) return;

        // 播放音效
        if (hitSound != null)
            AudioManager.Instance.PlaySFX(hitSound, transform.position);

        // 停止球
        ballRb.isKinematic = true;
        ballRb.useGravity = false;
        ballRb.velocity = Vector3.zero;
        player.InAction=false;
    }
}