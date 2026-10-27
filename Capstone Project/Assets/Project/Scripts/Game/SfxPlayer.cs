using System.Collections.Generic;
using UnityEngine;

public class SfxPlayer
{
    private class Voice
    {
        public AudioSource source;
        public AudioCue cue;
        public Transform follow;
        public float startTime;
        public bool active;
        
        // Loop voices only
        public bool loop;
        public int loopId;
        public float loopEndTime;
        public float baseVolume;
        public bool stopping;
        public float stopStart;
        public float stopDuration;
    }

    private class CueState
    {
        public float lastPlayTime = float.NegativeInfinity;
        public int activeCount;
        public int lastIndex = -1;
    }

    private const float TargetGoneFade = 0.2f;

    private readonly AudioConfig _config;
    private readonly Voice[] _voices;
    private readonly Dictionary<AudioCue, CueState> _states = new();
    private int _nextLoopId = 1;

    public SfxPlayer(Transform parent, AudioConfig config)
    {
        _config = config;

        var poolRoot = new GameObject("SFX Pool");
        poolRoot.transform.SetParent(parent, false);

        _voices = new Voice[Mathf.Max(1, config.sfxPoolSize)];
        for (int i = 0; i < _voices.Length; i++)
        {
            var go = new GameObject($"Sfx_{i}");
            go.transform.SetParent(poolRoot.transform, false);
            
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.rolloffMode = AudioRolloffMode.Linear;

            _voices[i] = new Voice { source = src };
        }
    }

    public void Play(AudioCue cue, bool positional, Vector3 position, Transform follow) => Begin(cue, positional, position, follow, false, 0f);

    public int PlayLoop(AudioCue cue, Transform follow, float maxSeconds)
    {
        if(follow == null) return 0;
        Voice voice = Begin(cue, true, follow.position, follow, true, maxSeconds);
        return voice?.loopId ?? 0;
    }

    public void StopLoop(int handle, float fade)
    {
        if (handle <= 0) return;
 
        for (int i = 0; i < _voices.Length; i++)
        {
            Voice v = _voices[i];
            if (v.active && v.loop && v.loopId == handle)
            {
                BeginStop(v, fade);
                return;
            }
        }
    }

    // Call every frame: frees finished voices and moves voices that follow a transform
    public void Tick()
    {
        float now = Time.unscaledTime;
 
        for (int i = 0; i < _voices.Length; i++)
        {
            Voice v = _voices[i];
            if(!v.active) continue;
 
            if (v.loop)
            {
                TickLoop(v, now);
                continue;
            }
 
            if (!v.source.isPlaying)
            {
                Release(v);
                continue;
            }

            if (v.follow == null) continue;
            
            // One-shot whose target was deactivated / destroyed: let the sound finish where it was.
            if(v.follow.gameObject.activeInHierarchy) v.source.transform.position = v.follow.position;
            else v.follow = null;
        }
    }

    public void StopAll()
    {
        for(int i = 0; i < _voices.Length; i++) Release(_voices[i]);
    }

    private Voice Begin(AudioCue cue, bool positional, Vector3 position, Transform follow, bool loop, float maxSeconds)
    {
        if(cue == null || !cue.HasClips) return null;
        
        CueState state = GetState(cue);
        float now = Time.unscaledTime;
        
        // The short anti-spam cooldown only makes sense for one-shots.
        if(!loop && now - state.lastPlayTime < cue.cooldown) return null;
        if(state.activeCount >= cue.maxInstances) return null;
        
        AudioClip clip = cue.Pick(ref state.lastIndex);
        if(clip == null) return null;
        
        Voice voice = AcquireVoice(cue);
        if(voice == null) return null;
        
        AudioSource src = voice.source;
        src.outputAudioMixerGroup = _config.GetGroup(cue.bus);
        src.clip = clip;
        src.loop = loop;
        src.volume = Mathf.Clamp01(cue.volume * Random.Range(cue.volumeVariance.x, cue.volumeVariance.y));
        src.pitch = Random.Range(cue.pitchRange.x, cue.pitchRange.y);
        src.priority = cue.priority;
        src.spatialBlend = positional ? cue.spatialBlend : 0f;
        src.minDistance = cue.minDistance;
        src.maxDistance = cue.maxDistance;
        
        if(positional) src.transform.position = follow != null ? follow.position : position;
 
        voice.cue = cue;
        voice.follow = follow;
        voice.startTime = now;
        voice.active = true;
 
        voice.loop = loop;
        voice.loopId = loop ? _nextLoopId++ : 0;
        voice.loopEndTime = loop && maxSeconds > 0f ? Time.time + maxSeconds : 0f;
        voice.baseVolume = src.volume;
        voice.stopping = false;
 
        state.lastPlayTime = now;
        state.activeCount++;
 
        src.Play();
        return voice;
    }

    private void TickLoop(Voice v, float now)
    {
        bool followAlive = v.follow != null && v.follow.gameObject.activeInHierarchy;
        if(followAlive) v.source.transform.position = v.follow.position;

        if (v.stopping)
        {
            float t = v.stopDuration > 0f ? (now - v.stopStart) / v.stopDuration : 1f;
            if(t >= 1f) Release(v);
            else v.source.volume = v.baseVolume * (1f - t);
            return;
        }

        bool expired = v.loopEndTime > 0f && Time.time >= v.loopEndTime;
        if(!followAlive || expired) BeginStop(v, TargetGoneFade);
    }

    private void BeginStop(Voice v, float fade)
    {
        if(v.stopping) return;

        if (fade <= 0f)
        {
            Release(v);
            return;
        }
        
        v.stopping = true;
        v.stopStart = Time.unscaledTime;
        v.stopDuration = fade;
    }

    private Voice AcquireVoice(AudioCue cue)
    {
        Voice victim = null;
 
        for (int i = 0; i < _voices.Length; i++)
        {
            Voice v = _voices[i];
            if(!v.active) return v;
 
            // Candidate to steal: least important (highest priority number), then oldest.
            if(victim == null
                || v.cue.priority > victim.cue.priority
                || (v.cue.priority == victim.cue.priority && v.startTime < victim.startTime))
            {
                victim = v;
            }
        }
 
        // Pool is full: only steal from a voice that is not more important than the new cue.
        if (victim == null || victim.cue.priority < cue.priority) return null;
        Release(victim);
        return victim;
    }

    private void Release(Voice v)
    {
        if(!v.active) return;
        
        v.source.Stop();
        v.source.clip = null;
        v.source.loop = false;
        
        if(v.cue != null && _states.TryGetValue(v.cue, out CueState state) && state.activeCount > 0)
            state.activeCount--;

        v.cue = null;
        v.follow = null;
        v.active = false;
        v.loop = false;
        v.loopId = 0;
        v.loopEndTime = 0f;
        v.stopping = false;
    }

    private CueState GetState(AudioCue cue)
    {
        if(_states.TryGetValue(cue, out CueState state)) return state;
        
        state = new CueState();
        _states[cue] = state;
        return state;
    }
}