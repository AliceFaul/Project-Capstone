using System;
using System.Collections.Generic;
using UnityEngine;

public class ArtifactContext
{
    public ArtifactData Data;
    public PlayerRuntime Caster;
    public Transform CasterTransform;
    public Vector3 Origin;
    public Vector3 AimPoint;
    public Vector3 AimDirection;
    public PlayerBuffs Buffs;
    public MonoBehaviour Runner;
    public LayerMask EnemyLayer;
    public LayerMask ObstacleLayer;
}

[Serializable]
public abstract class ArtifactAction
{
    public abstract void Execute(ArtifactContext context);
    public virtual IEnumerable<BonusStat> BuffStats { get { yield break; } }
}

[Serializable]
public class StatBuffEntry
{
    public BonusStat stat;
    [Tooltip("Percent: 30 = +30%.")]
    public StatMode mode = StatMode.Percent;
    public float value;
}

[Serializable]
public class DamageSpec
{
    public float flat = 10f;
    public float scale = 1f;
    
    public int Compute(float totalDamage) => Mathf.Max(0, Mathf.RoundToInt(flat + scale * totalDamage));
}

[Serializable]
public class DotEffectSpec
{
    public bool enabled;
    public StatusEffectType type = StatusEffectType.Fire;
    public float damagePerTick = 3f;
    public float duration = 4f;
    public float tickInterval = 1f;
    public AudioClip castSound;
    public AudioClip runningSound; // loop sound while the DoT is active
    public GameObject castVfx;
    public GameObject runningVfx;

    public StatusEffect ToStatusEffect()
    {
        var dot = new DamageOverTime
        {
            statusType = type,
            Duration = duration,
            TickInterval = tickInterval,
            DamagePerTick = Mathf.Max(1, Mathf.RoundToInt(damagePerTick))
        };

        var status = new StatusEffect { castSound = castSound, runningSound = runningSound, castVfx = castVfx, runningVfx = runningVfx };
        status.effects.Add(dot);
        return status;
    }
}

[Serializable]
public class ExplosionSpec
{
    public bool enabled;
    public float radius = 3f;
    public AudioCue sfx;
    [Tooltip("Sat thuong no = sat thuong projectile x he so nay.")]
    public float damageScale = 1f;
    public GameObject vfxPrefab;
    public float vfxLifeTime = 2f;
}