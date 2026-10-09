using System;
using System.Collections.Generic;
using UnityEngine;

public enum RuneKind { Stat, DamageEffect }

[Serializable]
public class RollRange
{
    public float min;
    public float max;

    public float Roll(System.Random rng)
    {
        if(max <= min) return min;
        return (float)Math.Round(min + (max - min) * rng.NextDouble(), 2);
    }

    // 0..1: vi tri cua value trong [min, max] (khoang rong -> 1). Dung de cham "chat luong" roll.
    public float Normalize(float value)
    {
        if(max <= min) return 1f;
        return Mathf.Clamp01((value - min) / (max - min));
    }
}

[Serializable]
public class RuneStatLine
{
    [Tooltip("ID dong, duy nhat trong 1 rune. De trong = lay ten chi so.")]
    public string id;
    public BonusStat stat;
    public RollRange range = new RollRange();
    
    public string Id => string.IsNullOrEmpty(id) ? stat.ToString() : id;
}

[CreateAssetMenu(fileName = "New Rune Data", menuName = "Inventory/Rune Data")]
public class RuneData : ItemData
{
    public const string DamagePerTickId = "damagePerTick";
    public const string DurationId = "duration";
    
    [Header("Rune")]
    public RuneKind kind;
    [Tooltip("Cac Rune cung nhom: tren 1 vu khi chi vien MANH NHAT co hieu luc. De trong = nhom rieng cua chinh no. Nen dat chung nhom cho cac rune effect, vi enemy chi giu mot hieu ung moi loai")]
    public string exclusiveGroup;
    
    [Header("Kind = Stat (Nhieu dong chi so, cong PHANG)")]
    public List<RuneStatLine> statLines = new List<RuneStatLine>();
    
    [Header("Kind = Damage Effect")]
    public StatusEffectType effectType = StatusEffectType.Fire;
    public RollRange damagePerTick = new RollRange { min = 2f, max = 5f };
    public RollRange duration = new RollRange { min = 3f, max = 5f };
    public float tickInterval = 1f;
    [Range(0f, 1f)] public float proChance = 1f;
    
    [Header("VFX / SFX cua hieu ung")]
    public AudioClip castSound;
    public GameObject castVfx;
    public GameObject runningVfx;
    
    public string GroupKey => string.IsNullOrEmpty(exclusiveGroup) ? id : exclusiveGroup;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        var seen = new HashSet<string>();

        foreach (var line in statLines)
        {
            if(!seen.Add(line.Id)) Debug.LogWarning($"[RuneData] {name}: trung id dong '{line.Id}' - dat id khac nhau cho moi dong.", this);
            if(line.range.max < line.range.min) Debug.LogWarning($"[RuneData] {name}: dong '{line.Id}' co max < min.", this);
        }
    }
#endif
}