using UnityEngine;

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Dùng một chiêu. Running trong lúc AbilityRunner đang chạy (windup + recovery),
    /// Success khi chiêu hoàn tất, Failure nếu không bắt đầu được hoặc bị ngắt.
    /// Bị hủy (Abort) thì hủy luôn chiêu đang chạy.
    /// </summary>
    public sealed class UseAbilityAction : ActionNode
    {
        private readonly AbilityDefinition _ability;
        private bool _started;

        public UseAbilityAction(AbilityDefinition ability) { _ability = ability; }

        protected override void OnEnter(BTContext ctx)
        {
            AbilityRunner runner = ctx.Get<AbilityRunner>();
            _started = runner != null
                       && ctx.Blackboard.HasTarget
                       && runner.Begin(_ability, ctx.Blackboard.Target);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            if (!_started) return NodeStatus.Failure;

            AbilityRunner runner = ctx.Get<AbilityRunner>();
            if (runner.IsBusy) return NodeStatus.Running;

            return runner.LastResult == AbilityResult.Completed ? NodeStatus.Success : NodeStatus.Failure;
        }

        protected override void OnAbort(BTContext ctx)
        {
            ctx.Get<AbilityRunner>()?.Cancel();
        }
    }

    /// <summary>
    /// Đuổi theo target tới khi vào tầm đánh (dừng ở stopFactor x tầm tối đa của chiêu).
    /// Luôn Running khi còn target; node cha (Guard) quyết định khi nào dừng.
    /// KHÔNG gọi mover.Stop() khi thoát: lúc chuyển sang đánh, AbilityRunner tự dừng mover.
    /// </summary>
    public sealed class MoveToTargetAction : ActionNode
    {
        private readonly float _stopDistance;

        public MoveToTargetAction(AbilityDefinition ability, float stopFactor)
        {
            _stopDistance = Mathf.Max(0.1f, ability.maxRange * stopFactor);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            Blackboard bb = ctx.Blackboard;
            if (mover == null || !bb.HasTarget) return NodeStatus.Failure;

            return mover.MoveTo(bb.Target.position, _stopDistance) ? NodeStatus.Running : NodeStatus.Failure;
        }
    }

    /// <summary>Đi về vị trí gốc. Success khi tới nơi.</summary>
    public sealed class ReturnHomeAction : ActionNode
    {
        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            EnemyAIConfig config = ctx.Get<EnemyAIConfig>();
            if (mover == null || config == null) return NodeStatus.Failure;

            if (!mover.MoveTo(ctx.Blackboard.HomePosition, config.homeArriveDistance * 0.5f))
                return NodeStatus.Failure;

            return mover.HasArrived ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    /// <summary>Đứng yên, chạy mãi cho tới khi nhánh ưu tiên cao hơn giành quyền.</summary>
    public sealed class IdleAction : ActionNode
    {
        protected override void OnEnter(BTContext ctx)
        {
            ctx.Get<IMover>()?.Stop();
        }

        protected override NodeStatus OnTick(BTContext ctx) => NodeStatus.Running;
    }
}
