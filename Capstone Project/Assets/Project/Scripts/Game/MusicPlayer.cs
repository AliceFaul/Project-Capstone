using System.Collections.Generic;
using UnityEngine;

// Plain C# class owned by AudioService, ticked from AudioService.Update with unscaled delta time
// so hit-stop (Time.timeScale ~ 0) never freezes a crossfade.
// Uses 3 slots instead of 2: if the music changes again while a crossfade is still running,
// the new track gets a free slot instead of cutting an audible one.
public class MusicPlayer
{
    private class Slot
    {
        public AudioSource source;
        public MusicTrack track;
        public float level;         // fade envelope 0..1
        public float target;        // 0 = fading out, 1 = fading in / playing
        public float duration;      // seconds for a full 0..1 move
        public float startedAt;     // unscaled time
    }
    
    private readonly AudioConfig _config;
    private readonly Slot[] _slots;
    private readonly Dictionary<MusicTrack, float> _savedTime = new();
    
    // The track that is supposed to be audible right now (null = silence / fading to silence)
    public MusicTrack Current { get; private set; }

    public MusicPlayer(Transform parent, AudioConfig config)
    {
        _config = config;
        if(config.musicGroup == null) Debug.LogWarning($"[MusicPlayer] AudioConfig.musicGroup is empty: music will ignore the Music volume slider.");

        var root = new GameObject("Music");
        root.transform.SetParent(parent, false);

        _slots = new Slot[3];
        for (int i = 0; i < _slots.Length; i++)
        {
            var go = new GameObject($"Music_{i}");
            go.transform.SetParent(root.transform, false);
            
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.outputAudioMixerGroup = config.musicGroup;

            _slots[i] = new Slot { source = src };
        }
    }

    // fade < 0 -> track.fadeOverride, then AudioConfig.defaultMusicFade.
    public void Play(MusicTrack track, float fade)
    {
        if (track == null || track.clip == null)
        {
            Stop(fade);
            return;
        }
        
        if(Current == track) return;

        float duration = ResolveFade(track, fade);
        
        // Everything else fades out over the same duration (symmetric crossfade).
        foreach (var s in _slots)
        {
            if(s.track != null && s.track != track) FadeOut(s, duration);
        }
        
        // The same track may still be fading out from a moment ago: bring it back instead of restarting it.
        Slot existing = FindSlotWith(track);

        if (existing != null)
        {
            existing.target = 1f;
            existing.duration = duration;
        }
        else
        {
            StartInSlot(track, duration);
        }
        
        Current = track;
    }

    public void Stop(float fade)
    {
        float duration = fade >= 0f ? fade : _config.defaultMusicFade;
        Current = null;

        foreach (var s in _slots)
        {
            if(s.track != null) FadeOut(s, duration);
        }
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            Slot s = _slots[i];
            if(s.track == null) continue;

            if (!Mathf.Approximately(s.level, s.target))
            {
                s.level = s.duration <= 0f
                    ? s.target
                    : Mathf.MoveTowards(s.level, s.target, deltaTime / s.duration);
            }

            if (s.target <= 0f && s.level <= 0f)
            {
                StopSlot(s, saveTime: true);
                continue;
            }
            
            // A non-looping track that finished by itself (guard: isPlaying can lag right after Play()).
            if (!s.source.isPlaying && Time.unscaledTime - s.startedAt > 0.25f)
            {
                if(Current == s.track) Current = null;
                StopSlot(s, saveTime: false);
                continue;
            }
            
            ApplyVolume(s);
        }
    }

    private void StartInSlot(MusicTrack track, float duration)
    {
        Slot slot = FindFreeSlot();

        if (slot == null)
        {
            slot = _slots[0];
            foreach (var s in _slots)
            {
                if(s.level < slot.level) slot = s;
            }
            StopSlot(slot, saveTime: true);
        }
        
        AudioSource src = slot.source;
        src.clip = track.clip;
        src.loop = track.loop;

        if (track.resumeOnReturn && _savedTime.Remove(track, out var savedTime))
            if(savedTime > 0f && savedTime < track.clip.length - 0.25f) src.time = savedTime;
        
        slot.track = track;
        slot.level = duration > 0f ? 0f : 1f;
        slot.target = 1f;
        slot.duration = duration;
        slot.startedAt = Time.unscaledTime;
        
        ApplyVolume(slot);
        src.Play();
    }

    private void FadeOut(Slot slot, float duration)
    {
        slot.target = 0f;
        slot.duration = duration;
    }

    private void StopSlot(Slot slot, bool saveTime)
    {
        if(slot.track != null && saveTime && slot.track.resumeOnReturn && slot.source.isPlaying)
            _savedTime[slot.track] = slot.source.time;
        
        slot.source.Stop();
        slot.source.clip = null;
        slot.track = null;
        slot.level = 0f;
        slot.target = 0f;
    }

    // Equal-power curve: with one slot going 1 -> 0 and another 0 -> 1 the perceived loudness stays constant.
    private static void ApplyVolume(Slot slot)
    {
        slot.source.volume = slot.track.volume * Mathf.Sin(slot.level * Mathf.PI * 0.5f);
    }

    private float ResolveFade(MusicTrack track, float fade)
    {
        if(fade >= 0f) return fade;
        return track.fadeOverride >= 0f ? track.fadeOverride : _config.defaultMusicFade;
    }

    private Slot FindSlotWith(MusicTrack track)
    {
        foreach (var s in _slots)
        {
            if(s.track == track) return s;
        }
        return null;
    }

    private Slot FindFreeSlot()
    {
        foreach (var s in _slots)
        {
            if(s.track == null) return s;
        }
        return null;
    }
}