using ImprovedTimers;
using UnityEngine;
using System.Collections.Generic;
using System;
using Mhieu.Enemy;

[Serializable]
public class StatusEffect
{
    // VFX & SFX
    public AudioClip castSound;
    public GameObject castVfx;
    public GameObject runningVfx;
    
    [SerializeReference, SerializeReferenceDropdown] 
    public List<IEffect<IAttackable>> effects = new();

    public event Action<IAttackable, IStatusEffect> OnStatusApplied;

    public void Apply(IAttackable target)
    {
        foreach (var template in effects)
        {
            var effect = template.Clone();
            
            if (target is EnemyHealth enemy)
            {
                enemy.ApplyEffect(effect);
            }
            else
            {
                effect.Apply(target);
            }
            
            if(effect is IStatusEffect statusEffect)
                OnStatusApplied?.Invoke(target, statusEffect);
        }
    }
}

public interface IEffect<TTarget>
{
    void Apply(TTarget target);
    void Cancel();
    event Action<IEffect<IAttackable>> OnCompleted;
    IEffect<TTarget> Clone();
}

public interface IStatusEffect 
{
    StatusEffectType StatusType { get; }
    float Duration { get; }

    event Action Activated;
    event Action<float, float> Progressed;
    event Action Deactivated;
}

[Serializable]
public class DamageEffect : IEffect<IAttackable>
{
    public int damageAmount = 10;
    public event Action<IEffect<IAttackable>> OnCompleted;
    
    public void Apply(IAttackable target)
    {
        target.TakeDamage(damageAmount);
        OnCompleted?.Invoke(this);
        
    }

    public void Cancel()
    {
        // none
    }
    
    public IEffect<IAttackable> Clone() => new DamageEffect { damageAmount = damageAmount };
}

[Serializable]
public class DamageOverTime : IEffect<IAttackable>, IStatusEffect
{
    public StatusEffectType statusType = StatusEffectType.Poison;
    public float Duration = 5f;
    public float TickInterval = 1f;
    public int DamagePerTick;
    public event Action<IEffect<IAttackable>> OnCompleted;

    public StatusEffectType StatusType => statusType;
    float IStatusEffect.Duration => Duration;
    
    // UI event-driven
    public event Action Activated;
    public event Action<float, float> Progressed;
    public event Action Deactivated;
    
    private IntervalTimer _timer;
    private IAttackable _currentTarget;
    private float _elapsed;
    
    public void Apply(IAttackable target)
    {
        _currentTarget = target;
        _elapsed = 0f;
        _timer = new IntervalTimer(Duration, TickInterval);
        _timer.OnInterval = OnInterval;
        _timer.OnTimerStop = OnStop;
        _timer.Start();
        
        Activated?.Invoke();
        Progressed?.Invoke(0f, Duration);
    }

    private void OnInterval()
    {
        _currentTarget?.TakeDamage(DamagePerTick);
        _elapsed += TickInterval;
        Progressed?.Invoke(Mathf.Clamp(_elapsed, 0f, Duration), Duration);
    }

    private void OnStop() => CleanUp();

    public void Cancel()
    {
        _timer?.Stop();
        CleanUp();
    }

    private void CleanUp()
    {
        _timer = null;
        _currentTarget = null;
        Deactivated?.Invoke();
        OnCompleted?.Invoke(this);
    }
    
    public IEffect<IAttackable> Clone()
    {
        return new DamageOverTime
        {
            statusType = statusType,
            Duration = Duration,
            TickInterval = TickInterval,
            DamagePerTick = DamagePerTick
        };
    }
}