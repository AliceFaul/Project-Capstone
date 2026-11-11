using System.Collections.Generic;
using UnityEngine;

// Gay sat thuong / ap hieu ung len enemy cho artifact (Area strike, Projectile, Explosion)
public static class ArtifactDamage
{
    private static readonly Collider[] OverlapBuffer = new Collider[64];
    
    public static void Hit(IAttackable target, int damage, PlayerRuntime caster, DotEffectSpec effect)
    {
        if(!IsValidTarget(target)) return;
        if(effect is { enabled: true }) ApplyStatus(effect.ToStatusEffect(), target);
        target.TakeDamage(damage, caster);
    }

    // Sat thuong dang cau tai 'center'. 'exclude': quai da bi trung truoc do (vi du vu no khong tinh lai muc tieu chinh)
    public static int Area(Vector3 center, float radius, int damage, PlayerRuntime caster, LayerMask enemyLayer, DotEffectSpec effect, HashSet<IAttackable> exclude = null)
    {
        var colliders = Physics.OverlapSphereNonAlloc(center, radius, results: OverlapBuffer, enemyLayer);
        var seen = new HashSet<IAttackable>();
        int count = 0;

        for (int i = 0; i < colliders; i++)
        {
            var collider = OverlapBuffer[i];
            var attackable = collider.GetComponentInParent<IAttackable>();
            
            if(!IsValidTarget(attackable) || !seen.Add(attackable)) continue;
            if(exclude != null && exclude.Contains(attackable)) continue;
            
            Hit(attackable, damage, caster, effect);
            count++;
        }
        
        System.Array.Clear(OverlapBuffer, 0, colliders);
        return count;
    }

    private static void ApplyStatus(StatusEffect status, IAttackable target)
    {
        status.OnStatusApplied += ShowStatusIcon;
        status.Apply(target);

        if (target is MonoBehaviour mb && mb)
        {
            if(status.castVfx) 
                Object.Instantiate(status.castVfx, mb.transform.position + Vector3.up * 2f, Quaternion.identity);

            if (status.runningVfx)
            {
                var running = Object.Instantiate(status.runningVfx, mb.transform);
                float duration = 3f;
                
                foreach (var eff in status.effects)
                {
                    if (eff is IStatusEffect statusEff)
                    {
                        duration = statusEff.Duration;
                        break;
                    }
                }
                
                Object.Destroy(running, duration);
            }
        }
    }

    private static void ShowStatusIcon(IAttackable target, IStatusEffect effect)
    {
        if (target is MonoBehaviour mb && mb)
        {
            mb.GetComponentInChildren<StatusEffectUI>()?.ShowEffects(effect);
        }
    }

    public static bool IsValidTarget(IAttackable target)
    {
        return target switch
        {
            null => false,
            Object obj => obj != null,
            _ => true
        };
    }
}