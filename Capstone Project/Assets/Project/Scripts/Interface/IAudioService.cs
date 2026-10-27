using System;
using UnityEngine;

public interface IAudioService
{
    bool IsReady { get; }

    // Raised after SetVolume. A save service can hook this to persist PlayerConfig.
    event Action<AudioChannel, float> OnVolumeChanged;

    // ===== SFX =====
    void PlaySfx(AudioCue cue);                     // 2D (UI, global feedback)
    void PlaySfx(AudioCue cue, Vector3 position);   // 3D, fire-and-forget at a position
    void PlaySfx(AudioCue cue, Transform follow);   // 3D, follows a transform while it is active
    void PlaySfx(AudioClip clip, Vector3 position); // 3D at a position, for legacy raw clips. Prefer AudioCue.
    void PlayUiClick();
    void PlayUiHover();

    int PlayLoop(AudioCue cue, Transform follow, float maxSeconds = 0f);
    int PlayLoop(AudioClip clip, Transform follow, float maxSeconds = 0f);
    void StopLoop(int handle, float fade = 0.25f);
    
    // ===== MUSIC =====
    MusicTrack CurrentMusic { get; }
    void PlayMusic(MusicTrack track, float fade = -1f);
    void PushMusic(MusicTrack track, MusicPriority priority, object owner, float fade = -1f);
    void PopMusic(object owner, float fade = -1f);
    void StopMusic(float fade = 1f);

    // ===== MIXER =====
    void SetVolume(AudioChannel channel, float value01);
    float GetVolume(AudioChannel channel);
    void SetSnapshot(AudioSnapshotType type, float transitionSeconds = 0.5f);
}