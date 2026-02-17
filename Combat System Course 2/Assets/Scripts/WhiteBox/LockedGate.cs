using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedGate : MonoBehaviour, IInteractable
{
    
    private Animator GateAnim;
    public BoxCollider gateCollider;
    public AudioClip LockSound;
    public AudioClip UnlockSound;
    private bool isActivated = false; // 触发一次后失效
    public string warnningMessage;//大门被锁住时显示的警告信息

    public bool CanInteract => !isActivated;
    public virtual int Priority => 10;

    private void Start()
    {
        GateAnim = GetComponent<Animator>(); 
    }
    public void Interact() { 
    
        Debug.Log("First Gate Triggered");
        //输出大门被机关锁住了的信息
        MessageUI.Instance.Show(warnningMessage);
        //发出无法打开的声音
        if (LockSound != null)
            AudioSource.PlayClipAtPoint(LockSound, transform.position);

    }
    public void OpenGate() {
    //打开大门
    GateAnim.SetTrigger("Open");
    //发出大门打开的声音
    if (UnlockSound != null)
        AudioSource.PlayClipAtPoint(UnlockSound, transform.position);
        //禁用大门碰撞器
        isActivated = true;
        Destroy( this);
    }//供外部动画事件调用，打开大门

}
