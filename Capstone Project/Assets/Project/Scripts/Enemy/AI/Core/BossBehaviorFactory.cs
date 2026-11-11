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
    ///  1. Guard(IsDisabled)      -> Idle        bị Knockback thì đứng yên
    ///  2. Guard(IsTransitioning) -> Idle        đang chuyển pha thì đứng yên (BossPhaseController)
    ///  3. Summon (tùy chọn)      -> từ pha minPhase, có hồi chiêu, tối đa N quái cùng lúc, không cắt ngang chiêu đang chạy
    ///  4. UseBossAbility         -> chọn một chiêu theo pha / tầm / hồi chiêu / không lặp lại chiêu vừa dùng
    ///  5. Guard(HasTarget)       -> đuổi tới chaseStopDistance
    ///  6. Về nhà
    ///  7. Idle
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

        public override Node Build(EnemyAIConfig config)
        {
            if (abilities == null || abilities.Length == 0)
            {
                UnityEngine.Debug.LogError(
                    "[BossBehaviorFactory] Chưa có chiêu nào trong 'Abilities'. Boss sẽ chỉ đuổi theo mà không đánh.", this);
            }

            var children = new List<Node>
            {
                new Guard(new IsDisabledCondition(), new IdleAction { Name = "Disabled" }) { Name = "Disabled" },
                new Guard(new IsTransitioningCondition(), new IdleAction { Name = "Transition" }) { Name = "Transition" }
            };

            if (minionPrefab != null)
            {
                children.Add(new Sequence(
                    new Inverter(new AbilityBusyCondition()),
                    new HasTargetCondition(),
                    new PhaseAtLeastCondition(summonMinPhase),
                    new Cooldown(
                        summonCooldown,
                        new SummonMinionsAction(minionPrefab, summonCount, summonRadius, summonWindup, summonMaxAlive)
                        { Name = "SummonAction" }) { Name = "SummonCooldown" }) { Name = "Summon" });
            }

            children.Add(new UseBossAbilityAction(abilities) { Name = "UseBossAbility" });
            children.Add(new Guard(
                new HasTargetCondition(),
                new MoveToTargetAtDistanceAction(chaseStopDistance)) { Name = "Chase" });
            children.Add(new Guard(new NeedsHomeCondition(), new ReturnHomeAction()) { Name = "ReturnHome" });
            children.Add(new IdleAction { Name = "Idle" });

            return new Selector(children.ToArray()) { Name = "BossRoot" };
        }
    }
}
