using System;
using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// State machine của enemy (tương đương PlayerStateMachine), implement IStateMachine.
    ///
    /// Quy tắc:
    /// - Dead là trạng thái cuối: ChangeState bị từ chối. Dùng ResetForReuse() khi hồi sinh / lấy lại từ pool.
    /// - Hit và Knockback tự thoát về Locomotion sau một khoảng thời gian (dùng Time.time có scale,
    ///   nên hit-stop của player cũng làm đứng hình đồng hồ này).
    /// - Attack do AbilityRunner (bước 2) tự chuyển vào và thoát ra.
    /// - Stagger: tự vào Hit khi ICharacterRuntime.OnHit được gọi. Boss tắt canBeStaggered.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyStateMachine : MonoBehaviour, IStateMachine
    {
        [SerializeField] private CharacterStateType currentState = CharacterStateType.Locomotion;

        [Header("Stagger (state Hit)")]
        [Tooltip("Tắt cho boss. Quái thường bật để bị khựng ngắn khi trúng đòn.")]
        [SerializeField] private bool canBeStaggered = true;
        [SerializeField, Min(0.05f)] private float hitStunDuration = 0.35f;
        [Tooltip("Thời gian miễn khựng sau khi hết Hit, tránh stun-lock (DoT, đánh liên tục).")]
        [SerializeField, Min(0f)] private float staggerCooldown = 0.6f;

        [Header("Knockback")]
        [SerializeField, Min(0.05f)] private float knockbackDuration = 0.4f;

        public CharacterStateType CurrentState => currentState;
        public bool IsDead => currentState == CharacterStateType.Dead;
        public bool CanBeStaggered
        {
            get => canBeStaggered;
            set => canBeStaggered = value;
        }

        public event Action<CharacterStateType, CharacterStateType> OnStateChange;

        private ICharacterRuntime _runtime;
        private float _stateEnterTime;
        private float _nextStaggerTime;

        private void OnEnable()
        {
            _runtime = GetComponent<ICharacterRuntime>();
            if (_runtime != null) _runtime.OnHit += HandleHit;
        }

        private void OnDisable()
        {
            if (_runtime != null) _runtime.OnHit -= HandleHit;
            _runtime = null;
        }

        private void Update()
        {
            float duration = currentState switch
            {
                CharacterStateType.Hit => hitStunDuration,
                CharacterStateType.Knockback => knockbackDuration,
                _ => 0f
            };

            if (duration > 0f && Time.time >= _stateEnterTime + duration)
                ChangeState(CharacterStateType.Locomotion);
        }

        public bool ChangeState(CharacterStateType newState)
        {
            if (currentState == newState) return false;
            if (!CanTransition(currentState, newState)) return false;

            CharacterStateType oldState = currentState;
            currentState = newState;
            _stateEnterTime = Time.time;

            // Hết khựng thì tính mốc miễn khựng
            if (oldState == CharacterStateType.Hit)
                _nextStaggerTime = Time.time + staggerCooldown;

            OnStateChange?.Invoke(oldState, currentState);
            return true;
        }

        public bool IsCurrentState(CharacterStateType state) => currentState == state;

        /// <summary>Đưa về Locomotion từ Dead (hồi sinh, lấy lại từ pool).</summary>
        public void ResetForReuse()
        {
            CharacterStateType oldState = currentState;
            currentState = CharacterStateType.Locomotion;
            _stateEnterTime = Time.time;
            _nextStaggerTime = 0f;

            if (oldState != currentState)
                OnStateChange?.Invoke(oldState, currentState);
        }

        private static bool CanTransition(CharacterStateType from, CharacterStateType to)
        {
            if (from == CharacterStateType.Dead) return false;                 // Dead là cuối
            if (from == CharacterStateType.Knockback && to == CharacterStateType.Hit) return false; // Knockback mạnh hơn Hit
            return true;
        }

        private void HandleHit()
        {
            if (!canBeStaggered || IsDead) return;
            if (Time.time < _nextStaggerTime) return;

            // Hit có thể cắt ngang Attack (quái bị đánh thì hủy đòn đang windup).
            // Đòn kết liễu: OnHit chạy trước Die(), Dead sẽ ghi đè Hit ngay sau đó.
            ChangeState(CharacterStateType.Hit);
        }
    }
}
