using System;
using System.Collections.Generic;
using UnityEngine;

// Du lieu 1 lan dung artifact, truyen cho tung Action
public class ArtifactContext
{
    public ArtifactData Data;
    public PlayerRuntime Caster;
    public Transform CasterTransform;
    public Vector3 Origin; // diem xuat phat (castOrigin hoac vi tri nguoi choi)
    public Vector3 AimPoint; // diem tren mat dat duoi con tro chuot (da gioi han maxRange neu targeting = Point)
    public Vector3 AimDirection; // huong ngang, da mormalize
    public PlayerBuffs Buffs;
    public MonoBehaviour Runner; // Dung de chay Coroutine
    public LayerMask EnemyLayer;
    public LayerMask ObstacleLayer;
}

// 1 module hanh vi. Ghep nhieu Action trong ArtifactData.actions de tao artifact moi, khong can viet class
[Serializable]
public abstract class ArtifactAction
{
    public abstract void Execute(ArtifactContext context);
    
    // Cac chi so ma Action nay buff len nguoi choi
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

// Sat thuong = flat + scale x TotalDamage cua nguoi choi. Khong crit.
[Serializable]
public class DamageSpec
{
    public float flat = 10f;
    public float scale = 1f;
    
    public int Compute(float totalDamage) => Mathf.Max(0, Mathf.RoundToInt(flat + scale * totalDamage));
}

// Hieu ung sat thuong theo thoi gian (chay, doc,...) - dung DamageOverTime co san
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