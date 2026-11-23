using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>AbilityRunner đang chạy một chiêu (windup/recovery). Dùng với Inverter để nói "không bận".</summary>
    public sealed class AbilityBusyCondition : ConditionNode
    {
        protected override bool Check(BTContext ctx)
        {
            AbilityRunner runner = ctx.Get<AbilityRunner>();
            return runner != null && runner.IsBusy;
        }
    }

    /// <summary>Target đang gần hơn "distance" (tính trên mặt phẳng XZ).</summary>
    public sealed class TooCloseCondition : ConditionNode
    {
        private readonly float _distance;

        public TooCloseCondition(float distance) { _distance = distance; }

        protected override bool Check(BTContext ctx)
        {
            Blackboard bb = ctx.Blackboard;
            if (!bb.HasTarget) return false;

            return AIMath.FlatDistance(ctx.Owner.transform.position, bb.Target.position) < _distance;
        }
    }

    /// <summary>
    /// Không có vật cản nào (trong blockers) nằm giữa enemy và target.
    /// Chỉ đưa layer tường/vật cản vào blockers, KHÔNG đưa layer Player hay Enemy vào.
    /// </summary>
    public sealed class HasLineOfFireCondition : ConditionNode
    {
        private readonly LayerMask _blockers;
        private readonly float _height;

        public HasLineOfFireCondition(LayerMask blockers, float height)
        {
            _blockers = blockers;
            _height = height;
        }

        protected override bool Check(BTContext ctx)
        {
            Blackboard bb = ctx.Blackboard;
            if (!bb.HasTarget) return false;

            Vector3 from = ctx.Owner.transform.position + Vector3.up * _height;
            Vector3 to = bb.Target.position + Vector3.up * _height;
            return !Physics.Linecast(from, to, _blockers, QueryTriggerInteraction.Ignore);
        }
    }

    /// <summary>Đuổi theo target tới khi cách target "stopDistance" mét (khoảng cách tuyệt đối, không theo ability).</summary>
    public sealed class MoveToTargetAtDistanceAction : ActionNode
    {
        private readonly float _stopDistance;

        public MoveToTargetAtDistanceAction(float stopDistance)
        {
            _stopDistance = Mathf.Max(0.1f, stopDistance);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            Blackboard bb = ctx.Blackboard;
            if (mover == null || !bb.HasTarget) return NodeStatus.Failure;

            return mover.MoveTo(bb.Target.position, _stopDistance) ? NodeStatus.Running : NodeStatus.Failure;
        }
    }

    /// <summary>
    /// Lùi ra xa target (kite). Chọn điểm trên NavMesh ở phía đối diện target, thử các góc lệch nếu bị chắn,
    /// chọn lại điểm mỗi repickInterval giây vì target di chuyển.
    ///
    ///  - Success: hết maxTime, hoặc tới điểm lùi, hoặc đã lùi được nhưng giờ hết chỗ (bị dồn góc).
    ///  - Failure: ngay từ đầu không có chỗ lùi (bị dồn góc) hoặc không thể di chuyển (đang Hit/Attack...).
    ///    Failure để cây rơi xuống nhánh Bắn, tức là kẹt góc thì đứng bắn thay vì chạy mãi.
    /// </summary>
    public sealed class RetreatAction : ActionNode
    {
        private static readonly float[] Angles = { 0f, 45f, -45f, 90f, -90f };
        private const float MinGain = 1f;
        private const float RepickInterval = 0.4f;

        private readonly float _step;
        private readonly float _maxTime;

        private float _endTime;
        private float _nextRepick;
        private bool _hasMoved;

        public RetreatAction(float step, float maxTime)
        {
            _step = Mathf.Max(1f, step);
            _maxTime = Mathf.Max(0.2f, maxTime);
        }

        protected override void OnEnter(BTContext ctx)
        {
            _endTime = ctx.Time + _maxTime;
            _nextRepick = ctx.Time;
            _hasMoved = false;
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            Blackboard bb = ctx.Blackboard;
            if (mover == null || !bb.HasTarget) return NodeStatus.Failure;

            if (ctx.Time >= _endTime) return NodeStatus.Success;

            if (ctx.Time >= _nextRepick)
            {
                _nextRepick = ctx.Time + RepickInterval;

                if (!TryPickPoint(ctx.Owner.transform, bb.Target.position, out Vector3 point))
                    return _hasMoved ? NodeStatus.Success : NodeStatus.Failure;

                if (!mover.MoveTo(point, 0.3f)) return NodeStatus.Failure;
                _hasMoved = true;
            }
            else if (mover.HasArrived)
            {
                return NodeStatus.Success;
            }

            return NodeStatus.Running;
        }

        private bool TryPickPoint(Transform self, Vector3 targetPosition, out Vector3 point)
        {
            Vector3 origin = self.position;

            Vector3 away = origin - targetPosition;
            away.y = 0f;
            away = away.sqrMagnitude > 0.01f ? away.normalized : self.forward;

            float currentDistance = AIMath.FlatDistance(origin, targetPosition);

            for (int i = 0; i < Angles.Length; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(Angles[i], Vector3.up) * away;
                Vector3 candidate = origin + direction * _step;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit sample, 2f, NavMesh.AllAreas)) continue;

                Vector3 end = sample.position;

                // Có mép NavMesh chắn giữa đường thì dừng ngay trước mép.
                if (NavMesh.Raycast(origin, end, out NavMeshHit edge, NavMesh.AllAreas)) end = edge.position;

                if (AIMath.FlatDistance(end, targetPosition) >= currentDistance + MinGain)
                {
                    point = end;
                    return true;
                }
            }

            point = origin;
            return false;
        }
    }
}
