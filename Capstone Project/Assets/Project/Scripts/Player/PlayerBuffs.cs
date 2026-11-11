using System;
using System.Collections.Generic;
using UnityEngine;

// Buff co thoi han tren nhan vat nguoi choi. Moi buff la 1 'nguon' modifier
// trong PlayerRuntime(SetModifierSource), het han chi go dung phan cua no - khong anh huong chi so tu trang bi.
// Dung lai cung buffId = LAM MOI thoi gian, khong cong don.
public class PlayerBuffs : MonoBehaviour
{
    private const string SourcePrefix = "buff:";

    private class ActiveBuff
    {
        public float remaining;
        public float duration;
    }

    private readonly Dictionary<string, ActiveBuff> _buffs = new();
    private readonly List<string> _expired = new();

    private PlayerRuntime _player;
    public event Action OnBuffsChanged;

    private void Awake()
    {
        _player = GetComponent<PlayerRuntime>();
    }

    private void Update()
    {
        if(_buffs.Count == 0) return;
        
        _expired.Clear();
        foreach (var pair in _buffs)
        {
            pair.Value.remaining -= Time.deltaTime;
            if(pair.Value.remaining <= 0f) _expired.Add(pair.Key);
        }

        foreach (var id in _expired) Remove(id);
    }

    public void Apply(string buffId, IEnumerable<StatModifier> modifiers, float duration)
    {
        if(_player == null || string.IsNullOrEmpty(buffId) || duration <= 0f) return;
        
        _buffs[buffId] = new ActiveBuff { remaining = duration, duration = duration };
        _player.SetModifierSource(SourcePrefix + buffId, modifiers);
        
        OnBuffsChanged?.Invoke();
    }

    public bool Remove(string buffId)
    {
        if(!_buffs.Remove(buffId)) return false;
        _player.RemoveModifierSource(SourcePrefix + buffId);
        OnBuffsChanged?.Invoke();
        return true;
    }
    
    public bool IsActive(string buffId) => _buffs.ContainsKey(buffId);

    // Cho cong cu debug/HUD: danh sach buff dang chay va thoi gian con lai.
    public IEnumerable<KeyValuePair<string, float>> GetActive()
    {
        foreach (var pair in _buffs)
        {
            yield return new KeyValuePair<string, float>(pair.Key, pair.Value.remaining);
        }
    }
    
    public float GetRemaining(string buffId) => _buffs.TryGetValue(buffId, out var buff) ? buff.remaining : 0f;
}