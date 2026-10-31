using System;
using System.Collections.Generic;
using UnityEngine.Localization;

/// <summary>
/// Utility create inventory localized text in UI Table.
/// </summary>
public static class InventoryText
{
    public const string Table = "UI";

    public static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
    {
        // static labels
        { "INV_TITLE", "INVENTORY" }, { "INV_LEVEL", "LEVEL" }, { "INV_EXPERIENCE", "EXPERIENCE" },
        { "INV_PLAYER_STATS", "TOTAL STATS" }, { "INV_PLAYER_STATS_HEADER", "TOTAL STATS" },
        { "INV_WEAPONS", "WEAPONS" }, { "INV_ARMOR", "ARMOR" }, { "INV_ARTIFACTS", "ARTIFACTS" },
        { "INV_MELEE", "MELEE" }, { "INV_RANGED", "RANGED" }, { "INV_RUNE", "RUNE" }, { "INV_ARTIFACT", "ARTIFACT" },
        { "INV_BACK", "Back" }, { "INV_EQUIP_UNEQUIP", "Equip / Unequip" }, { "INV_BAG_HINT", "Click an item to equip" },
        // stats
        { "STAT_HEALTH", "Health" }, { "STAT_DEFENSE", "Defense" }, { "STAT_DAMAGE", "Damage" },
        { "STAT_ATTACK_RANGE", "Attack Range" }, { "STAT_ATTACK_SPEED", "Attack Speed" }, { "STAT_MOVE_SPEED", "Move Speed" },
        { "STAT_CRIT_CHANCE", "Crit Chance" }, { "STAT_CRIT_DAMAGE", "Crit Damage" },
        // rarity / weapon types
        { "RARITY_COMMON", "COMMON" }, { "RARITY_UNCOMMON", "UNCOMMON" }, { "RARITY_RARE", "RARE" }, { "RARITY_LEGENDARY", "LEGENDARY" },
        { "WEAPON_SWORD", "Sword" }, { "WEAPON_AXE", "Axe" }, { "WEAPON_BOW", "Bow" }, { "WEAPON_CROSSBOW", "Crossbow" },
        // detail panel
        { "DETAIL_EQUIPPED", "EQUIPPED" }, { "DETAIL_IN_BAG", "IN BAG" }, { "DETAIL_UNKNOWN_ITEM", "UNKNOWN ITEM" },
        { "DETAIL_LEVEL", "Lv {0}" }, { "DETAIL_MAX_LEVEL", "MAX LEVEL" }, { "DETAIL_EXP", "{0} / {1} EXP" }, { "DETAIL_TIER", "Tier {0}" },
        { "DETAIL_SUB_ARMOR", "Armor" }, { "DETAIL_KIND_MELEE", "Melee weapon" }, { "DETAIL_KIND_RANGED", "Ranged weapon" },
        { "DETAIL_SUB_ARTIFACT", "Artifact" }, { "DETAIL_SUB_RUNE_STAT", "Stat rune" }, { "DETAIL_SUB_RUNE_EFFECT", "Damage effect rune" },
        { "DETAIL_STATS", "STATS" }, { "DETAIL_SOCKETS", "RUNES" },
        { "DETAIL_COOLDOWN", "Cooldown" }, { "DETAIL_SECONDS", "{0}s" },
        { "DETAIL_QUALITY", "Quality" }, { "DETAIL_DURATION", "Duration" }, { "DETAIL_EFFECT_DAMAGE", "{0} damage" },
        // runes / sockets
        { "RUNE_UNKNOWN", "Unknown rune" }, { "RUNE_INACTIVE", "(inactive)" },
        { "RUNE_EFFECT_LINE", "{0}: {1} {2} damage every {3}s for {4}s" }, { "RUNE_STAT_LINE", "{0}: {1}" },
        { "SOCKET_HEADER", "SOCKET {0}" }, { "SOCKET_FILLED_HEADER", "SOCKET {0} - CLICK TWICE TO REMOVE ({1} GEM)" },
        { "SOCKET_LOCKED_TITLE", "LOCKED SOCKET" }, { "SOCKET_LOCKED_BODY", "Unlocks when the weapon reaches level {0}." },
        // toasts
        { "TOAST_BAG_FULL", "The bag is full - cannot unequip." }, { "TOAST_CANNOT_EQUIP", "Cannot equip this item." },
        { "TOAST_NO_FREE_SOCKET", "No free socket on your equipped weapons." },
        { "TOAST_SOCKET_UNLOCKS", "This socket unlocks at weapon level {0}." },
        { "TOAST_PICK_RUNE", "Choose a rune from the bag (Esc to cancel)." }, { "TOAST_SELECT_CANCELLED", "Socket selection cancelled." },
        { "TOAST_REMOVE_CONFIRM", "Click again to remove this rune (costs {0} Gem)." },
        { "TOAST_RUNE_SOCKETED", "Rune socketed." }, { "TOAST_RUNE_REMOVED", "Rune removed." },
        { "TOAST_NOT_ENOUGH_GEM", "Not enough Gem." }, { "TOAST_RUNE_BAG_FULL", "The rune bag is full." },
        { "TOAST_SOCKET_LOCKED", "This socket is locked." }, { "TOAST_SOCKET_OCCUPIED", "This socket already has a rune." },
        { "TOAST_RUNE_MISSING", "That rune is no longer in the bag." }, { "TOAST_GENERIC_FAIL", "Cannot do that." },
    };

    private static readonly Dictionary<string, LocalizedString> Cache = new Dictionary<string, LocalizedString>();

    public static LocalizedString Ref(string key) => new LocalizedString(Table, key);

    // Localized text for 'key' with {0},{1}... arguments. Falls back to the English default if the key/table is not available.
    public static string Get(string key, params object[] args)
    {
        string text = null;
        bool hasArgs = args is { Length: > 0 };

        try
        {
            if (!Cache.TryGetValue(key, out var localized))
            {
                localized = new LocalizedString(Table, key);
                Cache[key] = localized;
            }

            localized.Arguments = hasArgs ? args : null;
            text = localized.GetLocalizedString();
        }
        catch (Exception)
        {
            text = null;
        }

        if (string.IsNullOrEmpty(text) || text.StartsWith("No translation found"))
            text = Defaults.TryGetValue(key, out var fallback) ? fallback : key;

        // Entries that are not "Smart" are returned raw - format them ourselves.
        if (hasArgs && text.Contains("{")) text = SafeFormat(text, args);
        return text;
    }

    private static string SafeFormat(string format, object[] args)
    {
        try { return string.Format(format, args); }
        catch (FormatException) { return format; }
    }
}