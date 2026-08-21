using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour
{ 
    [Header("Game Feel")]
    [Tooltip("Toc do quay mat theo huong di chuyen")]
    [SerializeField] private float rotationSpeed = 900f;
    [Tooltip("Gia toc khi bat dau di chuyen - cao = but toc ngay lap tuc")]
    [SerializeField] private float acceleration = 60f;
    [Tooltip("Gia toc khi phanh/dung lai - cao")]
    [SerializeField] private float angularAcceleration = 1080f;
    [SerializeField] private float stopSpeedThreshold = 0.05f;
    
    private NavMeshAgent _agent;
    private PlayerRuntime _runtime;
    
    public float NormalizedSpeed => _runtime != null && _runtime.MoveSpeed > 0f ? 
            Mathf.Clamp01(_agent.velocity.magnitude / _runtime.MoveSpeed) : 
            0f;
    
    private bool _isMoving = false;
    private bool _destinationReached = false;
    private bool _hasStartedMoveThisRun = false;
    
    public event Action OnDestinationReached;
    public event Action<Vector3> OnMoveStart;
    public event Action OnMoveStop;

    private void Awake() {
        _agent = GetComponent<NavMeshAgent>();
        _runtime = GetComponent<PlayerRuntime>();

        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.acceleration = acceleration;
            _agent.angularSpeed = angularAcceleration;
            _agent.autoBraking = true;
        }
    }

    private void Update()
    {
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if(!_isMoving)
            return;
        
        if(_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            return;
        
        if(_destinationReached)
            return;
        
        _isMoving = false;
        _agent.ResetPath();
        _destinationReached = true;
        OnMoveStop?.Invoke();
        OnDestinationReached?.Invoke();
    }

    // Moves the player to the specified destination using NavMeshAgent
    public void MoveTo(Vector3 destination)
        => StartMoving(destination, 0f);
    
    // Move the player to enemy position into attack range
    public void MoveToTarget(Transform target, float stoppingDistance)
       => StartMoving(target.position, stoppingDistance);

    // Stops the player's movement by resetting the NavMeshAgent's path
    public void Stop() { 
        bool wasMoving = _isMoving;
        _isMoving = false;
        _agent.ResetPath();

        if (wasMoving)
        {
            OnMoveStop?.Invoke();
        }
    }

    private void StartMoving(Vector3 destination, float stoppingDistance)
    {
        bool wasIdle = !_isMoving;
        
        _agent.speed = _runtime.MoveSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _destinationReached = false;
        _isMoving = true;
        _agent.SetDestination(destination);
        
        // Use for VFX/SFX
        if (wasIdle)
        {
            OnMoveStart?.Invoke(destination);
        }
    }
    
    // Rotate the character at high speed in the desired direction.
    private void UpdateRotation()
    {
        Vector3 desiredDirection = _agent.desiredVelocity;
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void SnapFaceTowards(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;
        
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
