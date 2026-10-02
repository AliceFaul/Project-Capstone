using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

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
    
    public float NormalizedSpeed => _runtime != null && _runtime.TotalSpeed > 0f ? 
            Mathf.Clamp01(_agent.velocity.magnitude / _runtime.TotalSpeed) : 
            0f;
    
    private bool _isMoving = false;
    private bool _destinationReached = false;
    
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
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if(!_isMoving || _destinationReached) return;
        if(_agent.pathPending) return;
        if(!(_agent.remainingDistance <= _agent.stoppingDistance)) return;
        
        if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f) CompleteMovement();
    }

    private void CompleteMovement()
    {
        _isMoving = false;
        _destinationReached = false;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
        
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
    public void Stop() 
    { 
        if(!_isMoving) return;
        
        _isMoving = false;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
        OnMoveStop?.Invoke();
    }

    private void StartMoving(Vector3 destination, float stoppingDistance)
    {
        bool wasIdle = !_isMoving;
        
        _agent.isStopped = false;
        
        _agent.speed = _runtime != null ? _runtime.TotalSpeed : _agent.speed;
        _agent.stoppingDistance = stoppingDistance;
        _destinationReached = false;
        _isMoving = true;
        _agent.SetDestination(destination);
        
        // Use for VFX/SFX
        if (wasIdle) OnMoveStart?.Invoke(destination);
    }

    public IEnumerator PerformRoll(Vector3 direction, float speed, float duration)
    {
        var elapsed = 0f;
        var normalizedDirection = direction.normalized;
        
        // Stop current path to perform roll ability
        if(_agent.enabled) _agent.isStopped = true;

        while (elapsed < duration)
        {
            if(_agent.enabled) _agent.Move(normalizedDirection * (speed * Time.deltaTime));
            else transform.position += normalizedDirection * (speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_agent.enabled)
        {
            _agent.isStopped = false;
            Stop();
        }
    }
    
    // Rotate the character at high speed in the desired direction.
    private void UpdateRotation()
    {
        Vector3 desiredDirection = _agent.desiredVelocity;
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void SnapFaceTowards(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;
        
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
