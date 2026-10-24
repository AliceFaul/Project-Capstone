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

    private void StartInSlot(MusicTrack track, float duration)
    {
        
    }
}