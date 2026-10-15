using System;

namespace Mhieu.AI.BT
{
    /// <summary>
    /// Node hành động (di chuyển, đánh...). Có thể chạy nhiều frame: trả Running cho tới khi xong.
    /// Dọn dẹp đặt ở OnExit (chạy cả khi Success/Failure lẫn khi bị Abort).
    /// </summary>
    public abstract class ActionNode : Node { }

    /// <summary>Node điều kiện tức thời: chỉ trả Success hoặc Failure, không bao giờ Running.</summary>
    public abstract class ConditionNode : Node
    {
        protected abstract bool Check(BTContext ctx);

        protected sealed override NodeStatus OnTick(BTContext ctx)
            => Check(ctx) ? NodeStatus.Success : NodeStatus.Failure;
    }

    /// <summary>Chờ "duration" giây (theo ctx.Time) rồi Success.</summary>
    public sealed class WaitNode : ActionNode
    {
        private readonly float _duration;
        private float _endTime;

        public WaitNode(float duration) { _duration = duration; }

        protected override void OnEnter(BTContext ctx) => _endTime = ctx.Time + _duration;

        protected override NodeStatus OnTick(BTContext ctx)
            => ctx.Time >= _endTime ? NodeStatus.Success : NodeStatus.Running;
    }

    // Hai node dưới dùng delegate: chỉ để prototype nhanh và viết test.
    // Node dùng thật nên là lớp riêng (dễ tái sử dụng, dễ debug).

    public sealed class FuncCondition : ConditionNode
    {
        private readonly Func<BTContext, bool> _check;
        public FuncCondition(Func<BTContext, bool> check) { _check = check; }
        protected override bool Check(BTContext ctx) => _check(ctx);
    }

    public sealed class FuncAction : ActionNode
    {
        private readonly Func<BTContext, NodeStatus> _tick;
        public FuncAction(Func<BTContext, NodeStatus> tick) { _tick = tick; }
        protected override NodeStatus OnTick(BTContext ctx) => _tick(ctx);
    }
}
