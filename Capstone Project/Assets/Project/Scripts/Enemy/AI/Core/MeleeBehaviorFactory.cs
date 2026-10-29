using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;
    using Mhieu.Enemy.Nodes;

    /// <summary>
    /// Cây cận chiến, ưu tiên từ trên xuống:
    ///
    /// Root: Selector
    ///  1. Guard(IsDisabled)  -> Idle                       bị Hit/Knockback thì đứng yên
    ///  2. Sequence           -> Có target, trong tầm, chiêu sẵn sàng, dùng chiêu
    ///  3. Guard(HasTarget)   -> Đuổi theo target
    ///  4. Guard(NeedsHome)   -> Về nhà
    ///  5. Idle
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeBehavior", menuName = "Data/Enemy Behavior/Melee")]
    public class MeleeBehaviorFactory : EnemyBehaviorFactory
    {
        [Tooltip("Đuổi tới khi cách target bằng hệ số này x tầm tối đa của chiêu.")]
        [SerializeField, Range(0.3f, 1f)] private float stopDistanceFactor = 0.8f;

        public override Node Build(EnemyAIConfig config)
        {
            AbilityDefinition ability = config.primaryAbility;

            return new Selector(
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                new Sequence(
                    new HasTargetCondition(),
                    new InAbilityRangeCondition(ability),
                    new AbilityReadyCondition(ability),
                    new UseAbilityAction(ability) { Name = "UseAbility" }) { Name = "Attack" },
                new Guard(new HasTargetCondition(), new MoveToTargetAction(ability, stopDistanceFactor)) { Name = "Chase" },
                new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" },
                new IdleAction { Name = "Idle" }) { Name = "Root" };
        }
    }
}
