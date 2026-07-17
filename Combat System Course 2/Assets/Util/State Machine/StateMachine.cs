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
        Debug.Log($"StateMachine.ChangeState �����ã�newState: {newState?.GetType().Name ?? "NULL"}");
        if (CurrentState != null)
        {
            Debug.Log($"�˳���ǰ״̬: {CurrentState.GetType().Name}");
            CurrentState.Exit();
        }
        else
        {
            Debug.Log("��ǰ״̬Ϊnull�������˳�");
        }
        CurrentState = newState;
		CurrentState.Enter(_owner);
	}

	public void Execute()
	{
		CurrentState?.Execute();
	}
}
