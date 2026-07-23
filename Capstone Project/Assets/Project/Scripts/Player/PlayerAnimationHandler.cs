using UnityEngine;

public class PlayerAnimationHandler : MonoBehaviour, IAnimationHandler
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    
    private string _currentHash;

    private void Awake()
    {
        _stateMachine = GetComponent<PlayerController>().StateMachine;
        _animator = GetComponentInChildren<Animator>();

        _stateMachine.OnStateChange += UpdateAnimation;
    }

    public void UpdateAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if(_stateMachine == null)
            return;
        
        if(_stateMachine.CurrentState == newState)
            return;

        switch (newState)
        {
            case CharacterStateType.Idle:
                PlayAnimation("idleHash", 0.2f);
                break;
            case CharacterStateType.Walk:
                PlayAnimation("walkHash", 0.2f);
                break;
            case CharacterStateType.Running:
                PlayAnimation("runningHash", 0.2f);
                break;
            case CharacterStateType.Attack:
                PlayAnimation("attackHash", 0.2f);
                break;
            case CharacterStateType.Roll:
                PlayAnimation("rollHash", 0.2f);
                break;
            case CharacterStateType.Hit:
                PlayAnimation("hitHash", 0.2f);
                break;
            case CharacterStateType.Dead:
                PlayAnimation("deadHash", 0.2f);
                break;
            case CharacterStateType.Interact:
                PlayAnimation("interactHash", 0.2f);
                break;
        }
    }

    private void PlayAnimation(string hash, float time)
    {
        if(_currentHash == hash)
            return;
        
        _animator.CrossFade(hash, time);
        _currentHash = hash;
    }
}