using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>BossPhaseController đang chuyển pha.</summary>
    public sealed class IsTransitioningCondition : ConditionNode
    {
        private BossPhaseController _controller;

        protected override bool Check(BTContext ctx)
        {
            if (_controller == null) _controller = ctx.Owner.GetComponent<BossPhaseController>();
            return _controller != null && _controller.IsTransitioning;
        }
    }

    /// <summary>Pha hiện tại (Blackboard.Phase) từ "phase" trở lên.</summary>
    public sealed class PhaseAtLeastCondition : ConditionNode
    {
        private readonly int _phase;

        public PhaseAtLeastCondition(int phase) { _phase = phase; }

        protected override bool Check(BTContext ctx) => ctx.Blackboard.Phase >= _phase;
    }

    /// <summary>
    /// Chọn và dùng một chiêu trong kho. Mỗi lần bắt đầu:
    ///  - Lọc: đủ pha, hết hồi chiêu, target trong tầm của chiêu đó.
    ///  - Không chọn lại chiêu vừa dùng nếu còn chiêu khác dùng được.
    ///  - Bốc ngẫu nhiên theo trọng số.
    /// Failure ngay nếu không có chiêu nào dùng được (để cây rơi xuống nhánh đuổi theo).
    /// Running trong lúc AbilityRunner đang chạy chiêu; Success khi chiêu hoàn tất; hủy thì hủy luôn chiêu.
    /// </summary>
    public sealed class UseBossAbilityAction : ActionNode
    {
        private readonly BossAbilityEntry[] _entries;
        private readonly List<BossAbilityEntry> _candidates = new List<BossAbilityEntry>();

        private AbilityDefinition _last;
        private bool _started;

        public UseBossAbilityAction(BossAbilityEntry[] entries)
        {
            _entries = entries ?? new BossAbilityEntry[0];
        }

        protected override void OnEnter(BTContext ctx)
        {
            _started = false;

            AbilityRunner runner = ctx.Get<AbilityRunner>();
            Blackboard bb = ctx.Blackboard;
            if (runner == null || !bb.HasTarget) return;

            _candidates.Clear();
            bool hasOtherThanLast = false;

            for (int i = 0; i < _entries.Length; i++)
            {
                BossAbilityEntry entry = _entries[i];
                if (entry == null || entry.ability == null) continue;
                if (bb.Phase < entry.minPhase) continue;
                if (!runner.IsReady(entry.ability)) continue;
                if (!runner.InRange(entry.ability, bb.Target)) continue;

                _candidates.Add(entry);
                if (entry.ability != _last) hasOtherThanLast = true;
            }

            BossAbilityEntry chosen = Pick(hasOtherThanLast);
            if (chosen == null) return;

            _started = runner.Begin(chosen.ability, bb.Target);
            if (_started) _last = chosen.ability;
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

        private BossAbilityEntry Pick(bool skipLast)
        {
            float total = 0f;
            BossAbilityEntry lastEligible = null;

            for (int i = 0; i < _candidates.Count; i++)
            {
                if (skipLast && _candidates[i].ability == _last) continue;
                total += _candidates[i].weight;
                lastEligible = _candidates[i];
            }

            if (lastEligible == null) return null;

            float roll = Random.value * total;
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (skipLast && _candidates[i].ability == _last) continue;

                roll -= _candidates[i].weight;
                if (roll <= 0f) return _candidates[i];
            }

            return lastEligible;
        }
    }

    /// <summary>
    /// Boss đứng niệm "windup" giây rồi gọi quái xung quanh. Mỗi quái được gán sẵn aggro vào player.
    /// Giữ danh sách quái do chính boss này gọi, không gọi thêm khi đã đủ maxAlive.
    /// Failure ngay nếu đã đủ số quái (không tốn hồi chiêu). Animator: trigger "Summon" nếu có.
    /// </summary>
    public sealed class SummonMinionsAction : ActionNode
    {
        private static readonly int SummonHash = Animator.StringToHash("Summon");

        private readonly GameObject _prefab;
        private readonly int _count;
        private readonly float _radius;
        private readonly float _windup;
        private readonly int _maxAlive;
        private readonly List<GameObject> _alive = new List<GameObject>();

        private bool _started;
        private float _spawnAt;
        private bool _animatorResolved;
        private Animator _animator;
        private bool _hasTrigger;

        public SummonMinionsAction(GameObject prefab, int count, float radius, float windup, int maxAlive)
        {
            _prefab = prefab;
            _count = Mathf.Max(1, count);
            _radius = Mathf.Max(1f, radius);
            _windup = Mathf.Max(0f, windup);
            _maxAlive = Mathf.Max(1, maxAlive);
        }

        protected override void OnEnter(BTContext ctx)
        {
            _alive.RemoveAll(IsGone);

            _started = _prefab != null && _alive.Count < _maxAlive;
            if (!_started) return;

            ctx.Get<IMover>()?.Stop();
            _spawnAt = ctx.Time + _windup;
            TriggerAnimation(ctx);
        }

        protected override NodeStatus OnTick(BTContext ctx)
        {
            if (!_started) return NodeStatus.Failure;
            if (ctx.Time < _spawnAt) return NodeStatus.Running;

            Spawn(ctx.Owner.transform);
            return NodeStatus.Success;
        }

        private void Spawn(Transform boss)
        {
            int toSpawn = Mathf.Min(_count, _maxAlive - _alive.Count);
            if (toSpawn <= 0) return;

            bool hasPlayer = PlayerRegistry.TryGetAlive(out Transform player);
            float startAngle = Random.Range(0f, 360f);
            float step = 360f / toSpawn;

            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, startAngle + step * i, 0f) * Vector3.forward * _radius;
                if (!NavMesh.SamplePosition(boss.position + offset, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;

                GameObject minion = Object.Instantiate(_prefab, hit.position, Quaternion.LookRotation(-offset));
                _alive.Add(minion);

                // Awake/OnEnable của minion đã chạy trong Instantiate nên Blackboard đã sẵn sàng.
                EnemyBrain brain = minion.GetComponent<EnemyBrain>();
                if (hasPlayer && brain != null && brain.Blackboard != null)
                {
                    brain.Blackboard.Target = player;
                    brain.Blackboard.IsAggro = true;
                    brain.Blackboard.DistToTarget = AIMath.FlatDistance(hit.position, player.position);
                }
            }
        }

        private void TriggerAnimation(BTContext ctx)
        {
            if (!_animatorResolved)
            {
                _animatorResolved = true;
                _animator = ctx.Owner.GetComponentInChildren<Animator>();
                _hasTrigger = EnemyAnimatorUtil.HasParameter(_animator, SummonHash, AnimatorControllerParameterType.Trigger);
            }

            if (_hasTrigger) _animator.SetTrigger(SummonHash);
        }

        private static bool IsGone(GameObject minion) => minion == null || !minion.activeInHierarchy;
    }
}
