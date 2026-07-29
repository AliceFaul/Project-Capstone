using System;
using UnityEngine;

public class PlayerController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInvokerCommand invoker;

    [Header("Layer")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask interactLayer;
    
    private ICommand _currentCommand;
    
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
                _animationHandler = GetComponent<PlayerAnimationHandler>();
            }
            return _animationHandler;
        }
        set => _animationHandler = value;
    }

    private void Awake() {
        if(mainCamera == null) {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        AnimationHandler.LocomotionProcess();
    }

    private void OnEnable() {
        InputHandler.OnLeftClick += HandleLeftClick;
        InputHandler.OnRightClick += HandleRightClick;
        movement.OnDestinationReached += OnDestinationReached;
    }

    private void OnDisable() {
        InputHandler.OnLeftClick -= HandleLeftClick;
        InputHandler.OnRightClick -= HandleRightClick;
        movement.OnDestinationReached -= OnDestinationReached;
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
        
        combat.Shoot(hit.point);
    }

    private void ExecuteMovement(Vector3 destination) {
        if(!PlayerModifier.CanMove)
            return;
        
        ICommand moveCommand = new MoveCommand(movement, destination);
        if(invoker != null) { 
            invoker.ExecuteCommand(moveCommand);
            _currentCommand = moveCommand;
        }
        StateMachine.ChangeState(CharacterStateType.Locomotion);
    }

    private void ExecuteAttack(Transform target) {
        if(!PlayerModifier.CanAttack)
            return;
        
        ICommand attackCommand = new AttackCommand(combat, movement, target);
        if (invoker != null)
        {
            invoker.ExecuteCommand(attackCommand);
            _currentCommand = attackCommand;
        }
        StateMachine.ChangeState(CharacterStateType.Locomotion);
    }

    private void ExecuteInteraction() { 
    
    }

    private void OnDestinationReached()
    {
        switch (_currentCommand)
        {
            case MoveCommand: 
                // Move...
                break;
            case AttackCommand:
                StateMachine.ChangeState(CharacterStateType.Attack);
                combat.Attack();
                break;
        }
        
        Debug.Log("Destination Reached");
    }
}
