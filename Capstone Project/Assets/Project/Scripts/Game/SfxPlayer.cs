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
    }

    private class CueState
    {
        public float lastPlayTime = float.NegativeInfinity;
        public int activeCount;
        public int lastIndex = -1;
    }

    private readonly AudioConfig _config;
    private readonly Voice[] _voices;
    private readonly Dictionary<AudioCue, CueState> _states = new();

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

    public void Play(AudioCue cue, bool positional, Vector3 position, Transform follow)
    {
        if(cue == null || !cue.HasClips) return;
        
        CueState state = GetState(cue);
        float now = Time.unscaledTime;
        
        if(now - state.lastPlayTime < cue.cooldown) return;
        if(state.activeCount >= cue.maxInstances) return;

        AudioClip clip = cue.Pick(ref state.lastIndex);
        if(clip == null) return;

        Voice voice = AcquireVoice(cue);
        if(voice == null) return;
        
        AudioSource src = voice.source;
        src.outputAudioMixerGroup = _config.GetGroup(cue.bus);
        src.clip = clip;
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
        
        state.lastPlayTime = now;
        state.activeCount++;
        
        src.Play();
    }

    // Call every frame: frees finished voices and moves voices that follow a transform
    public void Tick()
    {
        for (int i = 0; i < _voices.Length; i++)
        {
            Voice v = _voices[i];
            if(!v.active) continue;

            if (!v.source.isPlaying)
            {
                Release(v);
                continue;
            }

            if(v.follow == null) continue;
            // Enemy deactivated / destroyed: let the sound finish where it was.
            if(v.follow.gameObject.activeInHierarchy) v.source.transform.position = v.follow.position;
            else v.follow = null;
        }
    }

    public void StopAll()
    {
        for(int i = 0; i < _voices.Length; i++) Release(_voices[i]);
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
        
        if(v.cue != null && _states.TryGetValue(v.cue, out CueState state) && state.activeCount > 0)
            state.activeCount--;

        v.cue = null;
        v.follow = null;
        v.active = false;
    }

    private CueState GetState(AudioCue cue)
    {
        if(_states.TryGetValue(cue, out CueState state)) return state;
        
        state = new CueState();
        _states[cue] = state;
        return state;
    }
}