using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;
    using Mhieu.Enemy.Nodes;

    /// <summary>
    /// Cây cận chiến "thấy player là dí và đánh", KHÔNG có thẻ tấn công và KHÔNG chờ ở vòng ngoài:
    /// mọi con đều xông vào đánh cùng lúc. Khi player chết thì đi lang thang sang chỗ khác.
    ///
    /// Root: Selector
    ///  1. Guard(IsDisabled)  -> Idle           bị Hit/Knockback thì đứng yên
    ///  2. Guard(HasTarget)   "Combat"          có target (đã thấy player):
    ///        a. Sequence: trong tầm + chiêu sẵn sàng -> dùng chiêu
    ///        b. Chase: đuổi tới khi vào tầm (dừng ở stopDistanceFactor x tầm tối đa của chiêu)
    ///  3. Guard(PlayerDead)  -> Wander         player chết: đi sang chỗ khác, nghỉ một chút rồi đi tiếp
    ///  4. Guard(NeedsHome)   -> Về nhà         bị leash bỏ đuổi
    ///  5. Idle
    ///
    /// Dùng chiêu Primary Ability của EnemyAIConfig. Cần Aggro Radius đủ lớn trong config để "thấy" từ xa.
    /// </summary>
    [CreateAssetMenu(fileName = "ChaseMeleeBehavior", menuName = "Data/Enemy Behavior/Melee (Chase)")]
    public class ChaseMeleeBehaviorFactory : EnemyBehaviorFactory
    {
        [Tooltip("Dừng cách target hệ số này x tầm tối đa của chiêu.")]
        [SerializeField, Range(0.3f, 1f)] private float stopDistanceFactor = 0.8f;

        [Header("Khi player chết")]
        [Tooltip("Mỗi lần đi tới một điểm cách chỗ đang đứng trong khoảng này (mét).")]
        [SerializeField, Min(1f)] private float wanderMinDistance = 6f;
        [SerializeField, Min(1f)] private float wanderMaxDistance = 14f;
        [Tooltip("Tới điểm rồi nghỉ bao nhiêu giây trước khi đi tiếp.")]
        [SerializeField, Min(0f)] private float wanderPause = 2.5f;

        public override Node Build(EnemyAIConfig config)
        {
            AbilityDefinition ability = config.primaryAbility;

            var combat = new Selector(
                new Sequence(
                    new InAbilityRangeCondition(ability),
                    new AbilityReadyCondition(ability),
                    new UseAbilityAction(ability) { Name = "UseAbility" }) { Name = "Attack" },
                new MoveToTargetAction(ability, stopDistanceFactor) { Name = "Chase" }) { Name = "CombatBranches" };

            return new Selector(
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                new Guard(new HasTargetCondition(), combat) { Name = "Combat" },
                new Guard(new PlayerDeadCondition(),
                    new WanderAwayAction(wanderMinDistance, wanderMaxDistance, wanderPause) { Name = "WanderAway" })
                { Name = "PlayerDead" },
                new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" },
                new IdleAction { Name = "Idle" }) { Name = "Root" };
        }
    }
}

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>Player có trong scene nhưng đã chết.</summary>
    public sealed class PlayerDeadCondition : ConditionNode
    {
        protected override bool Check(BTContext ctx)
        {
            bool alive = PlayerRegistry.TryGetAlive(out Transform player);
            return !alive && player != null;
        }
    }

    /// <summary>
    /// Đi lang thang: chọn một điểm trên NavMesh cách chỗ đang đứng minDistance đến maxDistance, đi tới,
    /// nghỉ "pause" giây, rồi chọn điểm khác. Luôn Running; nhánh ưu tiên cao hơn (thấy player) giành quyền thì tự dừng.
    /// </summary>
    public sealed class WanderAwayAction : ActionNode
    {
        private readonly float _minDistance;
        private readonly float _maxDistance;
        private readonly float _pause;

        private Vector3 _destination;
        private bool _hasDestination;
        private float _resumeAt;

        public WanderAwayAction(float minDistance, float maxDistance, float pause)
        {
            _minDistance = Mathf.Max(1f, minDistance);
            _maxDistance = Mathf.Max(_minDistance, maxDistance);
            _pause = Mathf.Max(0f, pause);
        }

        protected override void OnEnter(BTContext ctx)
        {
            _hasDestination = false;
            _resumeAt = ctx.Time;
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            IMover mover = ctx.Get<IMover>();
            if (mover == null) return NodeStatus.Failure;

            if (ctx.Time < _resumeAt) return NodeStatus.Running;

            if (!_hasDestination)
            {
                if (!TryPickDestination(ctx.Owner.transform.position, out _destination))
                {
                    _resumeAt = ctx.Time + 1f;
                    return NodeStatus.Running;
                }

                _hasDestination = true;
            }

            if (!mover.MoveTo(_destination, 0.5f))
            {
                _hasDestination = false;
                _resumeAt = ctx.Time + 0.5f;
                return NodeStatus.Running;
            }

            if (mover.HasArrived)
            {
                _hasDestination = false;
                _resumeAt = ctx.Time + _pause;
                mover.Stop();
            }

            return NodeStatus.Running;
        }

        private bool TryPickDestination(Vector3 from, out Vector3 result)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                if (direction.sqrMagnitude < 0.01f) continue;

                float distance = Random.Range(_minDistance, _maxDistance);
                Vector3 candidate = from + new Vector3(direction.x, 0f, direction.y) * distance;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = from;
            return false;
        }
    }
}
