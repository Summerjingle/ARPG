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
		CurrentState?.Exit();
		CurrentState = newState;
		CurrentState.Enter(_owner);
	}

	public void Execute()
	{
		CurrentState?.Execute();
	}
}
