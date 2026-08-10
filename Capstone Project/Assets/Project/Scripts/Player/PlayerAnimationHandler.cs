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
    private bool _requestAttacking;
    
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
            _stateMachine.OnStateChange += TriggerAnimation;
        }
    }

    public void TriggerAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if(_stateMachine == null)
            return;
        
        switch (newState)
        {
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

    public void UpdateAnimation()
    {
        switch (_stateMachine.CurrentState)
        {
            case CharacterStateType.Locomotion:
                LocomotionProcess();
                break;
            case CharacterStateType.Attack:
                AttackProcess();
                break;
        }
    }

    // Call in PlayerController
    private void LocomotionProcess()
    {
        if(_stateMachine.CurrentState != CharacterStateType.Locomotion)
            return;
        
        var speed = runtime.MoveSpeed == 0 ? 0 : Mathf.Clamp01(
            agent.velocity.magnitude / runtime.MoveSpeed);
        _animator.SetFloat(LocomotionHash, speed);
    }

    private void AttackProcess()
    {
        if (_requestAttacking)
        {
            _requestAttacking = false;
           CmdAttackTrigger(0);
        }
        
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        
        if(!state.IsTag("Attack"))
            return;
        
        var time = state.normalizedTime;
        combat.CmdActiveComboWindow(time is >= 0.7f and <= 0.95f);
        if (state.IsTag("Attack") && time >= 1f)
        {
            EndAttackingProcess();
        }
    }

    public void CmdAttackTrigger(int attackCount)
    {
        _animator.SetTrigger(AttackHash);
        AttackCount = attackCount;
    }

    private int AttackCount
    {
        get => _animator.GetInteger(AttackCountHash);
        set => _animator.SetInteger(AttackCountHash, value); 
    }
    
    public void CmdRequestAttacking() => _requestAttacking = true;
    
    // === ATTACK ANIMATION EVENT
    public void DealDamage() => combat.CmdDealDamage();
    public void SpawnProjectile() => combat.CmdSpawnProjectile();
    public void EndAttackingProcess() => combat.CmdEndAttackingProcess();

    private void PlayAnimation(int hash, float time)
    {
        if(_currentHash == hash)
            return;
        
        _animator.CrossFade(hash, time);
        _currentHash = hash;
    }
}