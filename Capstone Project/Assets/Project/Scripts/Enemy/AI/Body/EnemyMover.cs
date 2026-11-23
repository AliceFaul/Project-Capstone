using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Bọc NavMeshAgent cho enemy. Tốc độ lấy từ ICharacterRuntime.TotalSpeed (cùng nguồn với player).
    ///
    /// - Tự dừng khi state chuyển sang Attack/Hit/Knockback/Dead.
    /// - MoveTo không tính lại đường nếu đích gần như không đổi, nên Brain gọi mỗi tick cũng an toàn.
    /// - Avoidance priority: số NHỎ = ưu tiên cao (player nên thấp nhất, ví dụ 30).
    ///   Gợi ý: boss 40, quái thường 50-60.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    public class EnemyMover : MonoBehaviour, IMover
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 540f;
        [Tooltip("Dùng khi TotalSpeed <= 0 (chưa điền baseSpeed trong CharacterData).")]
        [SerializeField] private float fallbackSpeed = 3.5f;
        [SerializeField, Range(0, 99)] private int avoidancePriority = 50;
        [Tooltip("Chỉ tính lại đường khi đích dịch chuyển hơn khoảng này (mét).")]
        [SerializeField, Min(0.05f)] private float repathDistance = 0.25f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private NavMeshAgent _agent;
        private ICharacterRuntime _runtime;
        private IStateMachine _stateMachine;
        private bool _hasSpeedParam;

        private bool _hasGoal;
        private bool _isMoving;
        private bool _hasArrived;
        private Vector3 _destination;
        private float _stoppingDistance;

        public float SpeedMultiplier { get; set; } = 1f;
        public bool IsMoving => _isMoving;
        public bool HasArrived => _hasArrived;

        public float RemainingDistance
        {
            get
            {
                if (!_isMoving) return 0f;
                return _agent.pathPending ? float.PositiveInfinity : _agent.remainingDistance;
            }
        }

        public float NormalizedSpeed
        {
            get
            {
                float max = Mathf.Max(0.01f, BaseSpeed);
                return Mathf.Clamp01(_agent.velocity.magnitude / max);
            }
        }

        private float BaseSpeed
        {
            get
            {
                float speed = _runtime != null ? _runtime.TotalSpeed : 0f;
                return speed > 0f ? speed : fallbackSpeed;
            }
        }

        private bool CanMove
        {
            get
            {
                if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return false;
                if (_stateMachine == null) return true;

                switch (_stateMachine.CurrentState)
                {
                    case CharacterStateType.Attack:
                    case CharacterStateType.Hit:
                    case CharacterStateType.Knockback:
                    case CharacterStateType.Dead:
                        return false;
                    default:
                        return true;
                }
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _runtime = GetComponent<ICharacterRuntime>();
            _stateMachine = GetComponent<IStateMachine>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hasSpeedParam = EnemyAnimatorUtil.HasParameter(animator, SpeedHash, AnimatorControllerParameterType.Float);

            _agent.updateRotation = false;
            _agent.autoBraking = true;
            _agent.avoidancePriority = avoidancePriority;
        }

        private void OnEnable()
        {
            if (_stateMachine != null) _stateMachine.OnStateChange += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (_stateMachine != null) _stateMachine.OnStateChange -= HandleStateChanged;
        }

        private void Update()
        {
            UpdateArrival();
            UpdateRotation();
            UpdateAnimator();
        }

        public bool MoveTo(Vector3 destination, float stoppingDistance = 0f)
        {
            if (!CanMove) return false;

            _agent.speed = BaseSpeed * SpeedMultiplier;

            bool sameGoal = _hasGoal
                            && Mathf.Approximately(_stoppingDistance, stoppingDistance)
                            && (destination - _destination).sqrMagnitude < repathDistance * repathDistance;
            if (sameGoal) return true;

            _agent.isStopped = false;
            _agent.stoppingDistance = stoppingDistance;
            if (!_agent.SetDestination(destination)) return false;

            _destination = destination;
            _stoppingDistance = stoppingDistance;
            _hasGoal = true;
            _isMoving = true;
            _hasArrived = false;
            return true;
        }

        public void Stop()
        {
            _isMoving = false;
            _hasArrived = false;
            _hasGoal = false;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }

        public void FaceTowards(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        public void RotateTowards(Vector3 worldPosition, float degreesPerSecond)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction),
                degreesPerSecond * Time.deltaTime);
        }

        private void UpdateArrival()
        {
            if (!_isMoving || !_agent.enabled || !_agent.isOnNavMesh) return;
            if (_agent.pathPending) return;

            if (_agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Stop();
                return;
            }

            if (_agent.remainingDistance > _agent.stoppingDistance + 0.05f) return;

            // Tới đích: dừng hẳn nhưng giữ _hasGoal để MoveTo cùng đích không đặt lại đường.
            _isMoving = false;
            _hasArrived = true;
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }

        private void UpdateRotation()
        {
            if (!_isMoving || !_agent.enabled) return;

            Vector3 desired = _agent.desiredVelocity;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.001f) return;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(desired),
                rotationSpeed * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            if (!_hasSpeedParam) return;
            animator.SetFloat(SpeedHash, NormalizedSpeed, 0.1f, Time.deltaTime);
        }

        private void HandleStateChanged(CharacterStateType oldState, CharacterStateType newState)
        {
            switch (newState)
            {
                case CharacterStateType.Attack:
                case CharacterStateType.Hit:
                case CharacterStateType.Knockback:
                case CharacterStateType.Dead:
                    Stop();
                    break;
            }
        }
    }
}
