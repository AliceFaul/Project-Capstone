using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

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

    [Tooltip("Scale when start moving")] [SerializeField]
    private Vector3 startStretchScale = new Vector3(0.85f, 1.15f, 0.85f);

    [Tooltip("Scale when end moving")] [SerializeField]
    private Vector3 stopSquashScale = new Vector3(1.15f, 0.85f, 1.15f);
    
    [Header("Attack Speed")]
    [SerializeField] private float baseAnimationAttackSpeed = 1f;
    [SerializeField] private float minAnimatorSpeed = 0.1f;

    private Coroutine _scaleRoutine;
    private ParticleSystem.EmissionModule _dustEmission;
    private int _currentHash;

    // === ANIMATOR HASH ===
    // === LOCOMOTION ===
    private readonly int _locomotionHash = Animator.StringToHash("Speed");

    // === ATTACKING ===
    private readonly int _attackHash = Animator.StringToHash("Attack");
    private readonly int _attackCountHash = Animator.StringToHash("AttackCount");
    private bool _requestAttacking;

    private readonly int _rollHash = Animator.StringToHash("Roll");
    private readonly int _hitHash = Animator.StringToHash("Hit");
    private readonly int _deadHash = Animator.StringToHash("Dead");
    private readonly int _interactHash = Animator.StringToHash("Interact");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.speed = 1f;

        if (rootTransform == null)
        {
            rootTransform = transform;
        }

        if (runDust != null)
        {
            _dustEmission = runDust.emission;
        }

        if (controller != null)
        {
            _stateMachine = controller.StateMachine;
            _movement = controller.Movement;
        }

        if (_stateMachine != null)
        {
            _stateMachine.OnStateChange += TriggerAnimation;
        }

        if (_movement != null)
        {
            _movement.OnMoveStart += OnMoveStart;
            _movement.OnMoveStop += OnMoveStop;
        }
    }

    private void OnDestroy()
    {
        if (_stateMachine != null)
        {
            _stateMachine.OnStateChange -= TriggerAnimation;
        }

        if (_movement != null)
        {
            _movement.OnMoveStart -= OnMoveStart;
            _movement.OnMoveStop -= OnMoveStop;
        }
    }

    private void OnMoveStart(Vector3 destination)
    {
        PlayDustInFoot();
        // RestartScaleRoutine(startStretchScale);
    }

    private void OnMoveStop()
    {
        StopDustInFoot();
        // RestartScaleRoutine(stopSquashScale);
    }

    private void RestartScaleRoutine(Vector3 punchScale)
    {
        if(_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);

        _scaleRoutine = StartCoroutine(SquashStretchRoutine(punchScale));
    }

    public void TriggerAnimation(CharacterStateType oldState, CharacterStateType newState)
    {
        if (_stateMachine == null)
            return;
        
        if (oldState == CharacterStateType.Attack && newState != CharacterStateType.Attack)
        {
            ResetAnimatorSpeed();
        }

        switch (newState)
        {
            case CharacterStateType.Roll:
                PlayAnimation(_rollHash, 0.2f);
                break;
            case CharacterStateType.Hit:
                PlayAnimation(_hitHash, 0.2f);
                break;
            case CharacterStateType.Dead:
                PlayAnimation(_deadHash, 0.2f);
                break;
            case CharacterStateType.Interact:
                PlayAnimation(_interactHash, 0.2f);
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
        if (_stateMachine.CurrentState != CharacterStateType.Locomotion)
            return;

        /*var speed = runtime.MoveSpeed == 0
            ? 0
            : Mathf.Clamp01(agent.velocity.magnitude / runtime.MoveSpeed); */
        
        _animator.SetFloat(_locomotionHash, _movement.NormalizedSpeed);

        if (runDust != null && runDust.isPlaying)
            _dustEmission.rateOverTimeMultiplier = Mathf.Lerp(0.3f, 1f, _movement.NormalizedSpeed);
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

        if (!state.IsTag("Attack"))
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
        _animator.SetTrigger(_attackHash);
        AttackCount = attackCount;
    }

    private void ApplyAttackSpeed()
    {
        if (runtime == null || baseAnimationAttackSpeed <= 0f)
        {
            _animator.speed = 1f;
            return;
        }
        
        float multiplier = runtime.TotalAttackSpeed / baseAnimationAttackSpeed;
        _animator.speed = Mathf.Max(minAnimatorSpeed, multiplier);
    }
    
    private void ResetAnimatorSpeed()
    {
        _animator.speed = 1f;
    }

    private int AttackCount
    {
        get => _animator.GetInteger(_attackCountHash);
        set => _animator.SetInteger(_attackCountHash, value);
    }

    public void CmdRequestAttacking() => _requestAttacking = true;

    // === ATTACK ANIMATION EVENT
    public void DealDamage() => combat.CmdDealDamage();
    public void SpawnProjectile() => combat.CmdSpawnProjectile();
    public void EndAttackingProcess() => combat.CmdEndAttackingProcess();

    private void PlayAnimation(int hash, float time)
    {
        if (_currentHash == hash)
            return;

        _animator.CrossFade(hash, time);
        _currentHash = hash;
    }

    private IEnumerator SquashStretchRoutine(Vector3 punchScale)
    {
        if(rootTransform != null)
            rootTransform.localScale = punchScale;
        else
            Debug.LogError($"Model transform have missed!");

        float elapsed = 0f;
        while (elapsed < squashStretchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / squashStretchDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            rootTransform.localScale = Vector3.Lerp(punchScale, Vector3.one, eased);
            yield return null;
        }

        rootTransform.localScale = Vector3.one;
        _scaleRoutine = null;
    }

    private void PlayDustInFoot()
    {
        if(runDust == null)
            return;

        if (!runDust.isPlaying)
        {
            runDust.Play();
        }
    }

    private void StopDustInFoot()
    {
        if(runDust == null)
            return;
        
        runDust.Stop(true,  ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}