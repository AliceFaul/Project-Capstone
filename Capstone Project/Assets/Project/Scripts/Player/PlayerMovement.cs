using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerMovement : MonoBehaviour {
    private NavMeshAgent _agent;

    private void Awake() {
        _agent = GetComponent<NavMeshAgent>();
    }

    // Moves the player to the specified destination using NavMeshAgent
    public void MoveTo(Vector3 destination) {
        _agent.SetDestination(destination);
    }

    // Stops the player's movement by resetting the NavMeshAgent's path
    public void Stop() { 
        _agent.ResetPath();
    }
}
