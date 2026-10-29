using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class AudioService : MonoBehaviour, IManager, IAudioService
{
    public static AudioService Instance { get; private set; }
    
    public event Action<AudioChannel, float> OnVolumeChanged;

    private AudioConfig _config;
    private PlayerConfig _playerConfig;
    private SfxPlayer _sfx;
    private MusicPlayer _music;
    private MusicDirector _director;
    private bool _initialized;

    private readonly Dictionary<AudioClip, AudioCue> _clipCues = new();

    private readonly HashSet<object> _overlayOwners = new();
    private static readonly Predicate<object> IsDestroyedOwner = owner => owner is UnityEngine.Object unityObject && unityObject == null;
    private bool _combatMix;

    private const float VolumeSaveDelay = 0.75f;
    private bool _volumeDirty;
    private float _lastVolumeChange;
    
    private readonly float[] _volumes = { 1f, 0.8f, 1f };
    
    public bool IsReady => _initialized;
    public MusicTrack CurrentMusic => _music?.Current;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    public async Task<bool> Initialize()
    {
        if(Instance != this) return false;
        if(_initialized) return true;
        
        _config = ResourceManager.Instance != null ? ResourceManager.Instance.GetAsset<AudioConfig>(AudioConfig.AssetKey) : null;

        if (_config == null)
        {
            Debug.LogError($"[AudioService] AudioConfig not found. Add the asset '{AudioConfig.AssetKey} to the 'Startup' group.");
            return false;
        }
        
        if(_config.mixer == null)
            Debug.LogWarning("[AudioService] AudioConfig has no mixer assigned: volume settings will not apply.");
        
        LoadPlayerConfig();

        _sfx = new SfxPlayer(transform, _config);
        _music = new MusicPlayer(transform, _config);
        _director = new MusicDirector(_music);
        
        _initialized = true;
        if(_config.autoAttachUGuiSounds && GetComponent<UiSoundAutoAttach>() == null) gameObject.AddComponent<UiSoundAutoAttach>();
        
        ApplyAllVolumes();

        // Apply once more next frame.
        await Task.Yield();
        if(Instance != this) return false;
        ApplyAllVolumes();
        
        if(_config.menuTrack != null) PlayMusic(_config.menuTrack);
        
        Debug.Log($"[AudioService] Ready (SFX pool = {_config.sfxPoolSize}).");
        return true;
    }

    private void Update()
    {
        _sfx?.Tick();
        _music?.Tick(Time.unscaledDeltaTime);
        _director?.Tick();
        
        if(_overlayOwners.Count > 0 && _overlayOwners.RemoveWhere(IsDestroyedOwner) > 0) RefreshSnapshot(0.25f);
        if(_volumeDirty && Time.unscaledTime - _lastVolumeChange >= VolumeSaveDelay) FlushVolumes();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if(pauseStatus) FlushVolumes();
    }

    private void OnApplicationQuit()
    {
        FlushVolumes();
    }

    private void OnDestroy()
    {
        FlushVolumes();
        if(Instance == this) Instance = null;
        OnVolumeChanged = null;

        foreach (var cue in _clipCues.Values)
        {
            if(cue != null) Destroy(cue);
        }
        _clipCues.Clear();
    }

    public void PlaySfx(AudioCue cue) => _sfx?.Play(cue, false, default, null);
    public void PlaySfx(AudioCue cue, Vector3 position) => _sfx?.Play(cue, true, position, null);

    public void PlaySfx(AudioCue cue, Transform follow)
    {
        if(follow == null) return;
        _sfx?.Play(cue, true, follow.position, follow);
    }

    public void PlaySfx(AudioClip clip, Vector3 position) => PlaySfx(GetClipCue(clip), position);

    // Returns a handle for StopLoop, 0 if nothing started. The loop follows 'follow' and also stops by itself
    // (short fade) if that transform is destroyed / deactivated, or after maxSeconds of game time (0 = no limit).
    public int PlayLoop(AudioCue cue, Transform follow, float maxSeconds = 0f) => _sfx != null && follow != null ? _sfx.PlayLoop(cue, follow, maxSeconds) : 0;
    public int PlayLoop(AudioClip clip, Transform follow, float maxSeconds = 0f) => PlayLoop(GetClipCue(clip), follow, maxSeconds);
    public void StopLoop(int handle, float fade = 0.25f) => _sfx?.StopLoop(handle, fade);

    private AudioCue GetClipCue(AudioClip clip)
    {
        if(clip == null) return null;

        if (!_clipCues.TryGetValue(clip, out AudioCue cue) || cue == null)
        {
            cue = ScriptableObject.CreateInstance<AudioCue>();
            cue.name = $"AdHoc_{clip.name}";
            cue.hideFlags = HideFlags.HideAndDontSave;
            cue.clips = new[] { clip };
            _clipCues[clip] = cue;
        }

        return cue;
    }

    public void PlayUiClick() => PlayUi(UiSfx.Click);
    public void PlayUiHover() => PlayUi(UiSfx.Hover);

    // Common UI sounds (popup open/close, confirm, cancel, error...) assigned through AudioConfig.
    public void PlayUi(UiSfx sfx)
    {
        if(_config == null || sfx == UiSfx.None) return;
        var cue = _config.GetUiCue(sfx);
        if(cue != null) PlaySfx(cue);
    }
    
    public void PlayMusic(MusicTrack track, float fade = -1f) => _director?.SetBase(track, fade);
    public void PushMusic(MusicTrack track, MusicPriority priority, object owner, float fade = -1f) => _director?.Push(track, priority, owner, fade);
    public void PopMusic(object owner, float fade = -1f) => _director?.Pop(owner, fade);
    public void StopMusic(float fade = 1f) => _director?.Clear(fade);

    public void SetVolume(AudioChannel channel, float value01)
    {
        float v = Mathf.Clamp01(value01);
        _volumes[(int)channel] = v;
 
        // PlayerConfig stays the single source of truth for what gets saved.
        if (_playerConfig != null)
        {
            switch (channel)
            {
                case AudioChannel.Master: _playerConfig.masterVolume = v; break;
                case AudioChannel.Music: _playerConfig.musicVolume = v; break;
                case AudioChannel.Sfx: _playerConfig.sfxVolume = v; break;
            }
        }
 
        ApplyVolume(channel);

        if (_playerConfig != null)
        {
            _volumeDirty = true;
            _lastVolumeChange = Time.unscaledTime;
        }
        
        OnVolumeChanged?.Invoke(channel, v);
    }

    public float GetVolume(AudioChannel channel) => _volumes[(int)channel];
    
    public void RefreshVolumesFromConfig()
    {
        ReadVolumesFromPlayerConfig();
        ApplyAllVolumes();
    }
 
    private void LoadPlayerConfig()
    {
        for (int i = 0; i < _volumes.Length; i++)
            _volumes[i] = _config.GetDefaultVolume((AudioChannel)i);
 
        if (StartupProcessor.Instance != null
            && StartupProcessor.Instance.GetService(out ConfigManager configManager)
            && configManager.GetConfig(out _playerConfig))
        {
            ReadVolumesFromPlayerConfig();
        }
        else
        {
            Debug.LogWarning("[AudioService] PlayerConfig not available, using default volumes from AudioConfig.");
        }
    }
    
    private void ReadVolumesFromPlayerConfig()
    {
        if(_playerConfig == null) return;
 
        _volumes[(int)AudioChannel.Master] = Mathf.Clamp01(_playerConfig.masterVolume);
        _volumes[(int)AudioChannel.Music] = Mathf.Clamp01(_playerConfig.musicVolume);
        _volumes[(int)AudioChannel.Sfx] = Mathf.Clamp01(_playerConfig.sfxVolume);
    }
    
    private void ApplyAllVolumes()
    {
        if(_config == null || _config.mixer == null) return;
 
        foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
        {
            if (!ApplyVolume(channel))
                Debug.LogWarning($"[AudioService] Mixer parameter '{_config.GetParam(channel)}' is not exposed.");
        }
    }

    private bool ApplyVolume(AudioChannel channel)
    {
        if (_config == null || _config.mixer == null) return false;
        return _config.mixer.SetFloat(_config.GetParam(channel), ToDecibel(_volumes[(int)channel]));
    }
 
    // Slider 0..1 -> dB. 0 maps to -80 dB (silence).
    private static float ToDecibel(float linear) => Mathf.Log10(Mathf.Max(0.0001f, linear)) * 20f;

    public void SetCombatMix(bool inCombat, float transitionSeconds = 0.5f)
    {
        _combatMix = inCombat;
        RefreshSnapshot(transitionSeconds);
    }

    public void SetOverlay(object owner, bool open, float transitionSeconds = 0.25f)
    {
        if(owner == null) return;
        bool changed = open ? _overlayOwners.Add(owner) : _overlayOwners.Remove(owner);
        if(changed) RefreshSnapshot(transitionSeconds);
    }

    private void RefreshSnapshot(float transitionSeconds)
    {
        if(_config == null) return;

        AudioMixerSnapshot snapshot = null;
        if(_overlayOwners.Count > 0) snapshot = _config.pausedSnapshot;
        if(snapshot == null && _combatMix) snapshot = _config.combatSnapshot;
        if(snapshot == null) snapshot = _config.normalSnapshot;
        
        if(snapshot != null) snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
    }

    private void FlushVolumes()
    {
        if(!_volumeDirty) return;
        _volumeDirty = false;
        
        if(_playerConfig != null) LocalConfigWriter.Save(_playerConfig);
    }
}

public static class LocalConfigWriter
{
    public static bool Save(ScriptableObject config)
    {
        if (config == null) return false;
 
        try
        {
            string path = Path.Combine(Application.persistentDataPath, config.name + ".json");
            string tempPath = path + ".tmp";
 
            // Write to a temp file first so a crash in the middle never leaves a half-written config behind.
            File.WriteAllText(tempPath, JsonUtility.ToJson(config, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalConfigWriter] Could not save {config.name}: {e.Message}");
            return false;
        }
    }
}