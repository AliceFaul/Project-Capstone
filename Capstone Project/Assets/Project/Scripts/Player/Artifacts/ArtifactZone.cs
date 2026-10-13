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

// Vung ton tai N giay, CHI anh huong nguoi choi: moi tick,
// neu nguoi choi dung trong ban kinh thi duoc hoi mau va/hoac nhan buff.
// Buff chi keo dai ~1.5 tick nen roi vung la het buff.
public class ArtifactZone : MonoBehaviour
{
    [Tooltip("Tuy chon: object hinh anh (vong tron, VFX) se duoc scale theo duong kinh vung.")]
    [SerializeField] private Transform indicator;
    
    private ArtifactZoneSettings _settings;
    private PlayerRuntime _player;
    private PlayerBuffs _buffs;
    private List<StatModifier> _modifiers;
    private float _elapsed;
    private IntervalTimer _timer;
    private bool _initialized;

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

        _elapsed = 0f;
        
        _timer = new IntervalTimer(_settings.duration, _settings.tickInterval);
        _timer.OnInterval = OnInterval;
        _timer.OnTimerStop = OnStop;
        _timer.Start();
        
        _initialized = true;
    }

    private void OnDestroy()
    {
        if (_initialized && _buffs != null && _modifiers is { Count: > 0 })
        {
            try
            {
                _buffs.Remove(_settings.buffId);
            }
            catch
            {
                // Bắt ngoại lệ phòng hờ PlayerRuntime/PlayerBuffs đã bị hủy trước
            }
        }
    }

    private void OnInterval()
    {
        if(!_initialized) return;
        if(_player == null) return;
        
        Vector3 offset = _player.transform.position - transform.position;
        offset.y = 0f;
        
        if (offset.sqrMagnitude > _settings.radius * _settings.radius) return;

        float heal = _settings.healFlat + _settings.healPercentOfMax / 100f * _player.TotalHealth;
        if(heal > 0f) _player.Heal(heal);
        
        if(_buffs != null && _modifiers.Count > 0) _buffs.Apply(_settings.buffId, _modifiers, _settings.tickInterval * 1.5f);
        _elapsed += _settings.tickInterval;
    }

    private void OnStop()
    {
        _timer = null;
        Destroy(gameObject);
    }
}