using ImprovedTimers;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class StatusEffect
{
    
    public AudioClip castSound;
    public GameObject castVfx;
    public GameObject runningVfx;

    [SerializeReference, SerializeReferenceDropdown]
    public List<IEffect<IAttackable>> effects = new();

    public event Action<IAttackable, IStatusEffect> OnStatusApplied;

    public void Apply(IAttackable target)
    {
        if (target == null || effects == null)
            return;

        foreach (var template in effects)
        {
            if (template == null)
                continue;

            
            var effect = template.Clone();

            if (effect == null)
                continue;

            
            effect.Apply(target);

           
            if (effect is IStatusEffect statusEffect)
            {
                OnStatusApplied?.Invoke(target, statusEffect);
            }
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
        if (target == null)
            return;

        target.TakeDamage(damageAmount);

        OnCompleted?.Invoke(this);
    }

    public void Cancel()
    {
        
    }

    public IEffect<IAttackable> Clone()
    {
        return new DamageEffect
        {
            damageAmount = damageAmount
        };
    }
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

    
    public event Action Activated;
    public event Action<float, float> Progressed;
    public event Action Deactivated;

    private IntervalTimer _timer;
    private IAttackable _currentTarget;
    private float _elapsed;

    public void Apply(IAttackable target)
    {
        if (target == null)
            return;

        
        _timer?.Stop();

        _currentTarget = target;
        _elapsed = 0f;

        float safeDuration = Mathf.Max(0.01f, Duration);
        float safeInterval = Mathf.Max(0.01f, TickInterval);

        _timer = new IntervalTimer(safeDuration, safeInterval);
        _timer.OnInterval = OnInterval;
        _timer.OnTimerStop = OnStop;
        _timer.Start();

        Activated?.Invoke();
        Progressed?.Invoke(0f, safeDuration);
    }

    private void OnInterval()
    {
        if (_currentTarget == null)
        {
            Cancel();
            return;
        }

        _currentTarget.TakeDamage(DamagePerTick);

        _elapsed += Mathf.Max(0.01f, TickInterval);

        Progressed?.Invoke(
            Mathf.Clamp(_elapsed, 0f, Duration),
            Duration
        );
    }

    private void OnStop()
    {
        CleanUp();
    }

    public void Cancel()
    {
        if (_timer != null)
        {
            _timer.Stop();
        }
        else
        {
            CleanUp();
        }
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
