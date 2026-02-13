using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstGate : MonoBehaviour, IInteractable
{
    
    private Animator firstGateAnim;
    public BoxCollider gateCollider;
    public AudioClip LockSound;
    public AudioClip UnlockSound;
    private bool isActivated = false; // 触发一次后失效

    public bool CanInteract => !isActivated;
    public virtual int Priority => 10;

    private void Start()
    {
        firstGateAnim = GetComponent<Animator>(); 
    }
    public void Interact() { 
    
        Debug.Log("First Gate Triggered");
        //输出大门被机关锁住了的信息
        MessageUI.Instance.Show("这扇大门被机关锁住了，无法打开。");
        //发出无法打开的声音
        if (LockSound != null)
            AudioSource.PlayClipAtPoint(LockSound, transform.position);

    }
    public void OpenGate() {
    //打开大门
    firstGateAnim.SetTrigger("Open");
    //发出大门打开的声音
    if (UnlockSound != null)
        AudioSource.PlayClipAtPoint(UnlockSound, transform.position);
        //禁用大门碰撞器
        isActivated = true;
        Destroy( this);
    }//供外部动画事件调用，打开大门

}
