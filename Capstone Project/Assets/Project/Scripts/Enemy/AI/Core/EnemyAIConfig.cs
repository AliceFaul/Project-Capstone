using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// Dữ liệu hành vi AI của một loại enemy (tách khỏi CharacterData để designer chỉnh độc lập).
    /// Khoảng cách tính trên mặt phẳng XZ.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyAIConfig", menuName = "Data/Enemy AI Config")]
    public class EnemyAIConfig : ScriptableObject
    {
        [Header("Perception")]
        [Tooltip("Player vào trong bán kính này thì enemy phát hiện và bắt đầu đuổi.")]
        [Min(0f)] public float aggroRadius = 8f;
        [Tooltip("Bật: tường chắn giữa enemy và player thì không phát hiện được.")]
        public bool requireLineOfSight;
        public LayerMask lineOfSightBlockers;
        [Tooltip("Độ cao điểm nhìn so với chân.")]
        public float eyeHeight = 1.2f;

        [Header("Leash (bỏ đuổi)")]
        [Tooltip("Player xa hơn khoảng này (tính từ enemy) thì enemy bỏ đuổi.")]
        [Min(0f)] public float loseRadius = 14f;
        [Tooltip("Enemy rời vị trí gốc xa hơn khoảng này thì bỏ đuổi và quay về.")]
        [Min(0f)] public float leashDistance = 18f;
        [Tooltip("Coi như đã về tới nhà khi cách vị trí gốc nhỏ hơn khoảng này.")]
        [Min(0.1f)] public float homeArriveDistance = 0.6f;

        [Header("Group alert")]
        [Tooltip("Khi một enemy bị đánh, enemy khác trong bán kính này cũng vào trạng thái chiến đấu. 0 = tắt.")]
        [Min(0f)] public float alertRadius = 6f;

        [Header("Behavior")]
        [Tooltip("Chiêu chính (cây cận chiến dùng chiêu này).")]
        public AbilityDefinition primaryAbility;
        [Tooltip("Chu kỳ tick của cây (giây). 0.1 = 10 lần/giây.")]
        [Range(0.05f, 0.5f)] public float thinkInterval = 0.1f;

        private void OnValidate()
        {
            if (loseRadius < aggroRadius) loseRadius = aggroRadius;
            if (leashDistance < aggroRadius) leashDistance = aggroRadius;
        }
    }
}
