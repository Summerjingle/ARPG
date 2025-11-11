using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T>
{
    T _owner;
    public State<T> CurrentState {  get; private set; }

    public StateMachine(T owner)
	{
		_owner = owner;
	}
	public void ChangeState(State<T> newState)
	{
        Debug.Log($"StateMachine.ChangeState 被调用，newState: {newState?.GetType().Name ?? "NULL"}");
        if (CurrentState != null)
        {
            Debug.Log($"退出当前状态: {CurrentState.GetType().Name}");
            CurrentState.Exit();
        }
        else
        {
            Debug.Log("当前状态为null，无需退出");
        }
        CurrentState = newState;
		CurrentState.Enter(_owner);
	}

	public void Execute()
	{
		CurrentState?.Execute();
	}
}
