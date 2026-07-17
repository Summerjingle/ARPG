using UnityEngine;

public class State<T> : MonoBehaviour
{   protected T owner; // 缓存拥有者，方便子类调用
    public virtual void Enter(T owner)
    {
        this.owner = owner;
    }
    public virtual void Execute() { }
    public virtual void Exit() { }
}
