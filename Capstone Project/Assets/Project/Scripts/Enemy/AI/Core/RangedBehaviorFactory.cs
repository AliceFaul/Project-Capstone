using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;
    using Mhieu.Enemy.Nodes;

    /// <summary>
    /// Cây tầm xa (kite và bắn), ưu tiên từ trên xuống:
    ///
    /// Root: Selector
    ///  1. Guard(IsDisabled) -> Idle                        bị Hit/Knockback thì đứng yên
    ///  2. Cooldown( Sequence: không đang bắn, có target, target quá gần, Lùi )     kite
    ///  3. Sequence: có target, trong tầm, có đường bắn, chiêu sẵn sàng, Bắn
    ///  4. Guard(có target nhưng bị che) -> đi lại gần để có đường bắn
    ///  5. Guard(có target) -> đứng ở khoảng cách ưa thích
    ///  6. Về nhà
    ///  7. Idle
    ///
    /// Vì sao có Cooldown ở nhánh Lùi: nếu không, player đuổi sát thì enemy lùi mãi và không bao giờ bắn.
    /// Sau mỗi đợt lùi (tối đa retreatMaxTime) enemy phải đứng bắn ít nhất retreatCooldown giây.
    /// Nhánh Lùi KHÔNG chạy khi đang bắn, nên không bao giờ hủy giữa chừng một phát đã bắt đầu.
    ///
    /// Ability của enemy tầm xa nên để minRange = 0 (khoảng cách nên giữ do "Retreat Below Distance" quyết định).
    /// </summary>
    [CreateAssetMenu(fileName = "RangedBehavior", menuName = "Data/Enemy Behavior/Ranged")]
    public class RangedBehaviorFactory : EnemyBehaviorFactory
    {
        [Header("Kiting")]
        [Tooltip("Player gần hơn khoảng này thì enemy lùi.")]
        [SerializeField, Min(0f)] private float retreatBelowDistance = 4f;
        [Tooltip("Mỗi lần chọn điểm lùi cách enemy bao xa.")]
        [SerializeField, Min(1f)] private float retreatStep = 5f;
        [Tooltip("Một đợt lùi kéo dài tối đa bao lâu.")]
        [SerializeField, Min(0.2f)] private float retreatMaxTime = 1.5f;
        [Tooltip("Sau một đợt lùi, enemy phải đứng bắn tối thiểu bao lâu trước khi lùi tiếp.")]
        [SerializeField, Min(0f)] private float retreatCooldown = 3f;

        [Header("Positioning")]
        [Tooltip("Khi chưa bắn được, đứng cách target hệ số này x tầm tối đa của chiêu.")]
        [SerializeField, Range(0.3f, 1f)] private float preferredRangeFactor = 0.8f;
        [Tooltip("Khi bị tường che, đi lại gần target tới khoảng cách này rồi thử lại.")]
        [SerializeField, Min(1f)] private float repositionStopDistance = 2f;

        [Header("Line of fire")]
        [Tooltip("Layer vật cản chặn đạn (tường, đá...). KHÔNG chọn Player, Enemy, hay layer mặt đất.")]
        [SerializeField] private LayerMask lineOfFireBlockers;
        [Tooltip("Độ cao kiểm tra đường bắn so với chân.")]
        [SerializeField] private float lineOfFireHeight = 1f;

        public override Node Build(EnemyAIConfig config)
        {
            AbilityDefinition ability = config.primaryAbility;

            var kite = new Cooldown(
                retreatCooldown,
                new Sequence(
                    new Inverter(new AbilityBusyCondition()),
                    new HasTargetCondition(),
                    new TooCloseCondition(retreatBelowDistance),
                    new RetreatAction(retreatStep, retreatMaxTime) { Name = "Retreat" }) { Name = "KiteSequence" })
            { Name = "Kite" };

            var shoot = new Sequence(
                new HasTargetCondition(),
                new InAbilityRangeCondition(ability),
                new HasLineOfFireCondition(lineOfFireBlockers, lineOfFireHeight),
                new AbilityReadyCondition(ability),
                new UseAbilityAction(ability) { Name = "Shoot" }) { Name = "ShootSequence" };

            var reposition = new Guard(
                new Sequence(
                    new HasTargetCondition(),
                    new Inverter(new HasLineOfFireCondition(lineOfFireBlockers, lineOfFireHeight))),
                new MoveToTargetAtDistanceAction(repositionStopDistance)) { Name = "Reposition" };

            return new Selector(
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                kite,
                shoot,
                reposition,
                new Guard(new HasTargetCondition(), new MoveToTargetAction(ability, preferredRangeFactor)) { Name = "Approach" },
                new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" },
                new IdleAction { Name = "Idle" }) { Name = "Root" };
        }
    }
}
