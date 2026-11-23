using UnityEngine;

namespace Mhieu.Enemy
{
    using Mhieu.AI.BT;

    /// <summary>
    /// Cảm biến phát hiện player: chỉ đọc thế giới và GHI vào Blackboard, không ra quyết định hành vi.
    ///
    /// Quy tắc:
    ///  - Phát hiện: player trong aggroRadius (và thấy được nếu bật requireLineOfSight).
    ///  - Bỏ đuổi: player quá xa enemy (loseRadius) hoặc enemy rời nhà quá xa (leashDistance)
    ///    -> IsReturningHome = true cho tới khi về tới nhà (chống giật qua lại ở rìa leash).
    ///  - Player chết: bỏ aggro.
    /// Việc bị đánh thì aggro được xử lý ở EnemyBrain (HandleHit).
    /// </summary>
    public sealed class TargetSensor
    {
        private readonly Transform _self;
        private readonly EnemyAIConfig _config;

        public TargetSensor(Transform self, EnemyAIConfig config)
        {
            _self = self;
            _config = config;
        }

        public void Sense(Blackboard bb)
        {
            float homeDistance = AIMath.FlatDistance(_self.position, bb.HomePosition);

            if (bb.IsReturningHome && homeDistance <= _config.homeArriveDistance)
                bb.IsReturningHome = false;

            if (!PlayerRegistry.TryGetAlive(out Transform player))
            {
                if (bb.IsAggro) bb.ClearTarget();
                return;
            }

            float distance = AIMath.FlatDistance(_self.position, player.position);

            if (bb.IsAggro)
            {
                bb.Target = player;
                bb.DistToTarget = distance;

                if (distance > _config.loseRadius || homeDistance > _config.leashDistance)
                {
                    bb.ClearTarget();
                    bb.IsReturningHome = true;
                }

                return;
            }

            if (bb.IsReturningHome) return;

            if (distance <= _config.aggroRadius && HasLineOfSight(player))
            {
                bb.Target = player;
                bb.IsAggro = true;
                bb.DistToTarget = distance;
            }
        }

        private bool HasLineOfSight(Transform player)
        {
            if (!_config.requireLineOfSight) return true;

            Vector3 from = _self.position + Vector3.up * _config.eyeHeight;
            Vector3 to = player.position + Vector3.up * _config.eyeHeight;
            return !Physics.Linecast(from, to, _config.lineOfSightBlockers, QueryTriggerInteraction.Ignore);
        }
    }
}
