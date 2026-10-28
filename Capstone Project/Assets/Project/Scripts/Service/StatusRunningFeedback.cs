using System;
using UnityEngine;

// Keeps running VFX and the optional loop sound of a StatusEffect alive for as long as the
// IStatusEffect(s) it spawned are active (a DoT of 6s -> VFX / sound for 6s), and stops them when the
// status ends, is canceled, is replaced by a newer one of the same type, or its target dies
public sealed class StatusRunningFeedback
{
    private readonly Transform _target;
    private readonly GameObject _vfx;
    private readonly AudioCue _loopCue;
    private readonly AudioClip _loopClip;
    private readonly float _vfxFadeOut;

    private int _loopHandle;
    private int _activeStatuses;
    private bool _stopped;

    public static StatusRunningFeedback Create(StatusEffect effect, Transform target, float vfxFadeOut)
    {
        bool hasVfx = effect.runningVfx != null;
        bool hasLoop = effect.runningSfx != null || effect.runningSound != null;
        if(!hasVfx && !hasLoop) return null;

        return new StatusRunningFeedback(effect, target, vfxFadeOut);
    }

    private StatusRunningFeedback(StatusEffect effect, Transform target, float vfxFadeOut)
    {
        _target = target;
        _loopCue = effect.runningSfx;
        _loopClip = effect.runningSound;
        _vfxFadeOut = vfxFadeOut;

        if (effect.runningVfx == null) return;
        _vfx = UnityEngine.Object.Instantiate(effect.runningVfx, target);
        ForceLooping(_vfx);
    }

    // Ties the lifetime of this feedback to one running status. With several statuses the feedback lasts
    // until the last of them ends.
    public void Bind(IStatusEffect status)
    {
        if(_stopped || status == null) return;
        _activeStatuses++;
        
        if(_loopHandle == 0) StartLoop(status.Duration);

        bool done = false;
        Action handler = null;
        handler = () =>
        {
            if(done) return;
            done = true;

            status.Deactivated -= handler;
            if(--_activeStatuses <= 0) Stop();
        };
        status.Deactivated += handler;
    }

    private void StartLoop(float statusDuration)
    {
        var audio = AudioService.Instance;
        if(audio == null) return;

        // Safety net in case Deactivated never fires: the loop fades out a second after the status should have ended.
        float maxSeconds = statusDuration > 0f ? statusDuration + 1f : 0f;
        
        if(_loopCue != null) _loopHandle = audio.PlayLoop(_loopCue, _target, maxSeconds);
        else if(_loopClip != null) _loopHandle = audio.PlayLoop(_loopClip, _target, maxSeconds);
    }

    private void Stop()
    {
        if(_stopped) return;
        _stopped = true;
        
        if(_loopHandle != 0) AudioService.Instance?.StopLoop(_loopHandle);

        if (_vfx == null) return;
        foreach(var ps in _vfx.GetComponentsInChildren<ParticleSystem>())
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            
        UnityEngine.Object.Destroy(_vfx, _vfxFadeOut);
    }

    // A one-shot looking VFX prefab would end after its first cycle. Systems that autoplay (Play On Awake)
    // are switched to loop; systems used as sub-emitters normally have Play On Awake off and are left alone.
    private static void ForceLooping(GameObject vfx)
    {
        foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            if(main.playOnAwake) main.loop = true;
        }
    }
}