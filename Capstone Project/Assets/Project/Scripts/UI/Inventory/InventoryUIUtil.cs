using System.Collections.Generic;
using UnityEngine;

public static class InventoryUIUtil
{
    public static Color RarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: return new Color(0.31f, 0.70f, 0.42f);
            case Rarity.Rare: return new Color(0.25f, 0.53f, 0.88f);
            case Rarity.Legendary: return new Color(0.94f, 0.50f, 0.16f);
            default: return new Color(0.54f, 0.59f, 0.63f);
        }
    }

    public static string StatName(BonusStat stat)
    {
        switch (stat)
        {
            case BonusStat.AttackRange: return "Attack Range";
            case BonusStat.AttackSpeed: return "Attack Speed";
            case BonusStat.MoveSpeed: return "Move Speed";
            case BonusStat.CritChance: return "Crit Chance";
            case BonusStat.CritDamage: return "Crit Damage";
            default: return stat.ToString();
        }
    }

    public static string FormatStat(BonusStat stat, float value)
    {
        switch (stat)
        {
            case BonusStat.CritChance:
            case BonusStat.CritDamage: return $"{value:0.#}%";
            case BonusStat.AttackSpeed:
            case BonusStat.AttackRange:
            case BonusStat.MoveSpeed: return $"{value:0.##}";
            default: return $"{value:0.#}";
        }
    }

    // fill must be anchored to the left edge of its bar (anchorMin.x = 0).
    public static void SetBar(RectTransform fill, float ratio)
    {
        if (fill == null) return;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }

    public static string RuneLine(RuneInstance rune, RuneData data)
    {
        if (data == null) return "Unknown rune";

        if (data.kind == RuneKind.DamageEffect)
        {
            RuneResolver.TryGetRoll(rune, RuneData.DamagePerTickId, out float dmg);
            RuneResolver.TryGetRoll(rune, RuneData.DurationId, out float dur);
            return $"{data.itemName}: {dmg:0.#} {data.effectType} damage every {data.tickInterval:0.#}s for {dur:0.#}s";
        }

        var parts = new List<string>();
        foreach (var line in data.statLines)
        {
            if (!RuneResolver.TryGetRoll(rune, line.Id, out float v)) continue;
            parts.Add(line.mode == StatMode.Percent ? $"+{v:0.##}% {StatName(line.stat)}" : $"+{v:0.##} {StatName(line.stat)}");
        }
        return $"{data.itemName}: {string.Join(", ", parts)}";
    }
}
