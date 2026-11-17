using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Dữ liệu chiêu "lao tới" của boss: đứng niệm (có vạch cảnh báo hình chữ nhật), khóa hướng,
    /// rồi lao thẳng một đoạn, trúng player thì gây sát thương một lần.
    ///
    /// Tạo asset: Project > chuột phải > Create > Data > Enemy Charge.
    /// </summary>
    [CreateAssetMenu(fileName = "BossCharge", menuName = "Data/Enemy Charge")]
    public class ChargeDefinition : ScriptableObject
    {
        [Header("Điều kiện dùng")]
        [Tooltip("Chỉ lao khi player cách boss trong khoảng [minRange, maxRange].")]
        [Min(0f)] public float minRange = 4f;
        [Min(0.5f)] public float maxRange = 12f;
        [Min(0f)] public float cooldown = 12f;

        [Header("Niệm (windup)")]
        [Min(0.1f)] public float windupTime = 1f;
        [Tooltip("Trong khoảng này CUỐI windup, hướng lao bị khóa (player kịp né). 0 = theo dõi tới phút chót.")]
        [Min(0f)] public float aimLockTime = 0.35f;
        [Tooltip("Tốc độ xoay của boss khi đang ngắm (độ/giây).")]
        [Min(0f)] public float aimTurnSpeed = 720f;

        [Header("Lao")]
        [Min(1f)] public float chargeSpeed = 14f;
        [Min(1f)] public float chargeDistance = 10f;
        [Min(0f)] public float recoveryTime = 0.8f;

        [Header("Sát thương")]
        [Min(0f)] public float damageMultiplier = 2f;
        [Min(0.1f)] public float hitRadius = 1.2f;
        public float hitHeight = 1f;
        [Tooltip("Trúng player thì dừng lao ngay (nếu tắt, boss lao xuyên qua).")]
        public bool stopOnHit = true;

        [Header("Hiển thị")]
        [Tooltip("Prefab có component TelegraphBar. Để trống = không hiện vạch cảnh báo.")]
        public TelegraphBar telegraphPrefab;
        public float telegraphYOffset = 0.05f;
        [Tooltip("Giá trị gán vào Animator parameter AttackId (int) khi bắt đầu chiêu.")]
        public int animId = 4;

        public bool IsInRange(float flatDistance) => flatDistance >= minRange && flatDistance <= maxRange;
    }
}
