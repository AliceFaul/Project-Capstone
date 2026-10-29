using UnityEngine;

namespace Mhieu.Enemy
{
    public enum AbilityKind
    {
        /// <summary>Hitbox hình cầu ngay trước mặt enemy.</summary>
        Melee,
        /// <summary>Bắn EnemyProjectile về phía mục tiêu.</summary>
        Projectile,
        /// <summary>Vùng tròn trên mặt đất (slam, nổ). Tâm khóa vào lúc bắt đầu windup.</summary>
        GroundAoE
    }

    /// <summary>
    /// Dữ liệu một chiêu của enemy. Cận chiến = 1 asset Melee, tầm xa = 1 asset Projectile,
    /// boss = nhiều asset. Một asset dùng chung cho nhiều enemy được (cooldown tính riêng theo từng enemy).
    ///
    /// Mọi khoảng cách "range" tính từ tâm enemy tới tâm mục tiêu trên mặt phẳng XZ.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyAbility", menuName = "Data/Enemy Ability")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("General")]
        public AbilityKind kind = AbilityKind.Melee;
        [Min(0f)] public float minRange = 0f;
        [Min(0f)] public float maxRange = 1.8f;
        [Tooltip("Từ lúc bắt đầu tới lúc đòn trúng. Người chơi click né trong khoảng này nên đừng để dưới ~0.4s.")]
        [Min(0f)] public float windupTime = 0.5f;
        [Tooltip("Thời gian đứng yên sau khi đòn trúng.")]
        [Min(0f)] public float recoveryTime = 0.6f;
        [Tooltip("Tính từ lúc chiêu kết thúc (hoặc bị ngắt).")]
        [Min(0f)] public float cooldown = 2f;
        [Tooltip("Sát thương = hệ số này x TotalDamage của enemy.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Tooltip("Quay theo mục tiêu trong lúc windup (đòn nhắm cố định thì tắt).")]
        public bool trackTargetDuringWindup = true;
        [Min(0f)] public float windupTurnSpeed = 360f;

        [Header("Animation")]
        [Tooltip("Gán vào parameter AttackId của Animator (1 = chém, 2 = bắn, 3 = slam...).")]
        public int animId = 1;

        [Header("Melee")]
        [Min(0f)] public float hitRadius = 1.2f;
        [Tooltip("Tâm hitbox lệch về phía trước enemy.")]
        public float hitForwardOffset = 1f;
        [Tooltip("Độ cao tâm hitbox so với chân enemy.")]
        public float hitHeight = 0.8f;

        [Header("Projectile")]
        public EnemyProjectile projectilePrefab;
        [Min(0.1f)] public float projectileSpeed = 10f;
        [Min(0.1f)] public float projectileLifetime = 3f;
        [Min(1)] public int projectileCount = 1;
        [Tooltip("Tổng góc quạt (độ) khi bắn nhiều viên.")]
        [Range(0f, 180f)] public float spreadAngle = 0f;

        [Header("Ground AoE")]
        [Min(0f)] public float aoeRadius = 2.5f;
        [Tooltip("Bật: tâm tại vị trí mục tiêu lúc bắt đầu windup. Tắt: tâm tại chân enemy.")]
        public bool aoeCenterOnTarget = true;

        [Header("Telegraph (vòng cảnh báo dưới đất)")]
        [Tooltip("Prefab phẳng 1x1 đơn vị (decal/quad), sẽ được scale theo đường kính. Để trống nếu chưa có.")]
        public GameObject telegraphPrefab;
        public float telegraphYOffset = 0.05f;

        public bool IsInRange(float flatDistance) => flatDistance >= minRange && flatDistance <= maxRange;

        private void OnValidate()
        {
            if (maxRange < minRange) maxRange = minRange;
            if (projectileCount < 1) projectileCount = 1;
        }
    }
}
