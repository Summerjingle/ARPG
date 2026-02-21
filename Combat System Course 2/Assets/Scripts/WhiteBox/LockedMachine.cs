using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedMachine : MonoBehaviour, IInteractable
{
    
    private Animator machineAnim;
    public BoxCollider machineCollider;
    public AudioClip LockSound;
    public AudioClip UnlockSound;
    private bool isActivated = false; // 触发一次后失效
    public string warnningMessage;//机关被锁住时显示的警告信息

    public bool CanInteract => !isActivated;
    public virtual int Priority => 10;

    private void Start()
    {
        machineAnim = GetComponent<Animator>(); 
    }
    public void Interact() { 
    
        Debug.Log("First Gate Triggered");
        //输出被机关锁住了的信息
        MessageUI.Instance.Show(warnningMessage);
        //发出无法打开的声音
        if (LockSound != null)
            AudioSource.PlayClipAtPoint(LockSound, transform.position);

    }
    public void OpenMachine() {
    //打开机关
    machineAnim.SetTrigger("Open");
        //发出机关打开的声音
        if (UnlockSound != null)
        AudioSource.PlayClipAtPoint(UnlockSound, transform.position);
        //禁用机关碰撞器
        isActivated = true;
        Destroy( this);
    }//供外部动画事件调用，打开机关

}
