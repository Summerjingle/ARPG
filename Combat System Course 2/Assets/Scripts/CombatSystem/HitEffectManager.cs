using UnityEngine;

public class HitEffect : MonoBehaviour
{
    public static HitEffect Instance;

    private void Awake()
    {
        Instance = this;
    }

    // ������Ч
    public void PlaySound(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        AudioManager.Instance.PlaySFX(clip, pos, 1f);
    }

    // �������� FX
    public void PlayFX(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;

        GameObject fx = GameObject.Instantiate(prefab, pos, rot);
        GameObject.Destroy(fx, 2f);    // �Զ�����
    }
}
