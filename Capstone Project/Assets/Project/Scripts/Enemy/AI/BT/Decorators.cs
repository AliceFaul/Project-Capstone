using System;

namespace Mhieu.AI.BT
{
    public abstract class Decorator : Node
    {
        public Node Child { get; }

        protected Decorator(Node child)
        {
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        protected override void OnAbort(BTContext ctx) => Child.Abort(ctx);
    }

    /// <summary>Đảo Success &lt;-&gt; Failure, giữ nguyên Running.</summary>
    public sealed class Inverter : Decorator
    {
        public Inverter(Node child) : base(child) { }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            switch (Child.Tick(ctx))
            {
                case NodeStatus.Success: return NodeStatus.Failure;
                case NodeStatus.Failure: return NodeStatus.Success;
                default: return NodeStatus.Running;
            }
        }
    }

    /// <summary>
    /// Kiểm tra điều kiện MỖI tick. Điều kiện không còn Success thì hủy con đang chạy và trả Failure.
    /// Ví dụ: Guard(IsDisabled, Wait) hoặc Guard(Phase == 3, EnrageBranch).
    /// Điều kiện nên là ConditionNode (tức thời); Running được coi như Failure.
    /// </summary>
    public sealed class Guard : Decorator
    {
        private readonly Node _condition;

        public Guard(Node condition, Node child) : base(child)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            if (_condition.Tick(ctx) != NodeStatus.Success)
            {
                Child.Abort(ctx);
                return NodeStatus.Failure;
            }

            return Child.Tick(ctx);
        }
    }

    /// <summary>
    /// Sau khi con chạy xong Success, trả Failure trong "duration" giây (theo ctx.Time có scale).
    /// Con bị hủy giữa chừng cũng tính là đã dùng nên bị cooldown (đòn bị ngắt vẫn tốn lượt).
    /// </summary>
    public sealed class Cooldown : Decorator
    {
        private readonly float _duration;
        private float _readyAt = float.NegativeInfinity;

        public Cooldown(float duration, Node child) : base(child)
        {
            _duration = duration;
        }

        public bool IsReady(BTContext ctx) => ctx.Time >= _readyAt;

        protected override NodeStatus OnTick(BTContext ctx)
        {
            if (!Child.IsRunning && ctx.Time < _readyAt)
                return NodeStatus.Failure;

            NodeStatus status = Child.Tick(ctx);
            if (status == NodeStatus.Success)
                _readyAt = ctx.Time + _duration;

            return status;
        }

        protected override void OnAbort(BTContext ctx)
        {
            base.OnAbort(ctx);
            _readyAt = ctx.Time + _duration;
        }
    }
}
