using System;

public enum CharacterStateType
{
    None,
    Idle,
    Walk,
    Running,
    Attack,
    Roll,
    Hit,
    Knockback,
    Dead,
    Interact,
}

public interface IStateMachine
{
    CharacterStateType CurrentState { get; }
    bool ChangeState(CharacterStateType newState);
    bool IsCurrentState(CharacterStateType state);
    CharacterStateType GetCurrentState() => CurrentState;
    event Action<CharacterStateType, CharacterStateType> OnStateChange;
}