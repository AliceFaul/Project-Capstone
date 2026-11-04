using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class InventoryUIUtil
{
    public const string HighlightColor = "#FFD27A";
    private static readonly Regex NumberRegex = new Regex(@"\d+(?:[.,]\d+)?%?");

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

    public static string RarityName(Rarity rarity) => InventoryText.Get("RARITY_" + rarity.ToString().ToUpper());

    public static string StatName(BonusStat stat)
    {
        string key;
        switch (stat)
        {
            case BonusStat.Health: key = "STAT_HEALTH"; break;
            case BonusStat.Defense: key = "STAT_DEFENSE"; break;
            case BonusStat.Damage: key = "STAT_DAMAGE"; break;
            case BonusStat.AttackRange: key = "STAT_ATTACK_RANGE"; break;
            case BonusStat.AttackSpeed: key = "STAT_ATTACK_SPEED"; break;
            case BonusStat.MoveSpeed: key = "STAT_MOVE_SPEED"; break;
            case BonusStat.CritChance: key = "STAT_CRIT_CHANCE"; break;
            default: key = "STAT_CRIT_DAMAGE"; break;
        }
        return InventoryText.Get(key);
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

    // Wraps every number / percentage in a highlight color (works for any language). Text that already has <color> tags is left alone.
    public static string HighlightNumbers(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Contains("<color")) return text;
        return NumberRegex.Replace(text, m => $"<color={HighlightColor}>{m.Value}</color>");
    }

    public static string RuneLine(RuneInstance rune, RuneData data)
    {
        if (data == null) return InventoryText.Get("RUNE_UNKNOWN");

        if (data.kind == RuneKind.DamageEffect)
        {
            RuneResolver.TryGetRoll(rune, RuneData.DamagePerTickId, out float dmg);
            RuneResolver.TryGetRoll(rune, RuneData.DurationId, out float dur);
            return InventoryText.Get("RUNE_EFFECT_LINE", data.itemName, dmg.ToString("0.#"), data.effectType,
                data.tickInterval.ToString("0.#"), dur.ToString("0.#"));
        }

        var parts = new List<string>();
        foreach (var line in data.statLines)
        {
            if (!RuneResolver.TryGetRoll(rune, line.Id, out float v)) continue;
            parts.Add(line.mode == StatMode.Percent ? $"+{v:0.##}% {StatName(line.stat)}" : $"+{v:0.##} {StatName(line.stat)}");
        }
        return InventoryText.Get("RUNE_STAT_LINE", data.itemName, string.Join(", ", parts));
    }

    // Effect text without the rune name (used by the socket popup rows).
    public static string RuneSummary(RuneInstance rune, RuneData data)
    {
        if (data == null) return InventoryText.Get("RUNE_UNKNOWN");

        if (data.kind == RuneKind.DamageEffect)
        {
            RuneResolver.TryGetRoll(rune, RuneData.DamagePerTickId, out float dmg);
            RuneResolver.TryGetRoll(rune, RuneData.DurationId, out float dur);
            return InventoryText.Get("RUNE_EFFECT_SUMMARY", dmg.ToString("0.#"), data.effectType,
                data.tickInterval.ToString("0.#"), dur.ToString("0.#"));
        }

        var parts = new List<string>();
        foreach (var line in data.statLines)
        {
            if (!RuneResolver.TryGetRoll(rune, line.Id, out float v)) continue;
            parts.Add(line.mode == StatMode.Percent ? $"+{v:0.##}% {StatName(line.stat)}" : $"+{v:0.##} {StatName(line.stat)}");
        }
        return string.Join(", ", parts);
    }

    public static string SocketMessage(SocketResult result, string successKey)
    {
        switch (result)
        {
            case SocketResult.Success: return InventoryText.Get(successKey);
            case SocketResult.NotEnoughGem: return InventoryText.Get("TOAST_NOT_ENOUGH_GEM");
            case SocketResult.InventoryFull: return InventoryText.Get("TOAST_RUNE_BAG_FULL");
            case SocketResult.SocketLocked: return InventoryText.Get("TOAST_SOCKET_LOCKED");
            case SocketResult.SocketOccupied: return InventoryText.Get("TOAST_SOCKET_OCCUPIED");
            case SocketResult.RuneNotInInventory: return InventoryText.Get("TOAST_RUNE_MISSING");
            default: return InventoryText.Get("TOAST_GENERIC_FAIL");
        }
    }
}