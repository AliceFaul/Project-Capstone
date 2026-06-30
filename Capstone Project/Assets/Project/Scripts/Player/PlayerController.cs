using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : MonoBehaviour {
    private InputSystem_Actions _actions;
    private NavMeshAgent _agent;
    private Camera _mainCamera;

    private PlayerCombat _combat;
    private Transform _currentTarget; // Store the current target (enemy or interactable)

    [Header("Layer")]
    [SerializeField] private LayerMask groundLayer; // Move
    [SerializeField] private LayerMask enemyLayer; // Attack
    [SerializeField] private LayerMask interactLayer; // Interact

    private void Awake() {
        _actions = new InputSystem_Actions();
        _agent = GetComponent<NavMeshAgent>();
        _combat = GetComponent<PlayerCombat>();
        _mainCamera = Camera.main;
    }

    private void OnEnable() {
        _actions.Enable();
        _agent.updateUpAxis = false; // Disable NavMeshAgent's automatic up axis update
        _agent.updateRotation = false; // Disable NavMeshAgent's automatic rotation update
        _actions.Player.LeftClick.performed += ctx => HandleLeftClick();
    }

    private void OnDisable() {
        _actions.Disable();
    }

    private void Update() {
        if(_currentTarget != null) { 
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            if(distanceToTarget <= _combat.attackRange) { 
                // Stop moving when close enough to the target
                _agent.ResetPath();
                _combat.Attack();
            }
        }
        ApplyGravity();
    }

    private void HandleLeftClick() { 
        Ray ray = _mainCamera.ScreenPointToRay(_actions.Player.MousePosition.ReadValue<Vector2>());

        if(Physics.Raycast(ray, out RaycastHit hit)) { 
            int layer = hit.collider.gameObject.layer; // Get the layer of the object that was hit by the raycast

            if(((1 << layer) & enemyLayer) != 0) { 
                MoveToEnemy(hit.collider.transform);
            } 
            else if(((1 << layer) & interactLayer) != 0) { 
                MoveToInteractable(hit.collider.transform);
            } 
            else if(((1 << layer) & groundLayer) != 0) { 
                MoveToPoint(hit.point);
            }
        }

        Debug.Log($"Clicked on: {hit.collider.gameObject.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
    }

    private void MoveToPoint(Vector3 position) { 
        _agent.SetDestination(position);
        // rotate player to face the direction of movement
        Vector3 direction = (position - transform.position).normalized;
        Rotate(direction);
    }

    private void MoveToEnemy(Transform enemy) { 
        _currentTarget = enemy; // Store the current target
        _agent.SetDestination(enemy.position);
        // rotate player to face the direction of movement
        Vector3 direction = (enemy.position - transform.position).normalized;
        Rotate(direction);
    }

    private void MoveToInteractable(Transform target) { 
        _currentTarget = target; // Store the current target
        _agent.SetDestination(target.position);
        // rotate player to face the direction of movement
        Vector3 direction = (target.position - transform.position).normalized;
        Rotate(direction);
    }

    private void Rotate(Vector3 direction) { 
        if(direction != Vector3.zero) { 
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void ApplyGravity() { 
        if(!_agent.isOnNavMesh) { 
            _agent.enabled = false;
            transform.position += Physics.gravity * Time.deltaTime;
            _agent.enabled = true;
        }
    }
}
