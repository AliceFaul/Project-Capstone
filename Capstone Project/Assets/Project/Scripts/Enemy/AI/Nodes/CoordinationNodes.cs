using UnityEngine;

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Bao quanh một nhánh "xông vào đánh". Chỉ cho chạy khi xin được thẻ tấn công, và TRẢ THẺ khi nhánh con
    /// kết thúc (Success hoặc Failure) hoặc bị hủy (bị Hit, chết, nhánh ưu tiên cao hơn giành quyền).
    /// Không có thẻ thì trả Failure ngay, để cây rơi xuống nhánh chờ ở vòng ngoài.
    /// </summary>
    public sealed class AttackTokenScope : Decorator
    {
        private bool _holds;

        public AttackTokenScope(Node child) : base(child) { }

        protected override void OnEnter(BTContext ctx)
        {
            _holds = AttackTokens.TryAcquire(ctx.Owner.transform);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            return _holds ? Child.Tick(ctx) : NodeStatus.Failure;
        }

        protected override void OnExit(BTContext ctx, NodeStatus status)
        {
            if (!_holds) return;

            AttackTokens.Release(ctx.Owner.transform);
            _holds = false;
        }
    }

    /// <summary>
    /// Đi tới khi vào tầm của chiêu thì Success (để node kế tiếp là UseAbility bắt đầu đánh ngay).
    /// Failure nếu mất target hoặc quá "timeout" giây mà chưa tới (bị kẹt), để nhả thẻ cho enemy khác.
    /// </summary>
    public sealed class ApproachUntilInRangeAction : ActionNode
    {
        private readonly AbilityDefinition _ability;
        private readonly float _timeout;
        private readonly float _stopDistance;

        private float _giveUpAt;

        public ApproachUntilInRangeAction(AbilityDefinition ability, float timeout, float stopFactor)
        {
            _ability = ability;
            _timeout = Mathf.Max(1f, timeout);
            _stopDistance = Mathf.Max(0.1f, ability.maxRange * stopFactor);
        }

        protected override void OnEnter(BTContext ctx)
        {
            _giveUpAt = ctx.Time + _timeout;
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            Blackboard bb = ctx.Blackboard;
            if (mover == null || !bb.HasTarget) return NodeStatus.Failure;

            float distance = AIMath.FlatDistance(ctx.Owner.transform.position, bb.Target.position);
            if (_ability.IsInRange(distance)) return NodeStatus.Success;

            if (ctx.Time >= _giveUpAt) return NodeStatus.Failure;

            return mover.MoveTo(bb.Target.position, _stopDistance) ? NodeStatus.Running : NodeStatus.Failure;
        }
    }

    /// <summary>
    /// Chờ ở "vòng ngoài" cách target ringRadius mét: ở xa thì tiến lại tới vòng, ở gần hơn thì lùi ra.
    /// Thỉnh thoảng (mỗi 2-4 giây) đi ngang một đoạn ngắn quanh target cho tự nhiên, và để đàn quái
    /// không dồn thành một cục. Luôn Running khi còn target; node cha quyết định khi nào dừng.
    /// </summary>
    public sealed class HoldRingAction : ActionNode
    {
        private const float Tolerance = 0.8f;
        private const float ShiftAngle = 35f;
        private const float ShiftDuration = 1f;

        private readonly float _ring;

        private float _nextShift;
        private float _shiftUntil;
        private float _shiftSign = 1f;

        public HoldRingAction(float ringRadius)
        {
            _ring = Mathf.Max(0.5f, ringRadius);
        }

        protected override void OnEnter(BTContext ctx)
        {
            _shiftUntil = 0f;
            _nextShift = ctx.Time + Random.Range(1f, 3f);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            Blackboard bb = ctx.Blackboard;
            if (mover == null || !bb.HasTarget) return NodeStatus.Failure;

            Transform self = ctx.Owner.transform;
            Vector3 target = bb.Target.position;

            Vector3 outward = self.position - target;
            outward.y = 0f;
            float distance = outward.magnitude;
            outward = distance > 0.01f ? outward / distance : self.forward;

            bool atRing = Mathf.Abs(distance - _ring) <= Tolerance;
            if (atRing && ctx.Time >= _nextShift && ctx.Time >= _shiftUntil)
            {
                _shiftUntil = ctx.Time + ShiftDuration;
                _nextShift = _shiftUntil + Random.Range(2f, 4f);
                _shiftSign = Random.value < 0.5f ? -1f : 1f;
            }

            float angle = ctx.Time < _shiftUntil ? ShiftAngle * _shiftSign : 0f;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * outward;
            Vector3 point = target + direction * _ring;

            return mover.MoveTo(point, 0.3f) ? NodeStatus.Running : NodeStatus.Failure;
        }
    }
}
