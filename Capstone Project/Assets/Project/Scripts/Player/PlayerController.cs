using System;
using UnityEngine;

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
    [SerializeField] private PlayerInvokerCommand invoker;

    [Header("Layer")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask interactLayer;
    
    [SerializeField] private CommandType currentCommand;
    private ICommand<Vector3> _moveCommand;
    private ICommand<Transform> _attackCommand;
    
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
            if (_playerModifier == null)
            {
                _playerModifier = new PlayerModifier();
            }
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
    }

    private void OnEnable() {
        InputHandler.OnLeftClick += HandleLeftClick;
        InputHandler.OnRightClick += HandleRightClick;
        Movement.OnDestinationReached += OnDestinationReached;
    }

    private void OnDisable() {
        InputHandler.OnLeftClick -= HandleLeftClick;
        InputHandler.OnRightClick -= HandleRightClick;
        Movement.OnDestinationReached -= OnDestinationReached;
    }

    private void HandleLeftClick() { 
        if(InputHandler == null) {
            Debug.LogWarning("InputHandler is not assigned.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(InputHandler.MousePosition);

        if(!Physics.Raycast(ray, out RaycastHit hit)) {
            return;
        }
        int hitLayer = hit.collider.gameObject.layer;

        if(((1 << hitLayer) & groundLayer) != 0) {
            // Move to the clicked position on the ground
            ExecuteMovement(hit.point);
            Debug.Log(hit.collider.gameObject.name);
            return;
        }

        if(((1 << hitLayer) & enemyLayer) != 0) {
            // Attack the clicked enemy
            ExecuteAttack(hit.collider.transform);
            Debug.Log(hit.collider.gameObject.name);
            return;
        }

        if(((1 << hitLayer) & interactLayer) != 0) {
            // Interact with the clicked object
            // ExecuteInteraction();
            Debug.Log(hit.collider.gameObject.name);
            return;
        }
    }

    private void HandleRightClick() {
        // Handle right-click actions if needed
        // Ranged attack, special ability, etc.
        Ray ray = mainCamera.ScreenPointToRay(InputHandler.MousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            return;
        }
        
        Combat.CmdShoot(hit.point);
    }

    private void ExecuteMovement(Vector3 destination) {
        if(invoker != null) { 
            invoker.ExecuteCommand(_moveCommand, destination);
            currentCommand = CommandType.Move;
        }
        StateMachine.ChangeState(CharacterStateType.Locomotion);
    }

    private void ExecuteAttack(Transform target) {
        if (invoker != null)
        {
            invoker.ExecuteCommand(_attackCommand, target);
            currentCommand = CommandType.Attack;
        }
    }

    private void ExecuteInteraction() { 
    
    }

    public void CmdCombatLocked(bool value)
    {
        PlayerModifier.AttackModifier(!value);
        PlayerModifier.MoveModifier(!value);
    }

    private void OnDestinationReached()
    {
        switch (currentCommand)
        {
            case CommandType.Move: 
                // Move...
                break;
            case CommandType.Attack:
                Combat.CmdAttack();
                break;
        }
        
        currentCommand = CommandType.None;
        Debug.Log("Destination Reached");
    }
}
