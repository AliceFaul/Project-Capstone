using System;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour, IStateMachine
{
    [SerializeField] private CharacterStateType currentState = CharacterStateType.Locomotion;
    public CharacterStateType CurrentState => currentState;

    public event Action<CharacterStateType, CharacterStateType> OnStateChange;

    public bool ChangeState(CharacterStateType newState)
    {
        if (currentState == newState) return false;
        if (currentState == CharacterStateType.Dead) return false;

        return SetState(newState);
    }

    public bool Revive(CharacterStateType newState = CharacterStateType.Locomotion)
    {
        if (currentState != CharacterStateType.Dead || newState == CharacterStateType.Dead) return false;
        return SetState(newState);
    }

    public bool IsCurrentState(CharacterStateType state)
    {
        return CurrentState == state;
    }

    private bool SetState(CharacterStateType newState)
    {
        CharacterStateType oldState = currentState;
        currentState = newState;

        OnStateChange?.Invoke(oldState, currentState);
        return true;
    }
}