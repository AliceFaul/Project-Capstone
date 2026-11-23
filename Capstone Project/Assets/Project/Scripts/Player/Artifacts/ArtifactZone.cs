using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;

public class ArtifactZoneSettings
{
    public string buffId;
    public float radius = 4f;
    public float duration = 6f;
    public float tickInterval = 1f;
    public float healFlat;
    public float healPercentOfMax;
    public List<StatBuffEntry> entries = new();
}

public class ArtifactZone : MonoBehaviour
{
    [SerializeField] private Transform indicator;

    [Header("Audio")]
    [Tooltip("Optional. Loops at the zone for as long as it exists (magic hum, fire crackle...).")]
    [SerializeField] private AudioCue loopSfx;
    
    [Tooltip("Optional. Played on every tick that really affects the player (heal / buff), at the player.")]
    [SerializeField] private AudioCue tickSfx;

    // How many zones with the same buffId are alive. A buff must only be removed when the LAST one is gone,
    // otherwise an old zone expiring would strip the buff that a newer overlapping zone is still giving.
    private static readonly Dictionary<string, int> ActiveByBuffId = new Dictionary<string, int>();

    private ArtifactZoneSettings _settings;
    private PlayerRuntime _player;
    private PlayerBuffs _buffs;
    private List<StatModifier> _modifiers;
    private IntervalTimer _timer;
    private bool _initialized;
    private int _loopHandle;

    // Static state survives when "Enter Play Mode without domain reload" is on.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveByBuffId.Clear();
    }

    public void Initialize(ArtifactZoneSettings settings, PlayerRuntime player, PlayerBuffs buffs)
    {
        _settings = settings;
        _settings.tickInterval = Mathf.Max(0.1f, _settings.tickInterval);
        _player = player;
        _buffs = buffs;

        _modifiers = new List<StatModifier>();
        foreach (var entry in _settings.entries)
        {
            _modifiers.Add(new StatModifier(entry.stat, entry.value, entry.mode));
        }

        if (indicator != null)
        {
            float diameter = _settings.radius * 2f;
            indicator.localScale = new Vector3(diameter, indicator.localScale.y, diameter);
        }

        ActiveByBuffId.TryGetValue(_settings.buffId, out int alive);
        ActiveByBuffId[_settings.buffId] = alive + 1;

        if (loopSfx != null && AudioService.Instance != null)
            _loopHandle = AudioService.Instance.PlayLoop(loopSfx, transform, _settings.duration + 0.5f);

        _timer = new IntervalTimer(_settings.duration, _settings.tickInterval);
        _timer.OnInterval = OnInterval;
        _timer.OnTimerStop = OnStop;
        _timer.Start();

        _initialized = true;
    }

    private void OnDestroy()
    {
        // Destroyed before the timer finished (scene unload, level change): do not leave it ticking on a dead object.
        if (_timer != null)
        {
            _timer.Dispose();
            _timer = null;
        }

        if (_loopHandle != 0) AudioService.Instance?.StopLoop(_loopHandle, 0.3f);

        if (!_initialized) return;

        int remaining = 0;
        if (ActiveByBuffId.TryGetValue(_settings.buffId, out int alive))
        {
            remaining = Mathf.Max(0, alive - 1);
            if (remaining == 0) ActiveByBuffId.Remove(_settings.buffId);
            else ActiveByBuffId[_settings.buffId] = remaining;
        }

        if (remaining == 0 && _buffs != null && _modifiers is { Count: > 0 })
        {
            try
            {
                _buffs.Remove(_settings.buffId);
            }
            catch
            {
                Debug.LogWarning($"[ArtifactZone] PlayerRuntime or PlayerBuffs are missing.");
            }
        }
    }

    private void OnInterval()
    {
        if (!_initialized || _player == null) return;

        Vector3 offset = _player.transform.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude > _settings.radius * _settings.radius) return;

        bool affected = false;

        float heal = _settings.healFlat + _settings.healPercentOfMax / 100f * _player.TotalHealth;
        if (heal > 0f)
        {
            _player.Heal(heal);
            affected = true;
        }

        if (_buffs != null && _modifiers.Count > 0)
        {
            _buffs.Apply(_settings.buffId, _modifiers, _settings.tickInterval * 1.5f);
            affected = true;
        }

        if (affected) AudioService.Instance?.PlaySfx(tickSfx, _player.transform.position);
    }

    private void OnStop()
    {
        _timer = null;
        Destroy(gameObject);
    }
}