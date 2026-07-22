using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour {
    private NavMeshAgent _agent;
    private PlayerRuntime _runtime;
    
    private bool _isMoving = false;
    
    public event Action OnDestinationReached;

    private void Awake() {
        _agent = GetComponent<NavMeshAgent>();
        _runtime = GetComponent<PlayerRuntime>();
    }

    private void FixedUpdate()
    {
        if(!_isMoving)
            return;
        
        if(_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
            return;
        
        _isMoving = false;
        _agent.ResetPath();
        OnDestinationReached?.Invoke();
    }

    // Moves the player to the specified destination using NavMeshAgent
    public void MoveTo(Vector3 destination) {
        _agent.speed = _runtime.MoveSpeed;
        
        _isMoving = true;
        _agent.stoppingDistance = 0f;
        _agent.SetDestination(destination);
    }
    
    // Move the player to enemy position into attack range
    public void MoveToTarget(Transform target, float stoppingDistance)
    {
        _agent.speed = _runtime.MoveSpeed;
        
        _isMoving = true;
        _agent.stoppingDistance = stoppingDistance;
        _agent.SetDestination(target.position);
    }

    // Stops the player's movement by resetting the NavMeshAgent's path
    public void Stop() { 
        _isMoving = false;
        _agent.ResetPath();
    }
}
