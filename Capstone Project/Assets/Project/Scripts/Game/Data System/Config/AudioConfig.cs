using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Config")]
public class AudioConfig : ScriptableObject, IConfig
{
    // ResourceManager caches assets by asset name.
    public const string AssetKey = "AudioConfig";
    
    [Header("Mixer")]
    public AudioMixer mixer;

    [Tooltip("Exposed parameter names on the mixer.")]
    public string masterParam = "MasterVolume";
    public string musicParam = "MusicVolume";
    public string sfxParam = "SFXVolume";
    
    [Header("Groups")]
    public AudioMixerGroup musicGroup;
    
    [Tooltip("Gameplay SFX. Also the fallback for UI / Ambient when those are empty.")]
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup ambientGroup;

    [Header("Snapshots (optional)")]
    [Tooltip("Do not let snapshots control the exposed volume parameters above, SetFloat override them.")]
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot combatSnapshot;
    public AudioMixerSnapshot pausedSnapshot;
    
    [Header("Music")]
    [Tooltip("Crossfade length (seconds) when a call does not specify one and the track has no override.")]
    [Min(0f)] public float defaultMusicFade = 1f;
    
    [Tooltip("Optional. Starts as base music as soon as AudioService is ready (loading/ login / main menu).")]
    public MusicTrack menuTrack;
    
    [Header("SFX Pool")]
    [Min(4)] public int sfxPoolSize = 20;
    
    [Header("Default volume (used only if PlayerConfig is unavailable)")]
    [Range(0f, 1f)] public float defaultMaster = 1f;
    [Range(0f, 1f)] public float defaultMusic = 0.8f;
    [Range(0f, 1f)] public float defaultSfx = 1f;
    
    [Header("UI cues")]
    public AudioCue uiClick;
    public AudioCue uiHover;
    public AudioCue uiPopupOpen;
    public AudioCue uiPopupClose;
    [Tooltip("Falls back to uiClick when empty.")] public AudioCue uiConfirm;
    [Tooltip("Falls back to uiClick when empty.")] public AudioCue uiCancel;
    public AudioCue uiError;

    [Header("uGUI")]
    [Tooltip("Automatically add UiSound (click / hover sounds) to every Button, Toggle and Dropdown that does not have one.")]
    public bool autoAttachUGuiSounds = true;
    
    public string GetParam(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Master => masterParam,
            AudioChannel.Music => musicParam,
            AudioChannel.Sfx => sfxParam,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
    }

    public float GetDefaultVolume(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Master => defaultMaster,
            AudioChannel.Music => defaultMusic,
            AudioChannel.Sfx => defaultSfx,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
    }

    public AudioMixerGroup GetGroup(SfxBus bus)
    {
        return bus switch
        {
            SfxBus.UI => uiGroup != null ? uiGroup : sfxGroup,
            SfxBus.Ambient => ambientGroup != null ? ambientGroup : sfxGroup,
            SfxBus.Gameplay => sfxGroup,
            _ => throw new ArgumentOutOfRangeException(nameof(bus), bus, null)
        };
    }

    public AudioCue GetUiCue(UiSfx sfx)
    {
        return sfx switch
        {
            UiSfx.Click => uiClick,
            UiSfx.Hover => uiHover,
            UiSfx.PopupOpen => uiPopupOpen,
            UiSfx.PopupClose => uiPopupClose,
            UiSfx.Confirm => uiConfirm,
            UiSfx.Cancel => uiCancel,
            UiSfx.Error => uiError,
            UiSfx.None => null,
            _ => throw new ArgumentOutOfRangeException(nameof(sfx), sfx, null)
        };
    }
}