using UnityEngine;

public class FountainDoor : MonoBehaviour
{
    public FountainTurner turner;
    public FountainDirection openDirection;
    private Animator anim;

    private void OnEnable()
    {
        turner.OnRotationFinished += HandleRotationFinished;
    }

    private void OnDisable()
    {
        turner.OnRotationFinished -= HandleRotationFinished;
    }

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        // 游戏开始时，如果当前方向就是 openDirection
        var turner = FindObjectOfType<FountainTurner>();
        if (turner != null && turner.currentDirection == openDirection)
        {
            // 直接让门显示抬起状态
            anim.Play("Atrium_Door_Up", 0, 1f);  // normalizedTime = 1f，最后一帧
            anim.Update(0f);           // 强制刷新
        }
    }
    private void HandleRotationFinished(FountainDirection dir)
    {
        if (dir == openDirection)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    void OpenDoor()
    {
        Debug.Log(name + " Opened");
        anim.SetBool("Opened", true);
    }

    void CloseDoor()
    {
        Debug.Log(name + " Closed");
        anim.SetBool("Opened", false);
    }
}