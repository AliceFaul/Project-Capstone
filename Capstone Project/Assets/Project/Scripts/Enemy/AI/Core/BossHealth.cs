
using UnityEngine;

namespace Mhieu.Enemy
{
    /// <summary>
    /// BossHealth kế thừa EnemyHealth.
    /// Bổ sung cơ chế bất tử trong lúc chuyển pha.
    /// </summary>
    public class BossHealth : EnemyHealth
    {
        public bool IsInvulnerable { get; set; }

        // Chặn sát thương qua lời gọi có CharacterRuntime.
        public new void TakeDamage(float damage, ICharacterRuntime runtime)
        {
            if (IsInvulnerable)
                return;

            TakeDamage(damage);
        }

        // Chặn sát thương qua lời gọi TakeDamage(float).
        public new void TakeDamage(float damage)
        {
            if (IsInvulnerable)
                return;

            if (damage <= 0f)
                return;

            if (currentHealth <= 0f)
                return;

            base.TakeDamage(damage);
        }
    }
}

