using System;
using UnityEngine;

public interface IAudioService
{
    bool IsReady { get; }

    // Raised after SetVolume. A save service can hook this to persist PlayerConfig.
    event Action<AudioChannel, float> OnVolumeChanged;

    void PlaySfx(AudioCue cue);                     // 2D (UI, global feedback)
    void PlaySfx(AudioCue cue, Vector3 position);   // 3D, fire-and-forget at a position
    void PlaySfx(AudioCue cue, Transform follow);   // 3D, follows a transform while it is active
    void PlayUiClick();

    void SetVolume(AudioChannel channel, float value01);
    float GetVolume(AudioChannel channel);
    void SetSnapshot(AudioSnapshotType type, float transitionSeconds = 0.5f);
}