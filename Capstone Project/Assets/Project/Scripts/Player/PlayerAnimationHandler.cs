using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimationHandler : MonoBehaviour, IAnimationHandler
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;

    [SerializeField] private PlayerController controller;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private PlayerRuntime runtime;
    [SerializeField] private PlayerCombat combat;
    
    private int _currentHash;
    
    // === ANIMATOR HASH ===
    // === LOCOMOTION ===
    private readonly int LocomotionHash = Animator.StringToHash("Speed");
    
    // === ATTACKING ===
    private readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly int AttackCountHash = Animator.StringToHash("AttackCount");
    
    private readonly int RollHash = Animator.StringToHash("Roll");
    private readonly int HitHash = Animator.StringToHash("Hit");
    private readonly int DeadHash = Animator.StringToHash("Dead");
    private readonly int InteractHash = Animator.StringToHash("Interact");

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (controller != null)
        {
            _stateMachine = controller.StateMachine;
        }

        if (_stateMachine != null)
        {
            _stateMachine.OnStateChange += UpdateAnimation;
        }
    }

    public void UpdateAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if(_stateMachine == null)
            return;
        
        switch (newState)
        {
            case CharacterStateType.Attack:
                AttackProcess();
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
        
        float speed = runtime.MoveSpeed == 0 ? 0 : Mathf.Clamp01(
            agent.velocity.magnitude / runtime.MoveSpeed);
        _animator.SetFloat(LocomotionHash, speed);
    }

    private void AttackProcess()
    {
        if(_animator == null)
            return;
        
        _animator.SetTrigger(AttackHash);
        AttackCount = 0;
    }

    private int AttackCount
    {
        get => _animator.GetInteger(AttackCountHash);
        set => _animator.SetInteger(AttackCountHash, value);
    }
    
    // === ATTACK ANIMATION EVENT
    public void DealDamage() => combat.DealDamage();
    public void EndAttackingProcess() => combat.EndAttackingProcess();

    private void PlayAnimation(int hash, float time)
    {
        if(_currentHash == hash)
            return;
        
        _animator.CrossFade(hash, time);
        _currentHash = hash;
    }
}