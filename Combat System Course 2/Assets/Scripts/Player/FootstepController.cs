using UnityEngine;

public class FootstepController : MonoBehaviour
{
    [Header("音频源")]
    public AudioSource footstepSource;

    [Header("音频剪辑")]
    public AudioClip walkClip;
    public AudioClip runClip;
    public AudioClip combatMoveClip; // 战斗移动统一使用一个音频

    [Header("音量设置")]
    [Range(0f, 1f)] public float walkVolume = 0.5f;
    [Range(0f, 1f)] public float runVolume = 0.7f;
    [Range(0f, 1f)] public float combatVolume = 0.6f;

    [Header("防重叠设置")]
    public float footstepCooldown = 0.4f;
    private float lastFootstepTime;

    private PlayerController playerController;
    private CombatController combatController;
    private Animator animator;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        combatController = GetComponent<CombatController>();
        animator = GetComponent<Animator>();
    }

    // 统一脚步声播放方法
    public void PlayFootstep()
    {
        if (!CanPlayFootstep()) return;

        // 根据战斗模式选择音频
        AudioClip clip;
        float volume;

        if (combatController.CombatMode)
        {
            clip = combatMoveClip;
            volume = combatVolume;
        }
        else
        {
            bool isRunning = IsRunning();
            clip = isRunning ? runClip : walkClip;
            volume = isRunning ? runVolume : walkVolume;
        }

        PlaySingleFootstep(clip, volume);
    }

    private void PlaySingleFootstep(AudioClip clip, float volume)
    {
        // 防重叠检查
        if (Time.time - lastFootstepTime < footstepCooldown)
            return;

        if (clip == null)
        {
            Debug.LogWarning("脚步声音频剪辑未设置!");
            return;
        }

        footstepSource.clip = clip;
        footstepSource.volume = volume;
        footstepSource.Play();

        lastFootstepTime = Time.time;

        Debug.Log($"播放脚步声: {clip.name}, 音量: {volume}, 战斗模式: {combatController.CombatMode}");
    }

    private bool CanPlayFootstep()
    {
        if (!playerController.isGrounded)
            return false;

        // 检查是否有移动输入
        float moveAmount = Mathf.Abs(animator.GetFloat("forwardSpeed")) + Mathf.Abs(animator.GetFloat("strafeSpeed"));
        return moveAmount > 0.1f;
    }

    private bool IsRunning()
    {
        // 战斗模式下不算跑步
        if (combatController != null && combatController.CombatMode)
            return false;

        float forwardSpeed = animator.GetFloat("forwardSpeed");
        return forwardSpeed > 0.7f;
    }
}