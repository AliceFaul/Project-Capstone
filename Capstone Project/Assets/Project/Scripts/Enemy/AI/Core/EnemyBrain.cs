using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Bộ não enemy: giữ Blackboard + cây hành vi, tick theo chu kỳ cố định (mặc định 10 lần/giây).
    ///
    /// Trên prefab cần: EnemyBrain, EnemyMover, AbilityRunner, EnemyStateMachine (+ EnemyHealth có sẵn).
    /// NHỚ gỡ (hoặc tắt Auto Loop của) EnemyDebugDriver, vì nó cũng điều khiển mover.
    ///
    /// Aggro khi bị đánh: nghe ICharacterRuntime.OnHit. Vì game chỉ có một player nên coi người đánh
    /// luôn là player (kể cả sát thương DoT không có attacker). Enemy khác trong alertRadius cũng
    /// vào trạng thái chiến đấu (báo động nhóm).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyBrain : MonoBehaviour
    {
        private static readonly List<EnemyBrain> Active = new List<EnemyBrain>();

        [SerializeField] private EnemyAIConfig config;
        [SerializeField] private EnemyBehaviorFactory behavior;

        private Blackboard _bb;
        private BehaviorTree _tree;
        private TargetSensor _targetSensor;
        private ICharacterRuntime _runtime;
        private IStateMachine _stateMachine;

        private float _thinkTimer;
        private float _lastThinkTime;

        public Blackboard Blackboard => _bb;

        private void Awake()
        {
            if (config == null || behavior == null || config.primaryAbility == null)
            {
                UnityEngine.Debug.LogError(
                    "[EnemyBrain] Thiếu Config / Behavior / Primary Ability. Brain bị tắt.", this);
                enabled = false;
                return;
            }

            _runtime = GetComponent<ICharacterRuntime>();
            _stateMachine = GetComponent<IStateMachine>();

            _bb = new Blackboard();
            var context = new BTContext(gameObject, _bb);
            context.Register<IMover>(GetComponent<IMover>());
            context.Register<AbilityRunner>(GetComponent<AbilityRunner>());
            context.Register<EnemyAIConfig>(config);

            _tree = new BehaviorTree(behavior.Build(config), context);
            _targetSensor = new TargetSensor(transform, config);
        }

        private void OnEnable()
        {
            if (_tree == null) return;

            Active.Add(this);
            ResetBrain();

            if (_runtime != null) _runtime.OnHit += HandleHit;
            if (_stateMachine != null) _stateMachine.OnStateChange += HandleStateChange;

            // Lệch pha giữa các enemy để không tick cùng một frame.
            _thinkTimer = Random.value * config.thinkInterval;
            _lastThinkTime = Time.time;
        }

        private void Start()
        {
            // Spawner có thể đặt lại vị trí ngay sau Instantiate, nên chốt lại "nhà" ở frame đầu.
            if (_tree != null) _bb.HomePosition = transform.position;
        }

        private void OnDisable()
        {
            if (_tree == null) return;

            Active.Remove(this);

            if (_runtime != null) _runtime.OnHit -= HandleHit;
            if (_stateMachine != null) _stateMachine.OnStateChange -= HandleStateChange;

            _tree.Abort();
        }

        private void Update()
        {
            _thinkTimer -= Time.deltaTime;
            if (_thinkTimer > 0f) return;

            _thinkTimer = config.thinkInterval;
            float deltaTime = Time.time - _lastThinkTime;
            _lastThinkTime = Time.time;

            Think(deltaTime);
        }

        /// <summary>Đặt lại vị trí "nhà" (ví dụ spawner đặt enemy sau khi tạo).</summary>
        public void SetHome(Vector3 position)
        {
            if (_bb != null) _bb.HomePosition = position;
        }

        private void Think(float deltaTime)
        {
            CharacterStateType state = _stateMachine != null
                ? _stateMachine.CurrentState
                : CharacterStateType.Locomotion;

            if (state == CharacterStateType.Dead) return;

            _bb.IsDisabled = state == CharacterStateType.Hit || state == CharacterStateType.Knockback;

            _targetSensor.Sense(_bb);
            _tree.Tick(Time.time, deltaTime);
        }

        private void ResetBrain()
        {
            _bb.Reset();
            _bb.HomePosition = transform.position;
        }

        private void HandleStateChange(CharacterStateType oldState, CharacterStateType newState)
        {
            if (newState == CharacterStateType.Dead)
            {
                _tree.Abort();
                _bb.ClearTarget();
            }
            else if (oldState == CharacterStateType.Dead)
            {
                // Hồi sinh / lấy lại từ pool.
                ResetBrain();
            }
        }

        // ---------------------------------------------------------------- Aggro khi bị đánh

        private void HandleHit()
        {
            _bb.LastDamagedTime = Time.time;
            if (_bb.IsReturningHome) return;
            if (!PlayerRegistry.TryGetAlive(out Transform player)) return;

            Acquire(player);

            float radius = config.alertRadius;
            if (radius <= 0f) return;

            for (int i = 0; i < Active.Count; i++)
            {
                EnemyBrain other = Active[i];
                if (other == this) continue;

                if (AIMath.FlatDistance(other.transform.position, transform.position) <= radius)
                    other.AlertTo(player);
            }
        }

        private void AlertTo(Transform player)
        {
            if (_bb.IsAggro || _bb.IsReturningHome) return;
            if (_stateMachine != null && _stateMachine.CurrentState == CharacterStateType.Dead) return;

            Acquire(player);
        }

        private void Acquire(Transform player)
        {
            _bb.Target = player;
            _bb.IsAggro = true;
            _bb.DistToTarget = AIMath.FlatDistance(transform.position, player.position);
        }

        // ---------------------------------------------------------------- Debug

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;

            bool playing = Application.isPlaying && _bb != null;
            Vector3 home = playing ? _bb.HomePosition : transform.position;

            Gizmos.color = playing && _bb.IsAggro ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, config.aggroRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, config.loseRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(home, config.leashDistance);
            Gizmos.DrawSphere(home, 0.15f);

#if UNITY_EDITOR
            if (playing)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2.2f,
                    $"aggro={_bb.IsAggro}  returning={_bb.IsReturningHome}  disabled={_bb.IsDisabled}");
            }
#endif
        }
    }
}
