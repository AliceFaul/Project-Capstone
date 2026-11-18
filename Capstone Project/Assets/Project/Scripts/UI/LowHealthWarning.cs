using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LowHealthWarning : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Empty = the PlayerRuntime found in the scene.")]
    [SerializeField] private PlayerRuntime player;

    [Header("Trigger")]
    [Tooltip("The warning starts when health is at or below this fraction of max health.")]
    [SerializeField, Range(0.05f, 0.9f)] private float threshold = 0.3f;
    
    [Tooltip("Health must recover this much above the threshold before the warning stops (avoids flickering).")]
    [Range(0f, 0.3f)] [SerializeField] private float hysteresis = 0.05f;

    [Header("Heartbeat")]
    [Tooltip("Optional 'lub-dub' sound, played on every beat. Empty = visual only.")]
    [SerializeField] private AudioCue heartbeatCue;
    [Tooltip("Seconds between beats right at the threshold.")]
    [SerializeField, Min(0.2f)] private float slowInterval = 1.1f;
    [Tooltip("Seconds between beats at 0 health.")]
    [SerializeField, Min(0.2f)] private float fastInterval = 0.55f;
    [Tooltip("Delay of the second ('dub') pulse after the first.")]
    [SerializeField, Min(0f)] private float secondPulseDelay = 0.2f;

    [Header("Screen effect (URP vignette)")]
    [SerializeField] private bool screenEffect = true;
    [SerializeField] private Color vignetteColor = new Color(0.7f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float vignetteSmoothness = 0.6f;
    
    [Tooltip("Effect strength between beats while health is low.")]
    [SerializeField, Range(0f, 1f)] private float baseStrength = 0.35f;
    
    [Tooltip("Extra strength added on every beat.")]
    [SerializeField, Range(0f, 1f)] private float pulseStrength = 0.65f;
    
    [Tooltip("How fast the pulse fades after a beat (per second).")]
    [SerializeField, Min(0.1f)] private float pulseDecay = 4f;
    
    [Tooltip("How fast the whole effect fades in / out (per second).")]
    [SerializeField, Min(0.1f)] private float fadeSpeed = 3f;
    
    [Tooltip("Layer of the generated Volume. It must be included in the camera's Volume Mask (Default = 0).")]
    [SerializeField] private int volumeLayer = 0;
    [SerializeField] private float volumePriority = 20f;

    private Volume _volume;
    private VolumeProfile _profile;

    private bool _active;
    private float _severity;          // 0 at the threshold -> 1 at 0 health
    private float _presence;          // 0..1 fade of the whole effect
    private float _pulse;             // 0..1, jumps up on every beat and decays
    private float _beatTimer;
    private float _secondPulseTimer;
    private float _nextFindTime;

    private void Awake()
    {
        if(screenEffect) CreateVolume();
    }

    private void OnDestroy()
    {
        if(_profile != null) Destroy(_profile);
    }

    private void CreateVolume()
    {
        _profile = ScriptableObject.CreateInstance<VolumeProfile>();

        Vignette vignette = _profile.Add<Vignette>(true);
        vignette.color.Override(vignetteColor);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(false);

        var volumeObject = new GameObject("LowHealthVolume");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.layer = volumeLayer;

        _volume = volumeObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = volumePriority;
        _volume.weight = 0f;
        _volume.sharedProfile = _profile;
    }

    private void Update()
    {
        if (player == null)
        {
            // The player may be spawned after this object: look again twice a second.
            if (Time.unscaledTime >= _nextFindTime)
            {
                _nextFindTime = Time.unscaledTime + 0.5f;
                player = FindFirstObjectByType<PlayerRuntime>();
            }

            if(player == null) return;
        }

        float dt = Time.deltaTime;

        UpdateState();
        UpdateHeartbeat(dt);
        UpdateScreenEffect(dt);
    }

    private void UpdateState()
    {
        int maxHealth = player.TotalHealth;
        float fraction = maxHealth > 0 ? player.Health / (float)maxHealth : 1f;
        bool dead = player.Health <= 0;

        if(!_active && !dead && fraction <= threshold) _active = true;
        else if(_active && (dead || fraction >= threshold + hysteresis)) _active = false;

        _severity = _active ? Mathf.Clamp01(Mathf.InverseLerp(threshold, 0f, fraction)) : 0f;
    }

    private void UpdateHeartbeat(float dt)
    {
        if (!_active)
        {
            _beatTimer = 0f; // the first beat plays right away the next time the warning starts
            _secondPulseTimer = 0f;
            return;
        }

        if (dt <= 0f) return; // paused

        _beatTimer -= dt;
        if (_beatTimer <= 0f)
        {
            _beatTimer = Mathf.Lerp(slowInterval, fastInterval, _severity);
            _pulse = 1f;
            _secondPulseTimer = secondPulseDelay;
            if(heartbeatCue != null) AudioService.Instance?.PlaySfx(heartbeatCue);
        }

        if (_secondPulseTimer > 0f)
        {
            _secondPulseTimer -= dt;
            if(_secondPulseTimer <= 0f) _pulse = Mathf.Max(_pulse, 0.7f);
        }
    }

    private void UpdateScreenEffect(float dt)
    {
        _pulse = Mathf.Max(0f, _pulse - pulseDecay * dt);

        if(_volume == null) return;
        _presence = Mathf.MoveTowards(_presence, _active ? 1f : 0f, fadeSpeed * dt);
        _volume.weight = _presence * Mathf.Clamp01(baseStrength + pulseStrength * _pulse);
    }
}