using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// TÙY CHỌN: đặt một object trong scene với component này để chỉnh số enemy cận chiến
    /// được đánh cùng lúc cho scene đó. Không đặt thì dùng mặc định (2).
    ///
    /// Khi đang Play, gizmo trong Scene view hiển thị "Attack tokens x/y" và một vòng đỏ trên đầu
    /// mỗi enemy đang giữ thẻ (cần bật Gizmos).
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatCoordinator : MonoBehaviour
    {
        [Tooltip("Số enemy cận chiến được tấn công player cùng một lúc.")]
        [SerializeField, Min(1)] private int maxMeleeAttackers = 2;

        private readonly List<Transform> _buffer = new List<Transform>();

        private void Awake() => Apply();

        private void OnValidate() => Apply();

        private void Apply() => AttackTokens.MaxMeleeAttackers = maxMeleeAttackers;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            AttackTokens.CopyHolders(_buffer);
            Gizmos.color = Color.red;
            for (int i = 0; i < _buffer.Count; i++)
                Gizmos.DrawWireSphere(_buffer[i].position + Vector3.up * 2.4f, 0.25f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up,
                $"Attack tokens {_buffer.Count}/{AttackTokens.MaxMeleeAttackers}");
#endif
        }
    }
}
