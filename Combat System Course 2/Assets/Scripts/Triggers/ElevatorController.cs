using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public Animator elevatorAnimator;
    public Animator triggerAnimator;

    private enum ElevatorState { Idle, Pressed, Moving, Arrived, Releasable }
    private ElevatorState state = ElevatorState.Idle;

    private int playerInsideCount = 0;
    private bool playerInside => playerInsideCount > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInsideCount++;

        Debug.Log($"Enter: count={playerInsideCount}, state={state}");

        if (state == ElevatorState.Idle && playerInsideCount == 1)
        {
            state = ElevatorState.Pressed;
            triggerAnimator.SetTrigger("Press");
            // 这里可以播放按键音效等
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInsideCount--;
        if (playerInsideCount < 0) playerInsideCount = 0;

        Debug.Log($"Exit: count={playerInsideCount}, state={state}");

        if (!playerInside)
        {
            if (state == ElevatorState.Arrived || state == ElevatorState.Releasable)
            {
                state = ElevatorState.Releasable;
                triggerAnimator.SetTrigger("Release");
                state = ElevatorState.Idle;          // 回到初始状态
            }
            // 注意：这里不再直接 TryRelease，而是等动画结束再处理
        }
    }

    // 由 Press 动画事件调用（动画最后一帧）
    public void ActivateElevator()
    {
        if (state != ElevatorState.Pressed) return;

        state = ElevatorState.Moving;
        elevatorAnimator.SetTrigger("Operate");
    }

    // 由电梯上升动画事件调用（到顶了）
    public void ElevatorFinished()
    {
        Debug.Log($"ElevatorFinished, state={state}");

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
