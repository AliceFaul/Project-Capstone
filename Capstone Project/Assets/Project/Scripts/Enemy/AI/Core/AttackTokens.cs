using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// "Thẻ tấn công": giới hạn số enemy cận chiến được phép tấn công player CÙNG LÚC.
    /// Enemy không có thẻ thì đứng chờ ở vòng ngoài (xem HoldRingAction), không xông vào đánh.
    /// Enemy tầm xa không dùng thẻ.
    ///
    /// Là static class nên KHÔNG cần đặt object nào trong scene. Muốn đổi số thẻ (mặc định 2) thì
    /// thêm component CombatCoordinator vào scene, hoặc gán AttackTokens.MaxMeleeAttackers từ code.
    ///
    /// Thẻ tự được trả khi enemy chết, bị tắt hoặc bị hủy (Brain gọi Abort cho cây).
    /// Mỗi lần hỏi thẻ cũng tự dọn các holder đã bị hủy hoặc bị tắt, phòng khi có sự cố.
    /// </summary>
    public static class AttackTokens
    {
        public const int DefaultMax = 2;

        private static readonly HashSet<Transform> Holders = new HashSet<Transform>();
        private static int _max = DefaultMax;

        public static int MaxMeleeAttackers
        {
            get => _max;
            set => _max = Mathf.Max(1, value);
        }

        public static int HolderCount
        {
            get
            {
                Prune();
                return Holders.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Holders.Clear();
            _max = DefaultMax;
        }

        /// <summary>Xin thẻ. Trả true nếu đã giữ thẻ (xin lặp lại khi đang giữ vẫn trả true).</summary>
        public static bool TryAcquire(Transform holder)
        {
            if (holder == null) return false;

            Prune();
            if (Holders.Contains(holder)) return true;
            if (Holders.Count >= _max) return false;

            Holders.Add(holder);
            return true;
        }

        public static void Release(Transform holder)
        {
            if (holder != null) Holders.Remove(holder);
        }

        /// <summary>Còn thẻ trống hoặc mình đang giữ thẻ. Chỉ để xem, không giữ chỗ.</summary>
        public static bool IsAvailableFor(Transform holder)
        {
            Prune();
            return Holders.Contains(holder) || Holders.Count < _max;
        }

        public static void CopyHolders(List<Transform> buffer)
        {
            buffer.Clear();
            Prune();
            buffer.AddRange(Holders);
        }

        private static void Prune()
        {
            if (Holders.Count == 0) return;
            Holders.RemoveWhere(IsInvalid);
        }

        private static bool IsInvalid(Transform holder)
            => holder == null || !holder.gameObject.activeInHierarchy;
    }
}
