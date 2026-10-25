using System;
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
    }

    private void OnDestroy()
    {
        if(Instance == this) Instance = null;
        OnVolumeChanged = null;
    }

    public void PlaySfx(AudioCue cue) => _sfx?.Play(cue, false, default, null);
    public void PlaySfx(AudioCue cue, Vector3 position) => _sfx?.Play(cue, true, position, null);

    public void PlaySfx(AudioCue cue, Transform follow)
    {
        if(follow == null) return;
        _sfx?.Play(cue, true, follow.position, follow);
    }

    public void PlayUiClick()
    {
        if(_config == null) return;
        PlaySfx(_config.uiClick);
    }

    public void PlayUIHover()
    {
        if(_config == null) return;
        PlaySfx(_config.uiHover);
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
 
    public void SetSnapshot(AudioSnapshotType type, float transitionSeconds = 0.5f)
    {
        if (_config == null) return;
 
        AudioMixerSnapshot snapshot;
        switch (type)
        {
            case AudioSnapshotType.Combat: snapshot = _config.combatSnapshot; break;
            case AudioSnapshotType.Paused: snapshot = _config.pausedSnapshot; break;
            default: snapshot = _config.normalSnapshot; break;
        }
 
        if (snapshot != null) snapshot.TransitionTo(Mathf.Max(0f, transitionSeconds));
    }
}