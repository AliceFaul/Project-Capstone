using UnityEngine;

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>ChargeRunner đang chạy cú lao (windup/dash/recovery). Dùng với Inverter để nói "không bận".</summary>
    public sealed class ChargeBusyCondition : ConditionNode
    {
        private ChargeRunner _runner;
        private bool _resolved;

        protected override bool Check(BTContext ctx)
        {
            if (!_resolved)
            {
                _resolved = true;
                _runner = ctx.Owner.GetComponent<ChargeRunner>();
            }

            return _runner != null && _runner.IsBusy;
        }
    }

    /// <summary>
    /// Dùng chiêu lao tới khi: đủ pha, hết hồi chiêu, target trong tầm [minRange, maxRange], không có chiêu nào đang chạy.
    /// Failure ngay nếu chưa thể (để cây rơi xuống các chiêu thường).
    /// Running trong lúc lao; Success khi hoàn tất; bị hủy thì hủy luôn cú lao.
    /// </summary>
    public sealed class UseChargeAction : ActionNode
    {
        private readonly ChargeDefinition _definition;
        private readonly int _minPhase;

        private ChargeRunner _runner;
        private bool _resolved;
        private bool _started;

        public UseChargeAction(ChargeDefinition definition, int minPhase)
        {
            _definition = definition;
            _minPhase = minPhase;
        }

        private ChargeRunner Runner(BTContext ctx)
        {
            if (!_resolved)
            {
                _resolved = true;
                _runner = ctx.Owner.GetComponent<ChargeRunner>();
            }

            return _runner;
        }

        protected override void OnEnter(BTContext ctx)
        {
            _started = false;

            ChargeRunner runner = Runner(ctx);
            Blackboard bb = ctx.Blackboard;
            if (runner == null || _definition == null || !bb.HasTarget) return;
            if (bb.Phase < _minPhase) return;
            if (!runner.IsReady(_definition) || !runner.InRange(_definition, bb.Target)) return;

            _started = runner.Begin(_definition, bb.Target);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            if (!_started) return NodeStatus.Failure;

            ChargeRunner runner = Runner(ctx);
            if (runner.IsBusy) return NodeStatus.Running;

            return runner.LastResult == AbilityResult.Completed ? NodeStatus.Success : NodeStatus.Failure;
        }

        protected override void OnAbort(BTContext ctx)
        {
            Runner(ctx)?.Cancel();
        }
    }
}
