using UnityEngine;

public class PlayerAnimationHandler : MonoBehaviour, IAnimationHandler
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    
    private int _currentHash;
    
    // === ANIMATOR HASH ===
    private readonly int IdleHash = Animator.StringToHash("Idle");
    private readonly int WalkHash = Animator.StringToHash("Walk");
    private readonly int RunningHash = Animator.StringToHash("Running");
    private readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly int RollHash = Animator.StringToHash("Roll");
    private readonly int HitHash = Animator.StringToHash("Hit");
    private readonly int DeadHash = Animator.StringToHash("Dead");
    private readonly int InteractHash = Animator.StringToHash("Interact");

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
        
        switch (newState)
        {
            case CharacterStateType.Idle:
                PlayAnimation(IdleHash, 0.2f);
                break;
            case CharacterStateType.Walk:
                PlayAnimation(WalkHash, 0.2f);
                break;
            case CharacterStateType.Running:
                PlayAnimation(RunningHash, 0.2f);
                break;
            case CharacterStateType.Attack:
                PlayAnimation(AttackHash, 0.2f);
                break;
            case CharacterStateType.Roll:
                PlayAnimation(RollHash, 0.2f);
                break;
            case CharacterStateType.Hit:
                PlayAnimation(HitHash, 0.2f);
                break;
            case CharacterStateType.Dead:
                PlayAnimation(DeadHash, 0.2f);
                break;
            case CharacterStateType.Interact:
                PlayAnimation(InteractHash, 0.2f);
                break;
        }
    }

    private void PlayAnimation(int hash, float time)
    {
        if(_currentHash == hash)
            return;
        
        _animator.CrossFade(hash, time);
        _currentHash = hash;
    }
}