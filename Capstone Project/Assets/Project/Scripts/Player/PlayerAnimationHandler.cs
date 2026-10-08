using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationHandler : MonoBehaviour, IAnimationHandler
{
    private Animator _animator;
    private PlayerStateMachine _stateMachine;
    private PlayerMovement _movement;

    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerRuntime runtime;
    [SerializeField] private PlayerCombat combat;

    [Header("Improve movement")]
    [SerializeField] private Transform rootTransform;

    [SerializeField] private ParticleSystem runDust;
    [SerializeField] private float squashStretchDuration = 0.15f;

    [Header("Attack Speed")]
    [SerializeField] private float baseAnimationAttackSpeed = 1f;
    [SerializeField] private float minAnimatorSpeed = 0.1f;

    private bool _requestAttacking;
    private ParticleSystem.EmissionModule _dustEmission;
    private int _currentHash;
    
    private int AttackCount
    {
        get => _animator != null ? _animator.GetInteger(_attackCountHash) : 0;
        set { if (_animator != null) _animator.SetInteger(_attackCountHash, value); }
    }

    // === ANIMATOR PARAMETER HASHES ===
    private readonly int _speedParamHash = Animator.StringToHash("Speed");
    private readonly int _attackHash = Animator.StringToHash("Attack");
    private readonly int _attackCountHash = Animator.StringToHash("AttackCount");

    // === ANIMATOR STATE HASHES (Zero GC Alloc) ===
    private readonly int _locomotionStateHash = Animator.StringToHash("Locomotion");
    private readonly int _rollHash = Animator.StringToHash("Roll");
    private readonly int _hitHash = Animator.StringToHash("Hit");
    private readonly int _deadHash = Animator.StringToHash("Dead");
    private readonly int _interactHash = Animator.StringToHash("Interact");
    
    private readonly Dictionary<string, float> _clipLengthCache = new Dictionary<string, float>();

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator != null) _animator.speed = 1f;

        if (rootTransform == null) rootTransform = transform;
        if (runDust != null) _dustEmission = runDust.emission;

        if (controller != null)
        {
            _stateMachine = controller.StateMachine;
            _movement = controller.Movement;
            if (combat == null) combat = controller.Combat;
        }
        
        if (_animator == null || _animator.runtimeAnimatorController == null) return;

        _clipLengthCache.Clear();
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && !_clipLengthCache.ContainsKey(clip.name))
            {
                _clipLengthCache.Add(clip.name, clip.length);
            }
        }
    }

    private void OnEnable()
    {
        if (_stateMachine != null) _stateMachine.OnStateChange += TriggerAnimation;

        if (_movement != null)
        {
            _movement.OnMoveStart += OnMoveStart;
            _movement.OnMoveStop += OnMoveStop;
        }
    }

    private void OnDisable()
    {
        if (_stateMachine != null) _stateMachine.OnStateChange -= TriggerAnimation;
        
        if (_movement != null)
        {
            _movement.OnMoveStart -= OnMoveStart;
            _movement.OnMoveStop -= OnMoveStop;
        }
    }

    public void TriggerAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if (_stateMachine == null || _animator == null) return;
        
        if (oldState == CharacterStateType.Attack && newState != CharacterStateType.Attack) ResetAttackState();

        switch (newState)
        {
            case CharacterStateType.Roll:
                PlayAnimation(_rollHash, 0.08f); break;
            case CharacterStateType.Hit:
                PlayAnimation(_hitHash, 0.05f); break;
            case CharacterStateType.Dead:
                PlayAnimation(_deadHash, 0.1f); break;
            case CharacterStateType.Interact:
                PlayAnimation(_interactHash, 0.15f); break;
            case CharacterStateType.Locomotion:
                PlayAnimation(_locomotionStateHash, 0.15f); break;
        }
    }

    public void UpdateAnimation()
    {
        if(_stateMachine == null) return;
        
        switch (_stateMachine.CurrentState)
        {
            case CharacterStateType.Locomotion:
                LocomotionProcess(); break;
            case CharacterStateType.Attack:
                AttackProcess(); break;
        }
    }

    private void LocomotionProcess()
    {
        if (_stateMachine.CurrentState != CharacterStateType.Locomotion) return;

        var currentSpeed = _movement.NormalizedSpeed;
        _animator.SetFloat(_speedParamHash, currentSpeed);

        if (runDust != null && runDust.isPlaying)
        {
            float targetEmission = Mathf.Lerp(0.3f, 10f, currentSpeed);
            _dustEmission.rateOverTimeMultiplier = Mathf.MoveTowards(_dustEmission.rateOverTimeMultiplier, targetEmission, Time.deltaTime * 5f);
        }
    }

    private void AttackProcess()
    {
        ApplyAttackSpeed();
        
        if (_requestAttacking)
        {
            _requestAttacking = false;
            CmdAttackTrigger(0);
        }

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsTag("Attack")) return;

        var time = state.normalizedTime;
        if(combat != null) combat.CmdActiveComboWindow(time is >= 0.7f and <= 0.95f);
        
        if (state.IsTag("Attack") && time >= 1f) EndAttackingProcess();
    }

    private void ApplyAttackSpeed()
    {
        if (_animator == null) return;
        
        if (combat == null || baseAnimationAttackSpeed <= 0f)
        {
            _animator.speed = 1f;
            return;
        }

        float currentAttackSpeed = combat != null ? combat.CurrentAttackSpeed : baseAnimationAttackSpeed;
        if (currentAttackSpeed <= 0f)
        {
            _animator.speed = 1f;
            return;
        }
        
        float multiplier = currentAttackSpeed / baseAnimationAttackSpeed;
        _animator.speed = Mathf.Max(minAnimatorSpeed, multiplier);
    }

    public float GetAnimationLength(string clipName)
    {
        return _clipLengthCache.TryGetValue(clipName, out var length) ? length : 0.4f;
    }
    
    private void ResetAttackState()
    {
        _requestAttacking = false;
        if (_animator != null)
        {
            _animator.ResetTrigger(_attackHash);
            _animator.speed = 1f;
        }
    }
    
    public void CmdAttackTrigger(int attackCount)
    {
        if(_animator == null) return;
        
        _currentHash = _attackHash;
        _animator.SetTrigger(_attackHash);
        AttackCount = attackCount;
    }

    public void CmdRequestAttacking() => _requestAttacking = true;


    #region Animation Event Function

    public void DealDamage() => combat.CmdDealDamage();
    public void SpawnProjectile() => combat.CmdSpawnProjectile();
    public void EndAttackingProcess() => combat.CmdEndAttackingProcess();
    
    #endregion

    private void PlayAnimation(int hash, float fixedTime)
    {
        if(!_animator) return;
        
        _animator.ResetTrigger(_attackHash);
        _animator.CrossFadeInFixedTime(hash, fixedTime);
        _currentHash = hash;
    }

    private void OnMoveStart(Vector3 destination) => PlayDustInFoot();
    private void OnMoveStop() => StopDustInFoot();
    
    private void PlayDustInFoot()
    {
        if(runDust == null) return;
        if (!runDust.isPlaying) runDust.Play();
    }

    private void StopDustInFoot()
    {
        if(runDust == null) return;
        runDust.Stop(true,  ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}