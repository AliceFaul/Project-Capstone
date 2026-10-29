using UnityEngine;

// "using Mhieu.AI.BT" đặt BÊN TRONG namespace để tên Node/Selector/Sequence/NodeStatus
// luôn trỏ tới bản trong Mhieu.AI.BT, kể cả khi project còn file BT cũ khai báo ở namespace global.
namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    public sealed class IsDisabledCondition : ConditionNode
    {
        protected override bool Check(BTContext ctx) => ctx.Blackboard.IsDisabled;
    }

    /// <summary>Đang aggro và còn target hợp lệ.</summary>
    public sealed class HasTargetCondition : ConditionNode
    {
        protected override bool Check(BTContext ctx)
            => ctx.Blackboard.IsAggro && ctx.Blackboard.HasTarget;
    }

    public sealed class InAbilityRangeCondition : ConditionNode
    {
        private readonly AbilityDefinition _ability;

        public InAbilityRangeCondition(AbilityDefinition ability) { _ability = ability; }

        protected override bool Check(BTContext ctx)
        {
            AbilityRunner runner = ctx.Get<AbilityRunner>();
            return runner != null && runner.InRange(_ability, ctx.Blackboard.Target);
        }
    }

    public sealed class AbilityReadyCondition : ConditionNode
    {
        private readonly AbilityDefinition _ability;

        public AbilityReadyCondition(AbilityDefinition ability) { _ability = ability; }

        protected override bool Check(BTContext ctx)
        {
            AbilityRunner runner = ctx.Get<AbilityRunner>();
            return runner != null && runner.IsReady(_ability);
        }
    }

    /// <summary>Không còn aggro và đang đứng cách nhà xa hơn homeArriveDistance.</summary>
    public sealed class NeedsHomeCondition : ConditionNode
    {
        protected override bool Check(BTContext ctx)
        {
            Blackboard bb = ctx.Blackboard;
            if (bb.IsAggro) return false;

            EnemyAIConfig config = ctx.Get<EnemyAIConfig>();
            if (config == null) return false;

            return AIMath.FlatDistance(ctx.Owner.transform.position, bb.HomePosition) > config.homeArriveDistance;
        }
    }
}
