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

        /// <summary>True khi bị Hit/Knockback: Brain báo để cây tạm đứng yên.</summary>
        public bool IsDisabled;

        /// <summary>
        /// True từ lúc bỏ đuổi (quá xa) cho tới khi về tới nhà. Trong lúc này enemy phớt lờ player
        /// để không bị giật qua lại ở rìa leash.
        /// </summary>
        public bool IsReturningHome;

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
            IsReturningHome = false;
            Phase = 1;
            LastDamagedTime = float.NegativeInfinity;
        }
    }
}
