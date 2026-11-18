using UnityEngine;

public static class StatusPresenter
{
    private const float MinVfxLifetime = 1f;
    private const float MaxVfxLifetime = 6f;
    private const float FallbackVfxLifetime = 3f;

    public static void Apply(StatusEffect status, IAttackable target, float runningVfxFadeOut = 1.5f)
    {
        if(status == null || target == null) return;
        
        var mb = target as MonoBehaviour;
        StatusRunningFeedback running = null;

        void OnStatusApplied(IAttackable appliedTarget, IStatusEffect effect)
        {
            if(mb == null) return;

            var statusUi = mb.GetComponentInChildren<StatusEffectUI>();
            if(statusUi != null) statusUi.ShowEffects(effect);
            
            running ??= StatusRunningFeedback.Create(status, mb.transform, runningVfxFadeOut);
            running?.Bind(effect);
        }
        
        status.OnStatusApplied += OnStatusApplied;
        status.Apply(target);
        status.OnStatusApplied -= OnStatusApplied;
        
        if(mb == null) return;
        
        if(status.castVfx != null) SpawnTimedVfx(status.castVfx, mb.transform.position + Vector3.up * 2f, null);
        if(running == null && status.runningVfx != null) SpawnTimedVfx(status.runningVfx, mb.transform.position, mb.transform, FallbackVfxLifetime);
        
        Vector3 soundPosition = mb.transform.position;
        if(status.castSfx != null) AudioService.Instance?.PlaySfx(status.castSfx, soundPosition);
        else if(status.castSound != null) AudioService.Instance?.PlaySfx(status.castSound, soundPosition);
    }

    private static void SpawnTimedVfx(GameObject prefab, Vector3 position, Transform parent, float lifetime = 0f)
    {
        GameObject vfx = parent != null
            ? Object.Instantiate(prefab, parent)
            : Object.Instantiate(prefab, position, Quaternion.identity);
        
        Object.Destroy(vfx, lifetime > 0f ? lifetime : EstimateLifetime(vfx));
    }

    private static float EstimateLifetime(GameObject vfx)
    {
        float longest = 0f;

        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            float life = main.duration + main.startLifetime.constantMax;
            if(life > longest) longest = life;
        }
        
        return longest > 0f ? Mathf.Clamp(longest + 0.25f, MinVfxLifetime, MaxVfxLifetime) : FallbackVfxLifetime;
    }
}