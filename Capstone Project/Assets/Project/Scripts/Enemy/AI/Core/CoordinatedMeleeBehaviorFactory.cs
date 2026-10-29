using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;
    using Mhieu.Enemy.Nodes;

    /// <summary>
    /// Cây cận chiến có phối hợp (dùng thay cho "Melee" khi có nhiều quái), ưu tiên từ trên xuống:
    ///
    /// Root: Selector
    ///  1. Guard(IsDisabled) -> Idle                            bị Hit/Knockback thì đứng yên
    ///  2. AttackTokenScope(                                    chỉ vào đánh khi xin được thẻ
    ///        Sequence: có target, chiêu sẵn sàng, tiến tới tầm, dùng chiêu )
    ///  3. Guard(HasTarget) -> HoldRing                         không có thẻ hoặc chiêu đang hồi: chờ ở vòng ngoài
    ///  4. Guard(NeedsHome) -> Về nhà
    ///  5. Idle
    ///
    /// Vì sao chiêu đang hồi thì cũng lui ra vòng ngoài: nhường chỗ gần player cho quái khác xông vào,
    /// nên cả bầy luân phiên đánh thay vì đứng áp sát nhau.
    /// </summary>
    [CreateAssetMenu(fileName = "CoordinatedMeleeBehavior", menuName = "Data/Enemy Behavior/Melee (Coordinated)")]
    public class CoordinatedMeleeBehaviorFactory : EnemyBehaviorFactory
    {
        [Tooltip("Vòng chờ cách player = tầm tối đa của chiêu + khoảng này.")]
        [SerializeField, Min(0.5f)] private float waitRingExtra = 2.5f;
        [Tooltip("Đi tới tầm đánh mà quá thời gian này (bị kẹt) thì bỏ, nhả thẻ cho quái khác.")]
        [SerializeField, Min(1f)] private float approachTimeout = 6f;
        [Tooltip("Dừng cách target hệ số này x tầm tối đa của chiêu.")]
        [SerializeField, Range(0.3f, 1f)] private float stopDistanceFactor = 0.8f;

        public override Node Build(EnemyAIConfig config)
        {
            AbilityDefinition ability = config.primaryAbility;
            float ring = ability.maxRange + waitRingExtra;

            var engage = new AttackTokenScope(
                new Sequence(
                    new HasTargetCondition(),
                    new AbilityReadyCondition(ability),
                    new ApproachUntilInRangeAction(ability, approachTimeout, stopDistanceFactor) { Name = "Approach" },
                    new UseAbilityAction(ability) { Name = "UseAbility" }) { Name = "EngageSequence" })
            { Name = "Engage" };

            return new Selector(
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                engage,
                new Guard(new HasTargetCondition(), new HoldRingAction(ring)) { Name = "HoldRing" },
                new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" },
                new IdleAction { Name = "Idle" }) { Name = "Root" };
        }
    }
}
