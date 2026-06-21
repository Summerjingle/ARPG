using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedMachine : MonoBehaviour, IInteractable
{

    private Animator machineAnim;
    public BoxCollider machineCollider;
    public AudioClip LockSound;
    public AudioClip UnlockSound;
    private bool isActivated = false; // 激活一次后失效
    public string warnningMessage; // 机器被锁住时显示的提示信息
    public SwitchMechanism switchMechanism;  // 存档联动

    public bool CanInteract => !isActivated;
    public virtual int Priority => 10;
    public event System.Action OnInteracted;

    private void Start()
    {
        machineAnim = GetComponent<Animator>();

        // 加载存档后，如果已经打开过，直接恢复打开状态
        // Animator 由 SwitchMechanism.Awake() 直接跳到最后一帧，这里只设逻辑状态
        if (switchMechanism != null && switchMechanism.IsActivated())
        {
            isActivated = true;
            if (machineCollider != null)
                machineCollider.enabled = false;
        }
    }
    public void Interact() {

        Debug.Log("First Gate Triggered");
        OnInteracted?.Invoke();
        // 显示机器被锁住的提示信息
        MessageUI.Instance.Show(warnningMessage);
        // 播放无法打开的音效
        if (LockSound != null)
            AudioSource.PlayClipAtPoint(LockSound, transform.position);

    }
    public void OpenMachine() {
    // 打开机器
    machineAnim.SetTrigger("Open");
        // 播放机器打开的音效
        if (UnlockSound != null)
        AudioSource.PlayClipAtPoint(UnlockSound, transform.position);
        // 禁用机器碰撞体
        if (machineCollider != null)
            machineCollider.enabled = false;
        isActivated = true;
        // 持久化到存档
        if (switchMechanism != null)
            switchMechanism.Activate();
        Destroy( this);
    }// 供外部事件调用，打开机器

}