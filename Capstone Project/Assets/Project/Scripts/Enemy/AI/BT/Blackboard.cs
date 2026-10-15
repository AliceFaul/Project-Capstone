using UnityEngine;

namespace Mhieu.AI.BT
{
    /// <summary>
    /// Dữ liệu AI của MỘT enemy. Sensor/Brain ghi, node trong cây đọc.
    /// Dùng field cố định (không Dictionary) để không gõ sai key và không boxing.
    /// Thêm field mới ở đây khi một node cần dữ liệu mới.
    /// </summary>
    public sealed class Blackboard
    {
        public Transform Target;
        public float DistToTarget = float.PositiveInfinity;
        public bool IsAggro;
        public Vector3 HomePosition;

        /// <summary>True khi bị Hit/Knockback/Dead: Brain dừng cây và hủy hành động đang chạy.</summary>
        public bool IsDisabled;

        /// <summary>Pha của boss (1, 2, 3...). Quái thường luôn là 1.</summary>
        public int Phase = 1;

        public float LastDamagedTime = float.NegativeInfinity;

        public bool HasTarget => Target != null;

        public void ClearTarget()
        {
            Target = null;
            DistToTarget = float.PositiveInfinity;
            IsAggro = false;
        }

        public void Reset()
        {
            ClearTarget();
            IsDisabled = false;
            Phase = 1;
            LastDamagedTime = float.NegativeInfinity;
        }
    }
}
