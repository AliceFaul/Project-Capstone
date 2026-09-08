using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Player")]
    public Transform player;

    [Header("Detection")]
    public float detectionRange = 12f;

    public float losePlayerRange = 18f;

    [Header("Attack")]
    public float attackRange = 2f;

    [Header("Movement")]
    public float patrolSpeed = 2f;

    public float chaseSpeed = 4f;

    public float retreatSpeed = 4.5f;

    [Header("Retreat")]
    public float retreatDistance = 8f;

    [Header("Hit")]
    public float hitDuration = 0.4f;

    [Header("References")]
    public NavMeshAgent agent;

    public Animator animator;

    private EnemyPatrol patrol;

    private EnemyAttack attack;

    private float hitTimer;

    private bool isDead = false;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        patrol = GetComponent<EnemyPatrol>();

        attack = GetComponent<EnemyAttack>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (isDead)
            return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolState();
                break;

            case EnemyState.Chase:
                ChaseState();
                break;

            case EnemyState.Attack:
                AttackState();
                break;

            case EnemyState.Retreat:
                RetreatState();
                break;

            case EnemyState.Hit:
                HitState();
                break;

            case EnemyState.Dead:
                DeadState();
                break;
        }
    }

    // =========================================
    // CHANGE STATE
    // =========================================

    public void ChangeState(EnemyState newState)
    {
        if (isDead && newState != EnemyState.Dead)
            return;

        currentState = newState;

        Debug.Log(
            gameObject.name +
            " State → " +
            currentState
        );

        switch (newState)
        {
            case EnemyState.Patrol:
                EnterPatrol();
                break;

            case EnemyState.Chase:
                EnterChase();
                break;

            case EnemyState.Attack:
                EnterAttack();
                break;

            case EnemyState.Retreat:
                EnterRetreat();
                break;

            case EnemyState.Hit:
                EnterHit();
                break;

            case EnemyState.Dead:
                EnterDead();
                break;
        }
    }

    // =========================================
    // PATROL
    // =========================================

    private void EnterPatrol()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }

        SetAnimation("Walk");

        if (patrol != null)
            patrol.StartPatrol();
    }

    private void PatrolState()
    {
        if (player == null)
            return;

        if (patrol != null)
            patrol.Patrol();

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (distance <= detectionRange)
        {
            ChangeState(EnemyState.Chase);
        }
    }

    // =========================================
    // CHASE
    // =========================================

    private void EnterChase()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
        }

        SetAnimation("Run");
    }

    private void ChaseState()
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Player đủ gần → Attack
        if (distance <= attackRange)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Player quá xa → quay lại Patrol
        if (distance >= losePlayerRange)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    // =========================================
    // ATTACK
    // =========================================

    private void EnterAttack()
    {
        if (agent != null)
            agent.isStopped = true;

        SetAnimation("Attack");
    }

    private void AttackState()
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Player chạy xa
        if (distance > attackRange)
        {
            if (agent != null)
                agent.isStopped = false;

            ChangeState(EnemyState.Chase);
            return;
        }

        // Quay mặt về Player
        LookAtPlayer();

        if (attack != null)
        {
            attack.Attack(player.gameObject);
        }
    }

    // =========================================
    // RETREAT
    // =========================================

    private void EnterRetreat()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = retreatSpeed;
        }

        SetAnimation("Run");

        MoveAwayFromPlayer();
    }

    private void RetreatState()
    {
        if (player == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Đã chạy đủ xa
        if (distance >= retreatDistance)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        MoveAwayFromPlayer();
    }

    private void MoveAwayFromPlayer()
    {
        if (player == null || agent == null)
            return;

        Vector3 direction =
            transform.position - player.position;

        direction.y = 0;

        if (direction.sqrMagnitude < 0.01f)
            direction = -transform.forward;

        Vector3 target =
            transform.position +
            direction.normalized * 8f;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            target,
            out hit,
            8f,
            NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // =========================================
    // HIT
    // =========================================

    private void EnterHit()
    {
        hitTimer = hitDuration;

        if (agent != null)
            agent.isStopped = true;

        SetAnimation("Hit");
    }

    private void HitState()
    {
        hitTimer -= Time.deltaTime;

        if (hitTimer <= 0)
        {
            if (player == null)
            {
                ChangeState(EnemyState.Patrol);
                return;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position
                );

            if (distance <= attackRange)
                ChangeState(EnemyState.Attack);

            else if (distance <= losePlayerRange)
                ChangeState(EnemyState.Chase);

            else
                ChangeState(EnemyState.Patrol);
        }
    }

    // =========================================
    // DEAD
    // =========================================

    private void EnterDead()
    {
        isDead = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        SetAnimation("Dead");

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 5f);
    }

    private void DeadState()
    {
        // Không làm gì.
    }

    // =========================================
    // LOOK AT PLAYER
    // =========================================

    private void LookAtPlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotation,
                Time.deltaTime * 10f
            );
    }

    // =========================================
    // ANIMATION
    // =========================================

    private void SetAnimation(string animationName)
    {
        if (animator == null)
            return;

        animator.CrossFade(
            animationName,
            0.1f
        );
    }
}