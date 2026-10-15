using System;

namespace Mhieu.AI.BT
{
    /// <summary>
    /// Bọc root + context. EnemyBrain (bước 3) giữ một instance và gọi Tick theo chu kỳ.
    /// Khi enemy bị Hit/Dead: gọi Abort() để mọi node đang chạy được dọn dẹp.
    /// </summary>
    public sealed class BehaviorTree
    {
        public Node Root { get; }
        public BTContext Context { get; }

        public BehaviorTree(Node root, BTContext context)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <param name="time">Time.time (có scale).</param>
        /// <param name="deltaTime">Thời gian từ lần tick trước.</param>
        public NodeStatus Tick(float time, float deltaTime)
        {
            Context.Time = time;
            Context.DeltaTime = deltaTime;
            return Root.Tick(Context);
        }

        public void Abort() => Root.Abort(Context);
    }
}
