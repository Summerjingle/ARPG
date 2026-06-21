using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [Header("Platform")]
    [SerializeField] private Transform platform;
    [SerializeField] private float topYOffset = 17.5f;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 10f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Trigger Button")]
    [SerializeField] private Animator triggerAnimator;

    private enum ElevatorState { Idle, Pressed, Moving, Arrived, Releasable }
    private ElevatorState state = ElevatorState.Idle;

    private int playerInsideCount = 0;
    private bool playerInside => playerInsideCount > 0;

    private Rigidbody rb;
    private Vector3 bottomPos;
    private Vector3 topPos;
    private bool isAtBottom = true;

    // FixedUpdate 驱动
    private bool isMoving = false;
    private float moveElapsed;
    private Vector3 moveStartPos;
    private Vector3 moveTargetPos;

    private void Awake()
    {
        if (platform == null)
            platform = transform.parent;

        rb = platform.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = platform.gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;

        bottomPos = platform.position;
        topPos = bottomPos + Vector3.up * topYOffset;
    }

    private void FixedUpdate()
    {
        if (!isMoving) return;

        moveElapsed += Time.fixedDeltaTime;

        if (moveElapsed >= moveDuration)
        {
            rb.position = moveTargetPos;
            isMoving = false;
            isAtBottom = !isAtBottom;
            ElevatorFinished();
            return;
        }

        float t = moveCurve.Evaluate(moveElapsed / moveDuration);
        Vector3 newPos = Vector3.Lerp(moveStartPos, moveTargetPos, t);
        rb.position = newPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInsideCount++;

        if (state == ElevatorState.Idle && playerInsideCount == 1)
        {
            RequestOperate();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInsideCount--;
        if (playerInsideCount < 0) playerInsideCount = 0;

        if (!playerInside)
        {
            if (state == ElevatorState.Arrived || state == ElevatorState.Releasable)
            {
                state = ElevatorState.Releasable;
                triggerAnimator.SetTrigger("Release");
                state = ElevatorState.Idle;
            }
        }
    }

    public void RequestOperate()
    {
        if (state == ElevatorState.Idle)
        {
            state = ElevatorState.Pressed;
            triggerAnimator.SetTrigger("Press");
        }
    }

    // 由 Press 动画事件调用
    public void ActivateElevator()
    {
        if (state != ElevatorState.Pressed) return;

        state = ElevatorState.Moving;
        moveStartPos = rb.position;
        moveTargetPos = isAtBottom ? topPos : bottomPos;
        moveElapsed = 0f;
        isMoving = true;
    }

    public void ElevatorFinished()
    {
        if (state == ElevatorState.Moving)
        {
            state = playerInside ? ElevatorState.Arrived : ElevatorState.Releasable;

            if (state == ElevatorState.Releasable)
            {
                triggerAnimator.SetTrigger("Release");
                state = ElevatorState.Idle;
            }
        }
    }
}
