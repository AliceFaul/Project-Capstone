using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Theo dõi % máu của boss và chuyển pha (chỉ tăng, không quay lại pha trước).
    ///
    /// Khi máu tụt qua một ngưỡng:
    ///  1. Bắt đầu chuyển pha: boss đứng yên, bất tử, phát animation (trigger "PhaseChange") và VFX.
    ///  2. Sau transitionDuration giây: ghi Phase mới vào Blackboard, đổi tốc độ chạy theo pha, hết bất tử.
    ///
    /// Cây hành vi của boss (BossBehaviorFactory) đọc Blackboard.Phase để chọn chiêu, và đọc IsTransitioning
    /// để đứng yên trong lúc chuyển pha.
    ///
    /// Trên prefab: cần BossHealth, EnemyBrain, EnemyMover (nếu có) cùng object.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossPhaseController : MonoBehaviour
    {
        private static readonly int PhaseChangeHash = Animator.StringToHash("PhaseChange");

        [Header("Phases")]
        [Tooltip("Tỉ lệ máu để vào pha 2, pha 3... theo thứ tự GIẢM dần. 0.66 và 0.33 = 3 pha.")]
        [SerializeField] private float[] phaseThresholds = { 0.66f, 0.33f };
        [Tooltip("Hệ số tốc độ chạy theo pha: phần tử 0 là pha 1, phần tử 1 là pha 2... Thiếu thì dùng phần tử cuối.")]
        [SerializeField] private float[] speedMultipliers = { 1f, 1.15f, 1.3f };

        [Header("Transition")]
        [SerializeField, Min(0.1f)] private float transitionDuration = 2f;
        [Tooltip("Hiệu ứng xuất hiện tại boss khi bắt đầu chuyển pha (tùy chọn).")]
        [SerializeField] private GameObject transitionVfx;
        [SerializeField] private Animator animator;

        private ICharacterRuntime _runtime;
        private IStateMachine _stateMachine;
        private EnemyBrain _brain;
        private IMover _mover;
        private BossHealth _health;
        private bool _hasPhaseChangeParam;

        private int _pendingPhase;
        private float _transitionEndTime;

        public int Phase { get; private set; } = 1;
        public bool IsTransitioning { get; private set; }

        /// <summary>Gọi sau khi chuyển pha xong, tham số là pha mới.</summary>
        public event System.Action<int> OnPhaseChanged;

        private void Awake()
        {
            _runtime = GetComponent<ICharacterRuntime>();
            _stateMachine = GetComponent<IStateMachine>();
            _brain = GetComponent<EnemyBrain>();
            _mover = GetComponent<IMover>();
            _health = GetComponent<BossHealth>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hasPhaseChangeParam = EnemyAnimatorUtil.HasParameter(
                animator, PhaseChangeHash, AnimatorControllerParameterType.Trigger);

            if (_health == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[BossPhaseController] Không có BossHealth trên object này nên boss sẽ KHÔNG bất tử khi chuyển pha. " +
                    "Hãy thay EnemyHealth bằng BossHealth.", this);
            }
        }

        private void OnEnable()
        {
            ResetState();
        }

        private void OnDisable()
        {
            IsTransitioning = false;
            if (_health != null) _health.IsInvulnerable = false;
        }

        private void Update()
        {
            if (_brain == null || _brain.Blackboard == null || _runtime == null) return;
            if (_stateMachine != null && _stateMachine.IsCurrentState(CharacterStateType.Dead)) return;

            if (IsTransitioning)
            {
                if (Time.time >= _transitionEndTime) FinishTransition();
                return;
            }

            int target = ComputePhase();
            if (target > Phase) BeginTransition(target);
        }

        private int ComputePhase()
        {
            if (_runtime.Health <= 0 || _runtime.TotalHealth <= 0) return Phase;

            float ratio = (float)_runtime.Health / _runtime.TotalHealth;
            int result = 1;

            for (int i = 0; i < phaseThresholds.Length; i++)
            {
                if (ratio <= phaseThresholds[i]) result = Mathf.Max(result, i + 2);
            }

            return result;
        }

        private void BeginTransition(int newPhase)
        {
            IsTransitioning = true;
            _pendingPhase = newPhase;
            _transitionEndTime = Time.time + transitionDuration;

            if (_health != null) _health.IsInvulnerable = true;
            _mover?.Stop();

            if (_hasPhaseChangeParam) animator.SetTrigger(PhaseChangeHash);
            if (transitionVfx != null)
                Instantiate(transitionVfx, transform.position + Vector3.up, Quaternion.identity);
        }

        private void FinishTransition()
        {
            IsTransitioning = false;
            Phase = _pendingPhase;
            _brain.Blackboard.Phase = Phase;

            if (_health != null) _health.IsInvulnerable = false;
            ApplySpeedMultiplier();

            OnPhaseChanged?.Invoke(Phase);
        }

        private void ResetState()
        {
            Phase = 1;
            IsTransitioning = false;
            if (_health != null) _health.IsInvulnerable = false;
            if (_brain != null && _brain.Blackboard != null) _brain.Blackboard.Phase = 1;
            ApplySpeedMultiplier();
        }

        private void ApplySpeedMultiplier()
        {
            if (_mover == null || speedMultipliers == null || speedMultipliers.Length == 0) return;

            int index = Mathf.Clamp(Phase - 1, 0, speedMultipliers.Length - 1);
            _mover.SpeedMultiplier = speedMultipliers[index];
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                IsTransitioning ? $"PHASE {Phase} -> {_pendingPhase} (bất tử)" : $"PHASE {Phase}");
#endif
        }
    }
}
