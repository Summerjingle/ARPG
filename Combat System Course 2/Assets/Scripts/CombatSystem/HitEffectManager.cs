using UnityEngine;

public class HitEffect : MonoBehaviour
{
    public static HitEffect Instance;

    private void Awake()
    {
        Instance = this;
    }

    // 播放音效
    public void PlaySound(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(clip, pos, 1f);
    }

    // 播放粒子 FX
    public void PlayFX(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;

        GameObject fx = GameObject.Instantiate(prefab, pos, rot);
        GameObject.Destroy(fx, 2f);    // 自动销毁
    }
}
