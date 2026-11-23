using System.Collections.Generic;
using UnityEngine;

public static class ArtifactDamage
{
    private static readonly Collider[] OverlapBuffer = new Collider[64];

    public static void Hit(IAttackable target, int damage, PlayerRuntime caster, DotEffectSpec effect)
    {
        if (!IsValidTarget(target)) return;

        target.TakeDamage(damage, caster);

        // Status AFTER the damage: a target killed by this very hit must not get a DoT, cast VFX and sound.
        if (effect is { enabled: true } && IsAlive(target))
            StatusPresenter.Apply(effect.ToStatusEffect(), target);
    }

    public static int Area(Vector3 center, float radius, int damage, PlayerRuntime caster, LayerMask enemyLayer, DotEffectSpec effect, HashSet<IAttackable> exclude = null)
    {
        var colliders = Physics.OverlapSphereNonAlloc(center, radius, results: OverlapBuffer, enemyLayer);
        var seen = new HashSet<IAttackable>();
        int count = 0;

        for (int i = 0; i < colliders; i++)
        {
            var collider = OverlapBuffer[i];
            var attackable = collider.GetComponentInParent<IAttackable>();

            if (!IsValidTarget(attackable) || !seen.Add(attackable)) continue;
            if (exclude != null && exclude.Contains(attackable)) continue;

            Hit(attackable, damage, caster, effect);
            count++;
        }

        System.Array.Clear(OverlapBuffer, 0, colliders);
        return count;
    }

    // Characters report their health; anything else (props...) counts as alive.
    private static bool IsAlive(IAttackable target) => target is not ICharacterRuntime character || character.Health > 0;

    public static bool IsValidTarget(IAttackable target)
    {
        return target switch
        {
            null => false,
            _ => true
        };
    }
}