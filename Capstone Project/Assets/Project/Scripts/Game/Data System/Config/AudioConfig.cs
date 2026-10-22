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
    
    [Header("SFX Pool")]
    [Min(4)] public int sfxPoolSize = 20;
    
    [Header("Default volume (used only if PlayerConfig is unavailable)")]
    [Range(0f, 1f)] public float defaultMaster = 1f;
    [Range(0f, 1f)] public float defaultMusic = 0.8f;
    [Range(0f, 1f)] public float defaultSfx = 1f;
    
    [Header("UI cues")]
    public AudioClip uiClick;
    public AudioClip uiHover;

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
}