using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Nơi enemy lấy player. Game chỉ có một player nên không cần threat table.
    /// Tự tìm PlayerRuntime trong scene (thử lại mỗi 1 giây nếu chưa thấy).
    /// Muốn đăng ký thủ công (ví dụ spawner tạo player) thì gọi PlayerRegistry.Register(transform).
    /// </summary>
    public static class PlayerRegistry
    {
        private static Transform _player;
        private static IStateMachine _state;
        private static float _nextSearchTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _player = null;
            _state = null;
            _nextSearchTime = 0f;
        }

        public static void Register(Transform player)
        {
            _player = player;
            _state = player != null ? player.GetComponent<IStateMachine>() : null;
        }

        /// <summary>Lấy player nếu tồn tại và chưa chết.</summary>
        public static bool TryGetAlive(out Transform player)
        {
            EnsurePlayer();
            player = _player;

            if (_player == null) return false;
            return _state == null || !_state.IsCurrentState(CharacterStateType.Dead);
        }

        private static void EnsurePlayer()
        {
            if (_player != null) return;
            if (Time.unscaledTime < _nextSearchTime) return;

            _nextSearchTime = Time.unscaledTime + 1f;

            PlayerRuntime runtime = Object.FindFirstObjectByType<PlayerRuntime>();
            if (runtime != null) Register(runtime.transform);
        }
    }
}
