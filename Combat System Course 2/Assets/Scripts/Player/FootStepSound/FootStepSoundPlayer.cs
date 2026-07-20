using UnityEngine;

public class FootStepSoundPlayer : MonoBehaviour
{
    public AudioClip[] defaultClips;
    public AudioClip[] woodClips;
    public AudioClip[] rockClips;
    public AudioClip[] dirtClips;

    public LayerMask Environment;

    private float _lastPlayTime;

    /// <summary>
    /// 播放一次脚步声。由 Animation Event 调用（走/跑/攻击/技能等所有动画）。
    /// </summary>
    public void PlayFootstep(AnimationEvent evt)
    {
        // BlendTree 混合时 walk/run 的 Event 都会来，只响权重过半的那个
        if (evt.animatorClipInfo.weight < 0.5f) return;

        // 保险：0.5 附近两边交替过线时，避免连响
        if (Time.time - _lastPlayTime < 0.15f) return;
        _lastPlayTime = Time.time;

        var clips = GetClipsForSurface();
        var randomClip = clips[Random.Range(0, clips.Length)];
        AudioManager.Instance.PlaySFX(randomClip, transform.position);
    }

    private AudioClip[] GetClipsForSurface()
    {
        var ishit = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.25f, Environment);
        if (ishit)
        {
            var surface = hit.collider.GetComponent<SurfaceDefinition>();
            if (surface)
            {
                if (surface.SurfaceType == SurfaceType.Wood) return woodClips;
                if (surface.SurfaceType == SurfaceType.Rock) return rockClips;
                if (surface.SurfaceType == SurfaceType.Dirt) return dirtClips;
            }
        }
        return defaultClips;
    }
}
