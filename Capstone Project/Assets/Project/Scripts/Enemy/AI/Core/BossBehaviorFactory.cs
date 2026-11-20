using System.Collections.Generic;
using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;
    using Mhieu.Enemy.Nodes;

    /// <summary>Một chiêu trong kho chiêu của boss.</summary>
    [System.Serializable]
    public class BossAbilityEntry
    {
        public AbilityDefinition ability;
        [Tooltip("Chiêu này chỉ được dùng từ pha này trở đi.")]
        [Min(1)] public int minPhase = 1;
        [Tooltip("Trọng số khi bốc ngẫu nhiên giữa các chiêu dùng được. Càng lớn càng hay dùng.")]
        [Min(0.1f)] public float weight = 1f;
    }

    /// <summary>
    /// Cây hành vi boss, ưu tiên từ trên xuống:
    ///
    /// Root: Selector
    ///  0. Guard(IsDead)          -> Idle        BOSS CHẾT: đứng yên hẳn, hủy mọi hành động đang chạy
    ///  1. Guard(IsDisabled)      -> Idle        bị Knockback thì đứng yên
    ///  2. Guard(IsTransitioning) -> Idle        đang chuyển pha thì đứng yên (BossPhaseController)
    ///  3. Guard(HasTarget)  "Combat"  -> CHỈ BẬT khi đã phát hiện player; mất target thì cả cụm này tắt (hủy chiêu đang chạy)
    ///       Selector:
    ///       3a. Summon (tùy chọn)      -> từ pha minPhase, có hồi chiêu, tối đa N quái cùng lúc, không cắt ngang chiêu đang chạy
    ///       3b. UseCharge (tùy chọn)   -> lao tới player khi đủ pha, hết hồi chiêu, trong tầm lao
    ///       3c. UseBossAbility         -> chọn một chiêu theo pha / tầm / hồi chiêu / không lặp lại chiêu vừa dùng
    ///       3d. Chase                  -> đuổi tới chaseStopDistance
    ///  4. Về nhà
    ///  5. Idle
    ///
    /// Trên EnemyAIConfig của boss, ô Primary Ability vẫn phải gán một chiêu bất kỳ (ví dụ chiêu chém)
    /// vì EnemyBrain kiểm tra ô này; cây boss không dùng nó để chọn chiêu.
    /// </summary>
    [CreateAssetMenu(fileName = "BossBehavior", menuName = "Data/Enemy Behavior/Boss")]
    public class BossBehaviorFactory : EnemyBehaviorFactory
    {
        [Header("Abilities")]
        [SerializeField] private BossAbilityEntry[] abilities;

        [Header("Movement")]
        [Tooltip("Khi chưa có chiêu nào dùng được, đuổi tới khi cách target khoảng này.")]
        [SerializeField, Min(0.5f)] private float chaseStopDistance = 2.5f;

        [Header("Summon (để trống Minion Prefab = tắt)")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField, Min(1)] private int summonCount = 2;
        [SerializeField, Min(1f)] private float summonRadius = 3f;
        [Tooltip("Thời gian đứng niệm trước khi quái xuất hiện.")]
        [SerializeField, Min(0f)] private float summonWindup = 1.2f;
        [SerializeField, Min(1f)] private float summonCooldown = 25f;
        [Tooltip("Số quái do boss gọi được còn sống cùng lúc. Đủ số này thì không gọi thêm.")]
        [SerializeField, Min(1)] private int summonMaxAlive = 4;
        [SerializeField, Min(1)] private int summonMinPhase = 2;

        [Header("Charge (bước 8, để trống = tắt)")]
        [SerializeField] private ChargeDefinition charge;
        [SerializeField, Min(1)] private int chargeMinPhase = 2;

        public override Node Build(EnemyAIConfig config)
        {
            if (abilities == null || abilities.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    "[BossBehaviorFactory] Chưa có chiêu nào trong 'Abilities'. Boss sẽ chỉ đuổi theo mà không đánh.", this);
            }

            // Các nhánh chiến đấu (Summon, Charge, UseBossAbility, Chase) gom vào một cụm "Combat".
            var combat = new List<Node>();

            if (minionPrefab != null)
            {
                combat.Add(new Sequence(
                    new Inverter(new AbilityBusyCondition()),
                    new Inverter(new ChargeBusyCondition()),
                    new HasTargetCondition(),
                    new PhaseAtLeastCondition(summonMinPhase),
                    new Cooldown(
                        summonCooldown,
                        new SummonMinionsAction(minionPrefab, summonCount, summonRadius, summonWindup, summonMaxAlive)
                        { Name = "SummonAction" })
                    { Name = "SummonCooldown" })
                { Name = "Summon" });
            }

            if (charge != null)
                combat.Add(new UseChargeAction(charge, chargeMinPhase) { Name = "UseCharge" });

            combat.Add(new UseBossAbilityAction(abilities) { Name = "UseBossAbility" });
            combat.Add(new MoveToTargetAtDistanceAction(chaseStopDistance) { Name = "Chase" });

            var children = new List<Node>
            {
                // Boss chết: ưu tiên cao nhất, đứng yên và hủy mọi hành động đang chạy.
                new Guard(new IsDeadCondition(), new IdleAction { Name = "Dead" }) { Name = "Dead" },
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                new Guard(new IsTransitioningCondition(), new IdleAction { Name = "Transition" }) { Name = "Transition" },

                // Chỉ chiến đấu khi đã phát hiện player (có target).
                new Guard(new HasTargetCondition(), new Selector(combat.ToArray()) { Name = "CombatBranches" }) { Name = "Combat" },

                new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" },
                new IdleAction { Name = "Idle" }
            };

            return new Selector(children.ToArray()) { Name = "BossRoot" };
        }
    }
}

namespace Mhieu.Enemy.Nodes
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Boss đã chết: state machine ở Dead, hoặc máu đã về 0 (phòng khi state chưa kịp đổi).
    /// Dùng trong BossBehaviorFactory làm điều kiện của nhánh "Dead" (ưu tiên cao nhất).
    /// </summary>
    public sealed class IsDeadCondition : ConditionNode
    {
        private IStateMachine _stateMachine;
        private ICharacterRuntime _runtime;
        private bool _resolved;

        protected override bool Check(BTContext ctx)
        {
            if (!_resolved)
            {
                _resolved = true;
                _stateMachine = ctx.Owner.GetComponent<IStateMachine>();
                _runtime = ctx.Owner.GetComponent<ICharacterRuntime>();
            }

            if (_stateMachine != null && _stateMachine.IsCurrentState(CharacterStateType.Dead)) return true;
            return _runtime != null && _runtime.Health <= 0;
        }
    }
}