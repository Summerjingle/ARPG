using System.Collections;
using UnityEngine;

public class HitDelay : MonoBehaviour
{
    public static HitDelay Instance;
    private bool isHitStopping = false;


    private void Awake()
    {
        Instance = this;
    }

    public void Stop(float duration, Animator animator)
    {
        if (!isHitStopping)
            StartCoroutine(DoHitStop(duration, animator));
    }

    private IEnumerator DoHitStop(float duration, Animator animator)
    {
        isHitStopping = true;

        float originalSpeed = animator.speed;
        animator.speed = 0f;

        yield return new WaitForSecondsRealtime(duration);

        animator.speed = originalSpeed;
        isHitStopping = false;
    }
    // 供动画事件调用的播放音效方法
    public void PlaySwingSound(AudioClip clip)
    {
        if (clip != null)
            AudioManager.Instance.PlaySFX(clip, transform.position);
    }
}
