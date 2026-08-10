using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    public Transform[] patrolPoints;

    [Header("Settings")]
    public float pointReachDistance = 1.2f;

    private NavMeshAgent agent;

    private int currentPoint = 0;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void StartPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.isStopped = false;

        MoveToNextPoint();
    }

    public void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= pointReachDistance)
        {
            currentPoint++;

            if (currentPoint >= patrolPoints.Length)
                currentPoint = 0;

            MoveToNextPoint();
        }
    }

    private void MoveToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        agent.SetDestination(
            patrolPoints[currentPoint].position
        );
    }
}
