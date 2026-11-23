using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Giao diện di chuyển mà AI dùng. AI chỉ ra lệnh, không biết bên dưới là NavMeshAgent hay thứ khác.
    /// </summary>
    public interface IMover
    {
        /// <summary>Đang đi tới một đích.</summary>
        bool IsMoving { get; }

        /// <summary>Đã tới đích của lệnh MoveTo gần nhất (giữ true cho tới khi có lệnh mới hoặc Stop).</summary>
        bool HasArrived { get; }

        float RemainingDistance { get; }

        /// <summary>Vận tốc hiện tại / tốc độ tối đa, trong 0..1.</summary>
        float NormalizedSpeed { get; }

        /// <summary>Hệ số tốc độ (1 = bình thường). Dành cho slow/haste sau này.</summary>
        float SpeedMultiplier { get; set; }

        /// <summary>
        /// Đi tới vị trí. Trả về false nếu hiện không thể di chuyển (đang Attack/Hit/Knockback/Dead
        /// hoặc agent không nằm trên NavMesh). Gọi lặp lại với cùng đích là rẻ (không tính lại đường).
        /// </summary>
        bool MoveTo(Vector3 destination, float stoppingDistance = 0f);

        void Stop();

        /// <summary>Quay ngay lập tức về hướng vị trí (chỉ trục Y).</summary>
        void FaceTowards(Vector3 worldPosition);

        /// <summary>Quay dần về hướng vị trí, gọi mỗi frame.</summary>
        void RotateTowards(Vector3 worldPosition, float degreesPerSecond);
    }
}
