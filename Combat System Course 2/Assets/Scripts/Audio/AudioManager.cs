using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 统一音频管理：对象池播放、MixerGroup 路由、统一音量。
/// 用法：AudioManager.Instance.PlaySFX(clip, pos) 替代 AudioSource.PlayClipAtPoint
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ===== 单例 =====
    public static AudioManager Instance { get; private set; }

    // ===== Mixer Group（Inspector 拖拽赋值）=====
    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    public static AudioMixerGroup SFXGroup => Instance?.sfxGroup;
    public static AudioMixerGroup UIGroup  => Instance?.uiGroup;

    // ===== 默认音量 =====
    [Header("Default Volumes")]
    [Range(0f, 1f)] public float defaultSFXVolume = 0.8f;
    [Range(0f, 1f)] public float defaultUIVolume  = 1f;

    // ===== BGM =====
    [Header("BGM")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [Range(0f, 1f)] public float defaultBGMVolume = 1f;

    private AudioSource bgmSource;
    private Coroutine bgmFadeRoutine;

    public static AudioMixerGroup BGMGroup => Instance?.bgmGroup;

    // ===== 对象池 =====
    [Header("Pool")]
    [SerializeField] private int poolSize = 20;

    private Queue<AudioSource> pool = new Queue<AudioSource>();
    private GameObject poolRoot;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        poolRoot = new GameObject("AudioPool");
        poolRoot.transform.SetParent(transform);

        // 专用 BGM AudioSource，不走对象池
        var bgmGo = new GameObject("BGM");
        bgmGo.transform.SetParent(transform);
        bgmSource = bgmGo.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;    // 2D
        bgmSource.outputAudioMixerGroup = bgmGroup;

        for (int i = 0; i < poolSize; i++)
            CreatePooledSource();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ===== 池管理 =====

    void CreatePooledSource()
    {
        var go = new GameObject("PooledAudio");
        go.transform.SetParent(poolRoot.transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.minDistance  = 1f;
        src.maxDistance  = 30f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        go.SetActive(false);
        pool.Enqueue(src);
    }

    AudioSource GetFromPool()
    {
        if (pool.Count == 0) CreatePooledSource();
        var src = pool.Dequeue();
        src.gameObject.SetActive(true);
        return src;
    }

    void ReturnToPool(AudioSource src)
    {
        if (src == null) return;
        src.Stop();
        src.clip = null;
        src.outputAudioMixerGroup = null;
        src.spatialBlend = 1f;   // 恢复默认 3D
        src.volume = 1f;
        src.gameObject.SetActive(false);
        pool.Enqueue(src);
    }

    /// <summary>协程回收：WaitForSecondsRealtime 保证暂停时也能回收，不泄漏池</summary>
    IEnumerator ReturnAfterDelay(AudioSource src, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReturnToPool(src);
    }

    // ===== Public API =====

    /// <summary>播放 3D SFX（替代 AudioSource.PlayClipAtPoint）</summary>
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = -1f)
    {
        if (clip == null) return;
        if (volume < 0f) volume = defaultSFXVolume;

        var src = GetFromPool();
        src.transform.position = position;
        src.clip = clip;
        src.volume = volume;
        src.outputAudioMixerGroup = sfxGroup;
        src.spatialBlend = 1f;
        src.Play();

        StartCoroutine(ReturnAfterDelay(src, clip.length + 0.1f));
    }

    /// <summary>播放 2D UI 音效（不受 timeScale 影响）</summary>
    public void PlayUI(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;
        if (volume < 0f) volume = defaultUIVolume;

        var src = GetFromPool();
        src.transform.position = Vector3.zero;
        src.clip = clip;
        src.volume = volume;
        src.outputAudioMixerGroup = uiGroup;
        src.spatialBlend = 0f;
        src.Play();

        StartCoroutine(ReturnAfterDelay(src, clip.length + 0.1f));
    }

    /// <summary>给已有的 AudioSource 设置 SFX MixerGroup（在 Start/Awake 里调用）</summary>
    public static void RouteToSFX(AudioSource src)
    {
        if (src != null && SFXGroup != null)
            src.outputAudioMixerGroup = SFXGroup;
    }

    /// <summary>给已有的 AudioSource 设置 UI MixerGroup</summary>
    public static void RouteToUI(AudioSource src)
    {
        if (src != null && UIGroup != null)
            src.outputAudioMixerGroup = UIGroup;
    }

    // ===== BGM API =====

    /// <summary>播放 BGM，可设置淡入和是否循环</summary>
    public void PlayBGM(AudioClip clip, float fadeInDuration = 0f, bool loop = true)
    {
        Debug.Log($"[AudioManager] PlayBGM clip={clip.name}, loop={loop}, fadeIn={fadeInDuration}");
        if (clip == null || bgmSource == null) return;
        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();

        if (fadeInDuration > 0f)
        {
            bgmSource.volume = 0f;
            bgmFadeRoutine = StartCoroutine(FadeBGM(1f, fadeInDuration));
        }
        else
        {
            bgmSource.volume = defaultBGMVolume;
        }
    }

    /// <summary>停止 BGM，可设置淡出</summary>
    public void StopBGM(float fadeOutDuration = 0f)
    {
        if (bgmSource == null || !bgmSource.isPlaying) return;
        if (bgmFadeRoutine != null) StopCoroutine(bgmFadeRoutine);

        if (fadeOutDuration > 0f)
        {
            bgmFadeRoutine = StartCoroutine(FadeOutAndStop(fadeOutDuration));
        }
        else
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    private IEnumerator FadeBGM(float targetVolume, float duration)
    {
        float start = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, targetVolume, elapsed / duration);
            yield return null;
        }
        bgmSource.volume = targetVolume;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        yield return FadeBGM(0f, duration);
        bgmSource.Stop();
        bgmSource.clip = null;
    }
}
