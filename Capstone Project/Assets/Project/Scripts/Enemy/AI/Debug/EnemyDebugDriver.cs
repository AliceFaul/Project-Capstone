using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// TẠM THỜI: điều khiển enemy bằng lệnh cứng để kiểm tra bước 2 (di chuyển + chiêu) khi chưa có Brain.
    /// Xóa script này khi xong bước 3.
    ///
    /// Cách dùng: gắn lên prefab enemy, gán Ability. Bật Auto Loop để enemy đi tới player rồi đánh liên tục,
    /// hoặc chuột phải vào tên component trong Inspector để chạy từng lệnh (Debug: ...).
    /// </summary>
    [RequireComponent(typeof(EnemyMover), typeof(AbilityRunner))]
    public class EnemyDebugDriver : MonoBehaviour
    {
        [SerializeField] private AbilityDefinition ability;
        [Tooltip("Để trống: tự tìm PlayerRuntime trong scene.")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoLoop;

        private IMover _mover;
        private AbilityRunner _runner;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _runner = GetComponent<AbilityRunner>();
        }

        private void Update()
        {
            if (!autoLoop || ability == null || _runner.IsBusy) return;

            Transform t = ResolveTarget();
            if (t == null) return;

            Vector3 delta = t.position - transform.position;
            delta.y = 0f;

            if (delta.magnitude > ability.maxRange)
            {
                _mover.MoveTo(t.position, Mathf.Max(0.1f, ability.maxRange * 0.8f));
            }
            else
            {
                _mover.Stop();
                _runner.Begin(ability, t);
            }
        }

        [ContextMenu("Debug: Move To Target")]
        private void DebugMoveToTarget()
        {
            Transform t = ResolveTarget();
            if (t != null) _mover.MoveTo(t.position, ability != null ? ability.maxRange * 0.8f : 1f);
        }

        [ContextMenu("Debug: Use Ability Now")]
        private void DebugUseAbility()
        {
            Transform t = ResolveTarget();
            if (t == null || ability == null) return;

            bool started = _runner.Begin(ability, t);
            UnityEngine.Debug.Log($"[EnemyDebugDriver] Begin '{ability.name}' -> {started}", this);
        }

        [ContextMenu("Debug: Stop")]
        private void DebugStop() => _mover.Stop();

        private Transform ResolveTarget()
        {
            if (target != null) return target;

            PlayerRuntime player = FindFirstObjectByType<PlayerRuntime>();
            if (player != null) target = player.transform;
            return target;
        }

        private void OnDrawGizmosSelected()
        {
            if (ability == null) return;

            Vector3 p = transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p, ability.maxRange);
            if (ability.minRange > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(p, ability.minRange);
            }

            if (ability.kind == AbilityKind.Melee)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(
                    p + transform.forward * ability.hitForwardOffset + Vector3.up * ability.hitHeight,
                    ability.hitRadius);
            }
            else if (ability.kind == AbilityKind.GroundAoE)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(p, ability.aoeRadius);
            }
        }
    }
}
