using UnityEngine;

public class AnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private MonoBehaviour targetScript;
    [SerializeField] private string methodName = "OnCheckpointReached";

    public void OnAnimationEvent()
    {
        if (targetScript != null)
        {
            // 只发给目标脚本
            targetScript.SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning($"目标脚本未设置！{gameObject.name}", this);
        }
    }
}