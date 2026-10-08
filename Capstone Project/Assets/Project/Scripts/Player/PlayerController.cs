using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CommandType
{
    None,
    Move,
    Attack,
    Interact
}

public class PlayerController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private float clickEffectCooldown = 0.25f;
    [SerializeField] private PlayerInvokerCommand invoker;

    [Header("Layer")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask interactLayer;
    
    [SerializeField] private CommandType currentCommand;
    private ICommand<Vector3> _moveCommand;
    private ICommand<Transform> _attackCommand;

    private bool _isHoldingMove = false;
    private float _lastClickEffectTime;
    
    private PlayerMovement _movement;
    public PlayerMovement Movement
    {
        get
        {
            if (_movement == null)
            {
                _movement = GetComponent<PlayerMovement>();
            }
            return _movement;
        }
        set => _movement = value;
    }
    
    private PlayerCombat _combat;
    public PlayerCombat Combat
    {
        get
        {
            if (_combat == null)
            {
                _combat = GetComponent<PlayerCombat>();
            }
            return _combat;
        }
        set => _combat = value;
    }

    private PlayerRuntime _playerRuntime;
    public PlayerRuntime PlayerRuntime
    {
        get
        {
            if(_playerRuntime == null) _playerRuntime = GetComponent<PlayerRuntime>();
            return _playerRuntime;
        }
        set => _playerRuntime = value;
    }
    
    private PlayerStateMachine _stateMachine;
    public PlayerStateMachine StateMachine
    {
        get
        {
            if (_stateMachine == null)
            {
                _stateMachine = GetComponent<PlayerStateMachine>();
            }
            return _stateMachine;
        }
        set => _stateMachine = value;
    }
    
    private PlayerAbilityHolder _abilityHolder;
    public PlayerAbilityHolder AbilityHolder
    {
        get
        {
            if(_abilityHolder == null) _abilityHolder = GetComponent<PlayerAbilityHolder>();
            return _abilityHolder;
        }
        set => _abilityHolder = value;
    }
    
    private InputHandler _inputHandler;
    public InputHandler InputHandler
    {
        get
        {
            if (_inputHandler == null)
            {
                _inputHandler = GetComponent<InputHandler>();
            }
            return _inputHandler;
        }
        set => _inputHandler = value;
    }
    
    private PlayerModifier _playerModifier;
    public PlayerModifier PlayerModifier
    {
        get
        {
            _playerModifier ??= new PlayerModifier();
            return _playerModifier;
        }
        set => _playerModifier = value;
    }
    
    private PlayerAnimationHandler _animationHandler;
    public PlayerAnimationHandler AnimationHandler
    {
        get
        {
            if (_animationHandler == null)
            {
                _animationHandler = GetComponentInChildren<PlayerAnimationHandler>();
            }
            return _animationHandler;
        }
        set => _animationHandler = value;
    }

    private void Awake() {
        if(mainCamera == null) {
            mainCamera = Camera.main;
        }
        
        _moveCommand = new MoveCommand(this);
        _attackCommand = new AttackCommand(this);
    }

    private void Update()
    {
        AnimationHandler.UpdateAnimation();
        ContinuousMovement();
        Jump();
    }

    private void OnEnable() {
        InputHandler.OnLeftClick += HandleLeftClick;
        InputHandler.OnRightClick += HandleRightClick;
        Movement.OnDestinationReached += OnDestinationReached;
        Movement.OnMoveStart += OnMoveStart;
        Movement.OnMoveStop += OnMoveStop;
    }

    private void OnDisable() {
        InputHandler.OnLeftClick -= HandleLeftClick;
        InputHandler.OnRightClick -= HandleRightClick;
        Movement.OnDestinationReached -= OnDestinationReached;
        Movement.OnMoveStart -= OnMoveStart;
        Movement.OnMoveStop -= OnMoveStop;
    }

    private void HandleLeftClick() { 
        if(InputHandler == null) return;
        if(UIInputBlocker.IsPointerOverUI(InputHandler.MousePosition)) return;
        
        Ray ray = mainCamera.ScreenPointToRay(InputHandler.MousePosition);

        if(!Physics.Raycast(ray, out RaycastHit hit)) return;
        int hitLayer = hit.collider.gameObject.layer;

        if(((1 << hitLayer) & groundLayer) != 0) {
            if(!PlayerModifier.CanMove) return;
            
            // Move to the clicked position on the ground
            _isHoldingMove = true;
            ExecuteMovement(hit.point);
            SpawnClickEffect(hit.point);
            return;
        }

        if(((1 << hitLayer) & enemyLayer) != 0) {
            // Attack the clicked enemy
            _isHoldingMove = false;
            ExecuteAttack(hit.collider.transform);
            return;
        }

        if(((1 << hitLayer) & interactLayer) != 0) {
            // Interact with the clicked object
            _isHoldingMove = false;
            // ExecuteInteraction();
            Debug.Log(hit.collider.gameObject.name);
            return;
        }
    }

    private void HandleRightClick() {
        // Handle right-click actions if needed
        // Ranged attack, special ability, etc.
        if(UIInputBlocker.IsPointerOverUI(InputHandler.MousePosition)) return;
        
        Ray ray = mainCamera.ScreenPointToRay(InputHandler.MousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer)) return;

        _isHoldingMove = false;
        Movement.Stop();
        Combat.CmdShoot(hit.point);
    }

    private void ExecuteMovement(Vector3 destination) {
        if(!PlayerModifier.CanMove) return;
        
        if(invoker != null) 
        { 
            invoker.ExecuteCommand(_moveCommand, destination);
            currentCommand = CommandType.Move;
        }
        StateMachine.ChangeState(CharacterStateType.Locomotion);
    }

    private void ExecuteAttack(Transform target) {
        if(!PlayerModifier.CanAttack) return;
        
        Movement?.SnapFaceTowards(target.position);

        if (invoker == null) 
            return;
        
        invoker.ExecuteCommand(_attackCommand, target);
        currentCommand = CommandType.Attack;
    }

    private void ExecuteInteraction() { 
    
    }

    private void ContinuousMovement()
    {
        bool mouseHeld = Mouse.current != null ? Mouse.current.leftButton.isPressed : Input.GetMouseButton(0);
        if (!mouseHeld)
        {
            _isHoldingMove = false;
            return;
        }
        
        if(!_isHoldingMove) return;
        if(UIInputBlocker.IsPointerOverUI(InputHandler.MousePosition)) return;
        
        Ray ray = mainCamera.ScreenPointToRay(InputHandler.MousePosition);
        if(!Physics.Raycast(ray, out var hit, 100f, groundLayer)) return;
        ExecuteMovement(hit.point);
        if(Time.time >= _lastClickEffectTime + clickEffectCooldown) SpawnClickEffect(hit.point);
    }

    private void Jump()
    {
        bool pressed = Keyboard.current != null ? Keyboard.current.spaceKey.wasPressedThisFrame : Input.GetKeyDown(KeyCode.Space);
        if(pressed) Movement.CmdJump();
    }

    private void SpawnClickEffect(Vector3 point)
    {
        if(clickEffect == null) return;
        
        Instantiate(clickEffect, point, Quaternion.identity);
        _lastClickEffectTime = Time.time;
    }

    public void CmdCombatLocked(bool value)
    {
        PlayerModifier.AttackModifier(!value);
        PlayerModifier.MoveModifier(!value);
    }

    private void OnMoveStart(Vector3 destination) { }

    private void OnMoveStop() { }

    private void OnDestinationReached()
    {
        switch (currentCommand)
        {
            case CommandType.Move: 
                // Move...
                break;
            case CommandType.Attack:
                Combat.CmdAttack(); break;
            case CommandType.Interact:
                // Interact...
                break;
        }
        
        currentCommand = CommandType.None;
        Debug.Log("Destination Reached");
    }
}
