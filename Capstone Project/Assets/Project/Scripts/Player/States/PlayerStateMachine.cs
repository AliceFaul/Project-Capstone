using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMachine : MonoBehaviour, IStateMachine
{
    [SerializeField] private CharacterStateType currentState = CharacterStateType.Locomotion;
    public CharacterStateType CurrentState =>  currentState;
    
    public event Action<CharacterStateType, CharacterStateType> OnStateChange;
    
    public bool ChangeState(CharacterStateType newState)
    {
        if(currentState == newState)
            return false;
        
        CharacterStateType oldState = currentState;
        currentState = newState;
        
        OnStateChange?.Invoke(oldState, currentState);
        return true;
    }

    public bool IsCurrentState(CharacterStateType state)
    {
        return CurrentState == state;
    }
}