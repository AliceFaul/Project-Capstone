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
    
    [Header("Jump & Auto-Jump Configuration")]
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float jumpDuration = 0.45f;
    [SerializeField] private float jumpForwardSpeed = 7f;
    [SerializeField] private float autoJumpCheckDistance = 1.2f;
    [SerializeField] private float autoJumpCooldown = 0.6f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask groundLayer;
    
    private NavMeshAgent _agent;
    private PlayerRuntime _runtime;
    
    public float NormalizedSpeed => _runtime != null && _runtime.TotalSpeed > 0f ? 
            Mathf.Clamp01(_agent.velocity.magnitude / _runtime.TotalSpeed) : 
            0f;
    
    private bool _isMoving = false;
    private bool _destinationReached = false;
    
    private bool _isJumping = false;
    public bool IsJumping => _isJumping;
    
    private float _lastJumpTime;
    
    public event Action OnDestinationReached;
    public event Action<Vector3> OnMoveStart;
    public event Action OnMoveStop;
    public event Action OnJumpStart;

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
        UpdateAutoJump();
    }

    private void UpdateMovement()
    {
        if(!_isMoving || _destinationReached || _isJumping) return;
        if(!_agent.enabled || !_agent.isOnNavMesh) return;
        if(_agent.pathPending) return;
        
        if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Stop();
            return;
        }
        
        if(!(_agent.remainingDistance <= _agent.stoppingDistance)) return;
        if (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f) CompleteMovement();
    }

    private void CompleteMovement()
    {
        _isMoving = false;
        _destinationReached = false;
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;
        
        OnMoveStop?.Invoke();
        OnDestinationReached?.Invoke();
    }

    public void MoveTo(Vector3 destination) => StartMoving(destination, 0f);
    public void MoveToTarget(Transform target, float stoppingDistance) => StartMoving(target.position, stoppingDistance);

    public void Stop()
    { 
        if(!_isMoving) return;
        _isMoving = false;

        if (_agent.enabled)
        {
            _agent.isStopped = true;
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }
        
        OnMoveStop?.Invoke();
    }

    private void StartMoving(Vector3 destination, float stoppingDistance)
    {
        if(!_agent.enabled || !_agent.isOnNavMesh) return;
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

    public void CmdJump()
    {
        if (_isJumping || Time.time < _lastJumpTime + autoJumpCooldown) return;
        StartCoroutine(JumpRoutine(jumpHeight, jumpDuration));
    }

    private void UpdateAutoJump()
    {
        if(_isJumping || !_isMoving || Time.time < _lastJumpTime + autoJumpCooldown) return;
        if(_agent.velocity.sqrMagnitude < 0.5f) return;

        float dynamicCheckDistance = Mathf.Clamp(_agent.velocity.magnitude * 0.3f, 0.5f, autoJumpCheckDistance);
        
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        Vector3 forward = transform.forward;

        bool low = Physics.Raycast(origin, forward, dynamicCheckDistance, obstacleLayer);
        bool high = Physics.Raycast(origin + Vector3.up * 1.5f, forward, dynamicCheckDistance, obstacleLayer);

        if (low)
        {
            if (!high) CmdJump();
            return;
        }
        
        Vector3 gapCheck = transform.position + forward * dynamicCheckDistance + Vector3.up * 0.5f;
        bool hasGroundAhead = Physics.Raycast(gapCheck, Vector3.down, 3f, groundLayer);
        
        if(!hasGroundAhead) CmdJump();
    }

    private IEnumerator JumpRoutine(float height, float duration)
    {
        _isJumping = true;
        _lastJumpTime = Time.time;
        OnJumpStart?.Invoke();
        
        float elapsed = 0f;
        float originalOffset = _agent.baseOffset;

        Vector3 jumpDirection = transform.forward;
        jumpDirection.y = 0;
        jumpDirection.Normalize();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Parabol equation: 4 * h * t * (1 - t)
            float currentHeight = 4f * height * t * (1f - t);
            _agent.baseOffset = originalOffset + currentHeight;
            
            if(_agent.enabled) _agent.Move(jumpDirection * (jumpForwardSpeed * Time.deltaTime));
            else transform.position += jumpDirection * (jumpForwardSpeed * Time.deltaTime);
            yield return null;
        }
        
        _agent.baseOffset = originalOffset;
        _isJumping = false;
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
        if(_isJumping) return;
        
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
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        float dynamicCheckDistance = Mathf.Clamp(_agent.velocity.magnitude * 0.3f, 0.5f, autoJumpCheckDistance);
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        Vector3 forward = transform.forward;

        // Vẽ tia check bậc thấp (Xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, forward * dynamicCheckDistance);

        // Vẽ tia check bậc cao (Xanh lam)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(origin + Vector3.up * 1.5f, forward * dynamicCheckDistance);

        // Vẽ tia check vực (Đỏ)
        Vector3 gapCheck = transform.position + forward * dynamicCheckDistance + Vector3.up * 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(gapCheck, Vector3.down * 3.0f);
    }
}
