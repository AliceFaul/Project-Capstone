using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimationHandler : MonoBehaviour, IAnimationHandler
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    private NavMeshAgent _agent;
    private PlayerRuntime _runtime;
    
    private int _currentHash;
    
    // === ANIMATOR HASH ===
    // === LOCOMOTION ===
    private readonly int LocomotionHash = Animator.StringToHash("Speed");
    
    // === ATTACKING ===
    private readonly int Attack1Hash = Animator.StringToHash("Attack 1");
    private readonly int Attack2Hash = Animator.StringToHash("Attack 2");
    private readonly int Attack3Hash = Animator.StringToHash("Attack 3");
    
    private readonly int RollHash = Animator.StringToHash("Roll");
    private readonly int HitHash = Animator.StringToHash("Hit");
    private readonly int DeadHash = Animator.StringToHash("Dead");
    private readonly int InteractHash = Animator.StringToHash("Interact");

    private void Awake()
    {
        _stateMachine = GetComponent<PlayerController>().StateMachine;
        _runtime = GetComponent<PlayerRuntime>();
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        
        _stateMachine.OnStateChange += UpdateAnimation;
    }

    public void UpdateAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if(_stateMachine == null)
            return;
        
        switch (newState)
        {
            case CharacterStateType.Attack:
                // AttackProcess();
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
    
    // Call in PlayerController
    public void LocomotionProcess()
    {
        if (_animator == null)
        {
            Debug.LogWarning("Animator is null");
            return;
        }
        
        if(_stateMachine.CurrentState != CharacterStateType.Locomotion)
            return;
        
        float speed = _runtime.MoveSpeed == 0 ? 0 : Mathf.Clamp01(
            _agent.velocity.magnitude / _runtime.MoveSpeed);
        _animator.SetFloat(LocomotionHash, speed);
    }

    public void AttackProcess(int combo)
    {
        switch (combo)
        {
            case 1:
                _animator.SetTrigger(Attack1Hash);
                break;
            case 2:
                _animator.SetTrigger(Attack2Hash);
                break;
            case 3:
                _animator.SetTrigger(Attack3Hash);
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