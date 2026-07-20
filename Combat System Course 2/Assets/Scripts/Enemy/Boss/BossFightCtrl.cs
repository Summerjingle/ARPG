using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class BossFightCtrl : MonoBehaviour
{
    public BossController bossController;
    public GameObject bossEntranceLocker;
    public AudioClip bossFightStartMusic;
    public AudioClip bossFightBGM;

    private AudioSource introSource;

    void Awake()
    {
        var go = new GameObject("BossIntro");
        go.transform.SetParent(transform);
        introSource = go.AddComponent<AudioSource>();
        introSource.playOnAwake = false;
        introSource.loop = false;
        introSource.spatialBlend = 0f;
        introSource.outputAudioMixerGroup = AudioManager.BGMGroup;
    }

    void OnEnable()
    {
        bossController.OnBossFightEnter += OnBossFightBegin;
        bossController.OnBossFightExit += OnBossFightEnd;
    }

    void OnDisable()
    {
        bossController.OnBossFightEnter -= OnBossFightBegin;
        bossController.OnBossFightExit -= OnBossFightEnd;
    }

    void OnBossFightBegin()
    {
        bossEntranceLocker.SetActive(true);
        StartCoroutine(BossMusicSequence());
    }

    void OnBossFightEnd()
    {
        bossEntranceLocker.SetActive(false);
        AudioManager.Instance?.StopBGM(2f);
    }

    private IEnumerator BossMusicSequence()
    {
        Debug.Log($"[BossMusic] 协程启动, frame={Time.frameCount}");

        // 循环 BGM 立即开始
        if (bossFightBGM != null)
        {
            Debug.Log($"[BossMusic] 播BGM, clip={bossFightBGM.name}");
            AudioManager.Instance?.PlayBGM(bossFightBGM, 1f, loop: true);
        }
        else
        {
            Debug.LogWarning("[BossMusic] bossFightBGM 是 null！");
        }

        // 开场音乐同时叠加（独立 AudioSource，不抢 bgmSource）
        if (bossFightStartMusic != null)
        {
            Debug.Log($"[BossMusic] 播开场, clip={bossFightStartMusic.name}, length={bossFightStartMusic.length}");
            introSource.clip = bossFightStartMusic;
            introSource.volume = 1f;
            introSource.Play();
        }

        yield break;
    }
}
