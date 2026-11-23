using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.Enemy
{
    public enum AbilityResult { None, Completed, Interrupted }

    /// <summary>
    /// Thực thi một chiêu theo timer: Windup -> (đòn trúng, chỉ một lần) -> Recovery -> xong.
    /// Tự chạy trong Update của nó (không phụ thuộc tốc độ tick của Brain), dùng Time.time có scale.
    ///
    /// Cách dùng từ BT (bước 3):
    ///   - Begin(def, target) khi bắt đầu, trả false nếu chưa thể dùng chiêu.
    ///   - Mỗi tick: IsBusy -> Running; hết bận thì LastResult Completed -> Success, Interrupted -> Failure.
    ///   - Cancel() trong OnAbort của node.
    ///
    /// Tự ngắt khi state chuyển sang Hit/Knockback/Dead (boss không bị Hit vì tắt Can Be Staggered).
    /// Animator chỉ để trình diễn: parameter Attack (trigger) + AttackId (int), thiếu thì bỏ qua.
    /// </summary>
    [DisallowMultipleComponent]
    public class AbilityRunner : MonoBehaviour
    {
        private enum Phase { Idle, Windup, Recovery }

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int AttackIdHash = Animator.StringToHash("AttackId");

        [Header("References")]
        [SerializeField] private Animator animator;
        [Tooltip("Điểm bắn đạn. Để trống sẽ dùng vị trí enemy + 1m lên.")]
        [SerializeField] private Transform firePoint;

        [Header("Targets")]
        [Tooltip("Layer mà đòn của enemy có thể trúng (layer Player).")]
        [SerializeField] private LayerMask targetLayers;

        private readonly Dictionary<AbilityDefinition, float> _nextReadyTime = new Dictionary<AbilityDefinition, float>();
        private readonly Collider[] _hitBuffer = new Collider[8];
        private readonly HashSet<IAttackable> _hitSet = new HashSet<IAttackable>();

        private ICharacterRuntime _runtime;
        private IStateMachine _stateMachine;
        private IMover _mover;
        private bool _hasAttackTrigger;
        private bool _hasAttackId;

        private Phase _phase = Phase.Idle;
        private AbilityDefinition _current;
        private Transform _target;
        private float _elapsed;
        private Vector3 _aoeCenter;
        private GameObject _telegraph;

        public bool IsBusy => _phase != Phase.Idle;
        public AbilityResult LastResult { get; private set; }
        public AbilityDefinition Current => _current;

        private void Reset()
        {
            targetLayers = LayerMask.GetMask("Player");
        }

        private void Awake()
        {
            _runtime = GetComponent<ICharacterRuntime>();
            _stateMachine = GetComponent<IStateMachine>();
            _mover = GetComponent<IMover>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hasAttackTrigger = EnemyAnimatorUtil.HasParameter(animator, AttackHash, AnimatorControllerParameterType.Trigger);
            _hasAttackId = EnemyAnimatorUtil.HasParameter(animator, AttackIdHash, AnimatorControllerParameterType.Int);
        }

        private void OnDisable()
        {
            // Bị tắt giữa chừng (chết, trả pool): dọn telegraph, không đụng state machine.
            ClearTelegraph();
            _phase = Phase.Idle;
            _current = null;
            _target = null;
        }

        // ---------------------------------------------------------------- Public API

        public bool IsReady(AbilityDefinition def)
        {
            if (def == null) return false;
            return !_nextReadyTime.TryGetValue(def, out float readyAt) || Time.time >= readyAt;
        }

        public bool InRange(AbilityDefinition def, Transform target)
        {
            if (def == null || target == null) return false;
            return def.IsInRange(FlatDistance(target.position));
        }

        public bool CanBegin(AbilityDefinition def)
        {
            if (def == null || IsBusy || !IsReady(def)) return false;
            return _stateMachine == null || _stateMachine.CurrentState == CharacterStateType.Locomotion;
        }

        /// <summary>Bắt đầu chiêu. Trả false nếu chưa thể (đang bận, hồi chiêu, đang Hit/Dead...).</summary>
        public bool Begin(AbilityDefinition def, Transform target)
        {
            if (target == null || !CanBegin(def)) return false;

            _current = def;
            _target = target;
            _elapsed = 0f;
            _phase = Phase.Windup;
            LastResult = AbilityResult.None;
            _aoeCenter = def.aoeCenterOnTarget ? target.position : transform.position;

            _mover?.Stop();
            _stateMachine?.ChangeState(CharacterStateType.Attack);

            if (_hasAttackId) animator.SetInteger(AttackIdHash, def.animId);
            if (_hasAttackTrigger) animator.SetTrigger(AttackHash);

            SpawnTelegraph(def);
            return true;
        }

        /// <summary>Hủy chiêu đang chạy (dùng trong OnAbort của node BT).</summary>
        public void Cancel()
        {
            if (IsBusy) End(AbilityResult.Interrupted);
        }

        // ---------------------------------------------------------------- Update

        private void Update()
        {
            if (_phase == Phase.Idle) return;

            if (_stateMachine != null)
            {
                CharacterStateType state = _stateMachine.CurrentState;
                if (state == CharacterStateType.Hit
                    || state == CharacterStateType.Knockback
                    || state == CharacterStateType.Dead)
                {
                    End(AbilityResult.Interrupted);
                    return;
                }
            }

            _elapsed += Time.deltaTime;

            if (_phase == Phase.Windup)
            {
                if (_target == null)
                {
                    End(AbilityResult.Interrupted);
                    return;
                }

                if (_current.trackTargetDuringWindup)
                    _mover?.RotateTowards(_target.position, _current.windupTurnSpeed);

                UpdateTelegraph();

                if (_elapsed >= _current.windupTime)
                {
                    Execute();
                    ClearTelegraph();
                    _phase = Phase.Recovery;
                    _elapsed = 0f;
                }
            }
            else if (_elapsed >= _current.recoveryTime)
            {
                End(AbilityResult.Completed);
            }
        }

        private void End(AbilityResult result)
        {
            AbilityDefinition def = _current;

            ClearTelegraph();
            _phase = Phase.Idle;
            LastResult = result;
            _current = null;
            _target = null;

            if (def != null) _nextReadyTime[def] = Time.time + def.cooldown;

            if (result == AbilityResult.Interrupted && _hasAttackTrigger)
                animator.ResetTrigger(AttackHash);

            // Còn đang ở Attack (xong chiêu hoặc bị BT hủy) thì trả về Locomotion.
            // Nếu bị Hit/Dead ngắt thì state đã khác, không đụng tới.
            if (_stateMachine != null && _stateMachine.IsCurrentState(CharacterStateType.Attack))
                _stateMachine.ChangeState(CharacterStateType.Locomotion);
        }

        // ---------------------------------------------------------------- Execute

        private void Execute()
        {
            float damage = _current.damageMultiplier * (_runtime != null ? _runtime.TotalDamage : 0);

            switch (_current.kind)
            {
                case AbilityKind.Melee:
                    Vector3 center = transform.position
                                     + transform.forward * _current.hitForwardOffset
                                     + Vector3.up * _current.hitHeight;
                    DealDamageInSphere(center, _current.hitRadius, damage);
                    break;

                case AbilityKind.Projectile:
                    FireProjectiles(damage);
                    break;

                case AbilityKind.GroundAoE:
                    DealDamageInSphere(_aoeCenter, _current.aoeRadius, damage);
                    break;
            }
        }

        private void DealDamageInSphere(Vector3 center, float radius, float damage)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, _hitBuffer, targetLayers,
                QueryTriggerInteraction.Ignore);

            // Một mục tiêu có thể có nhiều collider: chỉ gây sát thương một lần cho mỗi mục tiêu.
            _hitSet.Clear();
            for (int i = 0; i < count; i++)
            {
                IAttackable attackable = _hitBuffer[i].GetComponentInParent<IAttackable>();
                if (attackable == null || ReferenceEquals(attackable, _runtime)) continue;
                if (!_hitSet.Add(attackable)) continue;

                attackable.TakeDamage(damage, _runtime);
            }
        }

        private void FireProjectiles(float damage)
        {
            if (_current.projectilePrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[AbilityRunner] '{_current.name}' chưa gán projectilePrefab.", this);
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up;

            Vector3 aim = _target != null ? _target.position - origin : transform.forward;
            aim.y = 0f;
            aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : transform.forward;

            int count = Mathf.Max(1, _current.projectileCount);
            float step = count > 1 ? _current.spreadAngle / (count - 1) : 0f;
            float start = count > 1 ? -_current.spreadAngle * 0.5f : 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(start + step * i, Vector3.up) * aim;
                EnemyProjectile projectile = Instantiate(_current.projectilePrefab, origin,
                    Quaternion.LookRotation(direction));
                projectile.Initialize(direction, damage, _current.projectileSpeed, _runtime, targetLayers,
                    _current.projectileLifetime);
            }
        }

        // ---------------------------------------------------------------- Telegraph

        private void SpawnTelegraph(AbilityDefinition def)
        {
            if (def.telegraphPrefab == null || def.kind == AbilityKind.Projectile) return;

            _telegraph = Instantiate(def.telegraphPrefab);
            float radius = def.kind == AbilityKind.Melee ? def.hitRadius : def.aoeRadius;
            _telegraph.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
            UpdateTelegraph();
        }

        private void UpdateTelegraph()
        {
            if (_telegraph == null || _current == null) return;

            if (_current.kind == AbilityKind.Melee)
            {
                // Hitbox melee đi theo enemy (vì enemy còn xoay trong lúc windup).
                Vector3 position = transform.position + transform.forward * _current.hitForwardOffset;
                position.y = transform.position.y + _current.telegraphYOffset;
                _telegraph.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            }
            else
            {
                Vector3 position = _aoeCenter;
                position.y += _current.telegraphYOffset;
                _telegraph.transform.SetPositionAndRotation(position, Quaternion.identity);
            }
        }

        private void ClearTelegraph()
        {
            if (_telegraph != null) Destroy(_telegraph);
            _telegraph = null;
        }

        // ---------------------------------------------------------------- Helpers

        private float FlatDistance(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
