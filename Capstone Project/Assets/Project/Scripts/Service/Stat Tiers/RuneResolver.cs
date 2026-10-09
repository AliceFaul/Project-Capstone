using UnityEngine;
using System.Collections.Generic;
using System;

public readonly struct ActiveRune
{
    public readonly int SocketIndex;
    public readonly RuneInstance Instance;
    public readonly RuneData Data;
    public readonly float Quality;

    public ActiveRune(int socketIndex, RuneInstance instance, RuneData data, float quality)
    {
        SocketIndex = socketIndex;
        Instance = instance;
        Data = data;
        Quality = quality;
    }
}

public readonly struct RuneEffect
{
    public readonly RuneData Data;
    public readonly float DamagePerTick;
    public readonly float Duration;

    public RuneEffect(RuneData data, float damagePerTick, float duration)
    {
        Data = data;
        DamagePerTick = damagePerTick;
        Duration = duration;
    }
}

// Doc rune dang gan tren 1 vu khi: loc trung nhom, cong chi so, lay hieu ung.
// 'lookup' tra cuu RuneData theo id (thuong la EquipmentProgressConfig.GetRune)
public static class RuneResolver
{
    public static bool TryGetRoll(RuneInstance instance, string id, out float value)
    {
        value = 0f;
        if(instance == null || instance.rolls == null) return false;

        foreach (var roll in instance.rolls)
        {
            if(roll.id != id) continue;
            value = roll.value;
            return true;
        }
        return false;
    }

    // Diem chat luong 0..1 = trung binh vi tri roll cua tat ca cac dong trong khoang cua chung
    // Dung de so "manh hon" khi 2 rune cung nhom
    public static float GetQuality(RuneInstance instance, RuneData data)
    {
        float sum = 0f;
        int count = 0;

        foreach (var pair in EnumerateRanges(data))
        {
            if(!TryGetRoll(instance, pair.Key, out float value)) continue;
            sum += pair.Value.Normalize(value);
            count++;
        }
        
        return count > 0 ? sum / count : 0f;
    }

    private static IEnumerable<KeyValuePair<string, RollRange>> EnumerateRanges(RuneData data)
    {
        if (data.kind == RuneKind.Stat)
        {
            foreach (var line in data.statLines)
            {
                yield return new KeyValuePair<string, RollRange>(line.Id, line.range);
            }
        }
        else
        {
            yield return new KeyValuePair<string, RollRange>(RuneData.DamagePerTickId, data.damagePerTick);
            yield return new KeyValuePair<string, RollRange>(RuneData.DurationId, data.duration);
        }
    }

    // Rune co hieu luc: trong moi nhom chi giu vien co quality cao nhat (bang diem -> socket thap hon thang)
    public static List<ActiveRune> GetActiveRunes(EquipmentInstance weapon, Func<string, RuneData> lookup)
    {
        var result = new List<ActiveRune>();
        if(weapon == null || lookup == null) return result;
        
        weapon.EnsureSockets();
        var bestPerGroup = new Dictionary<string, ActiveRune>();

        for (int i = 0; i < weapon.sockets.Count; i++)
        {
            var rune = weapon.sockets[i];
            if(rune == null || rune.IsEmpty) continue;

            var data = lookup(rune.runeId);
            if(data == null) continue;

            var candidate = new ActiveRune(i, rune, data, GetQuality(rune, data));
            string group = data.GroupKey;

            if (!bestPerGroup.TryGetValue(group, out var current) || candidate.Quality > current.Quality)
            {
                bestPerGroup[group] = candidate;
            }
        }
        
        result.AddRange(bestPerGroup.Values);
        result.Sort((a, b) => a.SocketIndex.CompareTo(b.SocketIndex));
        return result;
    }

    // Tong gia tri cac dong 'stat' cua rune chi so dang co hieu luc
    public static float GetStatBonus(EquipmentInstance weapon, BonusStat stat, Func<string, RuneData> lookup)
    {
        float total = 0f;

        foreach (var active in GetActiveRunes(weapon, lookup))
        {
            if(active.Data.kind != RuneKind.Stat) continue;

            foreach (var line in active.Data.statLines)
            {
                if(line.stat == stat && TryGetRoll(active.Instance, line.Id, out float value))
                    total += value;
            }
        }
        
        return total;
    }

    public static List<RuneEffect> GetEffects(EquipmentInstance weapon, Func<string, RuneData> lookup)
    {
        var effects = new List<RuneEffect>();

        foreach (var active in GetActiveRunes(weapon, lookup))
        {
            if(active.Data.kind != RuneKind.DamageEffect) continue;
            
            TryGetRoll(active.Instance, RuneData.DamagePerTickId, out float damagePerTick);
            TryGetRoll(active.Instance, RuneData.DurationId, out float duration);
            effects.Add(new RuneEffect(active.Data, damagePerTick, duration));
        }
        
        return effects;
    }
}

public static class RuneEffectFactory
{
    public static StatusEffect Create(RuneEffect effect)
    {
        var dot = new DamageOverTime
        {
            statusType = effect.Data.effectType,
            Duration = effect.Duration,
            TickInterval = effect.Data.tickInterval,
            DamagePerTick = Mathf.Max(1, Mathf.RoundToInt(effect.DamagePerTick))
        };

        var status = new StatusEffect
        {
            castSound = effect.Data.castSound,
            castVfx = effect.Data.castVfx,
            runningVfx = effect.Data.runningVfx
        };
        
        status.effects.Add(dot);
        return status;
    }
}