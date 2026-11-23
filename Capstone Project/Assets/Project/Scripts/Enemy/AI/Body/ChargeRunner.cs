using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Thực thi chiêu lao tới của boss: Windup (ngắm + vạch cảnh báo) -> Dash -> Recovery.
    /// Đặt cùng object với AbilityRunner (hai bộ chạy riêng nhưng không bao giờ chạy cùng lúc:
    /// cả hai đều đòi state Locomotion để bắt đầu và đổi sang Attack khi chạy).
    ///
    ///  - Windup: boss xoay về phía player, vạch cảnh báo chạy từ boss theo hướng đó.
    ///            aimLockTime giây cuối windup hướng bị khóa, vạch đổi màu -> player có thời gian né.
    ///  - Dash:   boss đi thẳng theo hướng đã khóa bằng NavMeshAgent.Move (không ra khỏi NavMesh).
    ///            Gặp mép NavMesh/vật cản (đi được quá ít so với dự kiến) thì dừng.
    ///            Trúng player (OverlapSphere) thì gây sát thương MỘT lần; stopOnHit thì dừng luôn.
    ///  - Recovery: đứng yên recoveryTime giây rồi hoàn tất.
    ///
    /// Tự ngắt khi state chuyển sang Hit/Knockback/Dead. Cooldown tính từ lúc kết thúc.
    /// Animator (tùy chọn): trigger "Attack" + int "AttackId" = def.animId, giống AbilityRunner.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChargeRunner : MonoBehaviour
    {
        private enum Stage { Idle, Windup, Dash, Recovery }

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int AttackIdHash = Animator.StringToHash("AttackId");

        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Targets")]
        [Tooltip("Layer mà cú lao có thể trúng (layer Player).")]
        [SerializeField] private LayerMask targetLayers;

        private readonly Dictionary<ChargeDefinition, float> _nextReadyTime = new Dictionary<ChargeDefinition, float>();
        private readonly Collider[] _hitBuffer = new Collider[8];
        private readonly HashSet<IAttackable> _hitSet = new HashSet<IAttackable>();

        private ICharacterRuntime _runtime;
        private IStateMachine _stateMachine;
        private IMover _mover;
        private NavMeshAgent _agent;
        private AbilityRunner _abilityRunner;
        private bool _hasAttackTrigger;
        private bool _hasAttackId;

        private Stage _stage = Stage.Idle;
        private ChargeDefinition _current;
        private Transform _target;
        private float _elapsed;
        private Vector3 _direction;
        private bool _locked;
        private float _travelled;
        private bool _hitSomeone;
        private TelegraphBar _telegraph;

        public bool IsBusy => _stage != Stage.Idle;
        public AbilityResult LastResult { get; private set; }
        public ChargeDefinition Current => _current;

        private void Reset()
        {
            targetLayers = LayerMask.GetMask("Player");
        }

        private void Awake()
        {
            _runtime = GetComponent<ICharacterRuntime>();
            _stateMachine = GetComponent<IStateMachine>();
            _mover = GetComponent<IMover>();
            _agent = GetComponent<NavMeshAgent>();
            _abilityRunner = GetComponent<AbilityRunner>();

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _hasAttackTrigger = EnemyAnimatorUtil.HasParameter(animator, AttackHash, AnimatorControllerParameterType.Trigger);
            _hasAttackId = EnemyAnimatorUtil.HasParameter(animator, AttackIdHash, AnimatorControllerParameterType.Int);
        }

        private void OnDisable()
        {
            ClearTelegraph();
            _stage = Stage.Idle;
            _current = null;
            _target = null;
        }

        // ---------------------------------------------------------------- Public API

        public bool IsReady(ChargeDefinition def)
        {
            if (def == null) return false;
            return !_nextReadyTime.TryGetValue(def, out float readyAt) || Time.time >= readyAt;
        }

        public bool InRange(ChargeDefinition def, Transform target)
        {
            if (def == null || target == null) return false;
            return def.IsInRange(FlatDistance(target.position));
        }

        public bool CanBegin(ChargeDefinition def)
        {
            if (def == null || IsBusy || !IsReady(def)) return false;
            if (_abilityRunner != null && _abilityRunner.IsBusy) return false;
            return _stateMachine == null || _stateMachine.CurrentState == CharacterStateType.Locomotion;
        }

        public bool Begin(ChargeDefinition def, Transform target)
        {
            if (target == null || !CanBegin(def)) return false;

            _current = def;
            _target = target;
            _elapsed = 0f;
            _locked = false;
            _travelled = 0f;
            _hitSomeone = false;
            _stage = Stage.Windup;
            LastResult = AbilityResult.None;

            _direction = FlatDirectionTo(target.position, transform.forward);

            _mover?.Stop();
            _stateMachine?.ChangeState(CharacterStateType.Attack);

            if (_hasAttackId) animator.SetInteger(AttackIdHash, def.animId);
            if (_hasAttackTrigger) animator.SetTrigger(AttackHash);

            SpawnTelegraph(def);
            return true;
        }

        public void Cancel()
        {
            if (IsBusy) End(AbilityResult.Interrupted);
        }

        // ---------------------------------------------------------------- Update

        private void Update()
        {
            if (_stage == Stage.Idle) return;

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

            switch (_stage)
            {
                case Stage.Windup: UpdateWindup(); break;
                case Stage.Dash: UpdateDash(); break;
                case Stage.Recovery: UpdateRecovery(); break;
            }
        }

        private void UpdateWindup()
        {
            if (_target == null)
            {
                End(AbilityResult.Interrupted);
                return;
            }

            _elapsed += Time.deltaTime;

            if (!_locked && _elapsed >= _current.windupTime - _current.aimLockTime)
            {
                _locked = true;
                if (_telegraph != null) _telegraph.SetLocked(true);
            }

            if (!_locked)
                _direction = FlatDirectionTo(_target.position, _direction);

            Quaternion look = Quaternion.LookRotation(_direction);
            transform.rotation = _locked
                ? look
                : Quaternion.RotateTowards(transform.rotation, look, _current.aimTurnSpeed * Time.deltaTime);

            UpdateTelegraph();

            if (_elapsed >= _current.windupTime)
            {
                ClearTelegraph();
                transform.rotation = look;
                _stage = Stage.Dash;
                _elapsed = 0f;
            }
        }

        private void UpdateDash()
        {
            float remaining = _current.chargeDistance - _travelled;
            float step = Mathf.Min(_current.chargeSpeed * Time.deltaTime, remaining);

            if (step > 0.0001f)
            {
                Vector3 before = transform.position;
                Vector3 delta = _direction * step;

                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                    _agent.Move(delta);
                else
                    transform.position += delta;

                Vector3 moved = transform.position - before;
                moved.y = 0f;
                float movedDistance = moved.magnitude;
                _travelled += movedDistance;

                // Đi được quá ít so với dự kiến = chạm mép NavMesh hoặc vật cản: dừng.
                bool blocked = movedDistance < step * 0.5f;

                if (TryHitTargets() && _current.stopOnHit)
                {
                    BeginRecovery();
                    return;
                }

                if (blocked)
                {
                    BeginRecovery();
                    return;
                }
            }

            if (_travelled >= _current.chargeDistance - 0.01f) BeginRecovery();
        }

        private void UpdateRecovery()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _current.recoveryTime) End(AbilityResult.Completed);
        }

        private void BeginRecovery()
        {
            _stage = Stage.Recovery;
            _elapsed = 0f;
            _mover?.Stop();
        }

        private void End(AbilityResult result)
        {
            ChargeDefinition def = _current;

            ClearTelegraph();
            _stage = Stage.Idle;
            LastResult = result;
            _current = null;
            _target = null;

            if (def != null) _nextReadyTime[def] = Time.time + def.cooldown;

            if (result == AbilityResult.Interrupted && _hasAttackTrigger)
                animator.ResetTrigger(AttackHash);

            if (_mover != null && _stage == Stage.Idle) _mover.Stop();

            if (_stateMachine != null && _stateMachine.IsCurrentState(CharacterStateType.Attack))
                _stateMachine.ChangeState(CharacterStateType.Locomotion);
        }

        // ---------------------------------------------------------------- Damage

        private bool TryHitTargets()
        {
            Vector3 center = transform.position + Vector3.up * _current.hitHeight;
            int count = Physics.OverlapSphereNonAlloc(center, _current.hitRadius, _hitBuffer, targetLayers,
                QueryTriggerInteraction.Ignore);

            float damage = _current.damageMultiplier * (_runtime != null ? _runtime.TotalDamage : 0);
            bool any = false;

            for (int i = 0; i < count; i++)
            {
                IAttackable attackable = _hitBuffer[i].GetComponentInParent<IAttackable>();
                if (attackable == null || ReferenceEquals(attackable, _runtime)) continue;

                // Mỗi mục tiêu chỉ ăn sát thương một lần trong cả cú lao.
                if (_hitSomeone && _hitSet.Contains(attackable)) continue;
                if (!_hitSomeone) _hitSet.Clear();
                _hitSet.Add(attackable);
                _hitSomeone = true;

                attackable.TakeDamage(damage, _runtime);
                any = true;
            }

            return any;
        }

        // ---------------------------------------------------------------- Telegraph

        private void SpawnTelegraph(ChargeDefinition def)
        {
            if (def.telegraphPrefab == null) return;

            _telegraph = Instantiate(def.telegraphPrefab);
            UpdateTelegraph();
        }

        private void UpdateTelegraph()
        {
            if (_telegraph == null || _current == null) return;

            Vector3 position = transform.position;
            position.y += _current.telegraphYOffset;

            _telegraph.transform.SetPositionAndRotation(position, Quaternion.LookRotation(_direction));
            _telegraph.transform.localScale = new Vector3(_current.hitRadius * 2f, 1f, _current.chargeDistance);
        }

        private void ClearTelegraph()
        {
            if (_telegraph != null) Destroy(_telegraph.gameObject);
            _telegraph = null;
        }

        // ---------------------------------------------------------------- Helpers

        private Vector3 FlatDirectionTo(Vector3 worldPosition, Vector3 fallback)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                fallback.y = 0f;
                return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
            }

            return direction.normalized;
        }

        private float FlatDistance(Vector3 worldPosition)
        {
            Vector3 delta = worldPosition - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
