using System;
using System.Collections.Generic;

namespace Mhieu.AI.BT
{
    public abstract class Composite : Node
    {
        protected readonly Node[] children;

        /// <summary>Dùng cho overlay debug (bước 7).</summary>
        public IReadOnlyList<Node> Children => children;

        protected Composite(params Node[] children)
        {
            this.children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }

    /// <summary>
    /// Chạy con theo thứ tự ưu tiên, trả về kết quả của con đầu tiên không Failure.
    /// PHẢN ỨNG: mỗi tick đều duyệt lại từ con đầu. Nếu một con ưu tiên cao hơn giành quyền
    /// (Success/Running) trong lúc con ưu tiên thấp đang Running, con thấp bị Abort.
    ///
    /// Lưu ý: Abort của con cũ chạy SAU OnEnter/OnTick của con mới. Vì vậy OnExit của node
    /// không nên vô điều kiện giải phóng tài nguyên dùng chung (ví dụ mover.Stop()).
    /// </summary>
    public sealed class Selector : Composite
    {
        private int _runningIndex = -1;

        public Selector(params Node[] children) : base(children) { }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            for (int i = 0; i < children.Length; i++)
            {
                NodeStatus status = children[i].Tick(ctx);
                if (status == NodeStatus.Failure) continue;

                if (_runningIndex != -1 && _runningIndex != i)
                    children[_runningIndex].Abort(ctx);

                _runningIndex = status == NodeStatus.Running ? i : -1;
                return status;
            }

            _runningIndex = -1;
            return NodeStatus.Failure;
        }

        protected override void OnAbort(BTContext ctx)
        {
            if (_runningIndex != -1) children[_runningIndex].Abort(ctx);
            _runningIndex = -1;
        }
    }

    /// <summary>
    /// Chạy con lần lượt, dừng ở Failure đầu tiên.
    /// CÓ BỘ NHỚ: khi một con đang Running, các tick sau tiếp tục từ con đó và KHÔNG kiểm tra lại
    /// các con đã Success phía trước. Nhờ vậy đòn đánh nhiều frame không bị hủy chỉ vì điều kiện
    /// (ví dụ InRange) đổi giữa chừng. Muốn hủy khi bị ngắt thì dùng Guard hoặc Selector phía trên.
    /// </summary>
    public sealed class Sequence : Composite
    {
        private int _index;

        public Sequence(params Node[] children) : base(children) { }

        protected override void OnEnter(BTContext ctx) => _index = 0;

        protected override NodeStatus OnTick(BTContext ctx)
        {
            while (_index < children.Length)
            {
                NodeStatus status = children[_index].Tick(ctx);

                if (status == NodeStatus.Running) return NodeStatus.Running;
                if (status == NodeStatus.Failure)
                {
                    _index = 0;
                    return NodeStatus.Failure;
                }

                _index++;
            }

            _index = 0;
            return NodeStatus.Success;
        }

        protected override void OnAbort(BTContext ctx)
        {
            if (_index < children.Length) children[_index].Abort(ctx);
            _index = 0;
        }
    }
}
