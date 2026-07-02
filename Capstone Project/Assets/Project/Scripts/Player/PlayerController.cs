using UnityEngine;

public class PlayerController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInvokerCommand invoker;

    [Header("Layer")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask interactLayer;

    private void Awake() {
        if(mainCamera == null) {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable() {
        inputHandler.OnLeftClick += HandleLeftClick;
        inputHandler.OnRightClick += HandleRightClick;
    }

    private void OnDisable() {
        inputHandler.OnLeftClick -= HandleLeftClick;
        inputHandler.OnRightClick -= HandleRightClick;
    }

    private void HandleLeftClick() { 
        if(inputHandler == null) {
            Debug.LogWarning("InputHandler is not assigned.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(inputHandler.MousePosition);

        if(!Physics.Raycast(ray, out RaycastHit hit)) {
            return;
        }
        int hitLayer = hit.collider.gameObject.layer;

        if(((1 << hitLayer) & groundLayer) != 0) {
            // Move to the clicked position on the ground
            ExecuteMovement(hit.point);
            return;
        }

        if(((1 << hitLayer) & enemyLayer) != 0) {
            // Attack the clicked enemy
            // ExecuteAttack();
            return;
        }

        if(((1 << hitLayer) & interactLayer) != 0) {
            // Interact with the clicked object
            // ExecuteInteraction();
            return;
        }
    }

    private void HandleRightClick() {
        // Handle right-click actions if needed
        // Ranged attack, special ability, etc.
    }

    private void ExecuteMovement(Vector3 destination) {
        ICommand moveCommand = new MoveCommand(movement, destination);
        if(invoker != null) { 
            invoker.ExecuteCommand(moveCommand);
        }
    }

    private void ExecuteAttack(Transform target) { 
        
    }

    private void ExecuteInteraction() { 
    
    }
}
