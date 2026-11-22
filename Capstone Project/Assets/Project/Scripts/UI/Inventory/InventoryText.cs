using System;
using System.Collections.Generic;
using UnityEngine.Localization;

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
        { "INV_BACK", "Back" }, { "INV_EQUIP_UNEQUIP", "Equip / Unequip" }, { "INV_BAG_HINT", "Click to pin details" },
        // stats
        { "STAT_HEALTH", "Health" }, { "STAT_DEFENSE", "Defense" }, { "STAT_DAMAGE", "Damage" },
        { "STAT_ATTACK_RANGE", "Attack Range" }, { "STAT_ATTACK_SPEED", "Attack Speed" }, { "STAT_MOVE_SPEED", "Move Speed" },
        { "STAT_CRIT_CHANCE", "Crit Chance" }, { "STAT_CRIT_DAMAGE", "Crit Damage" },
        // rarity / weapon types
        { "RARITY_COMMON", "COMMON" }, { "RARITY_UNCOMMON", "UNCOMMON" }, { "RARITY_RARE", "RARE" }, { "RARITY_LEGENDARY", "LEGENDARY" },
        { "WEAPON_SWORD", "Sword" }, { "WEAPON_AXE", "Axe" }, { "WEAPON_BOW", "Bow" }, { "WEAPON_CROSSBOW", "Crossbow" },
        // detail panel
        { "DETAIL_EQUIPPED", "EQUIPPED" }, { "DETAIL_IN_BAG", "IN BAG" }, { "DETAIL_UNKNOWN_ITEM", "UNKNOWN ITEM" },
        { "DETAIL_PINNED", "PINNED" },
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
        { "POPUP_SOCKETS", "SOCKETS" }, { "POPUP_STATE_ACTIVE", "ACTIVE" }, { "POPUP_STATE_INACTIVE", "INACTIVE" },
        // rune socket popup
        { "POPUP_TITLE", "RUNE SOCKETS" }, { "POPUP_ACTIVE_RUNES", "ACTIVE RUNES" }, { "POPUP_RUNES_IN_BAG", "RUNES IN BAG" },
        { "POPUP_SOCKET_EMPTY", "Socket {0} - empty" }, { "POPUP_SOCKET_LOCKED", "Socket {0} - locked" },
        { "POPUP_PICK_RUNE", "Pick a rune from the bag on the right" }, { "POPUP_NO_RUNE", "Select a rune from the bag to see its details." },
        { "POPUP_REMOVE", "Remove" }, { "POPUP_CONFIRM", "Confirm" }, { "POPUP_COST", "{0} Gem" },
        { "POPUP_INSERT", "INSERT" }, { "POPUP_CANCEL", "Cancel" }, { "POPUP_DESCRIPTION", "DESCRIPTION" },
        { "POPUP_CONFLICT_REPLACES", "Same group as {0}: {0} would become inactive." },
        { "POPUP_CONFLICT_WEAKER", "Same group as {0}: this rune would stay inactive." },
        { "RUNE_EFFECT_SUMMARY", "{0} {1} damage every {2}s for {3}s" },
        // level select
        { "LEVEL_BADGE", "LEVEL {0}" }, { "LEVEL_EASY", "EASY" }, { "LEVEL_HARD", "HARD" },
        { "LEVEL_RECOMMENDED", "Recommended power: {0}" }, { "LEVEL_ENEMIES", "Enemies: x{0} health, x{1} damage" },
        { "LEVEL_LOOT_BETTER", "Better odds for rare loot" }, { "LEVEL_LOOT_STANDARD", "Standard loot odds" },
        { "LEVEL_YOUR_POWER", "YOUR POWER" }, { "LEVEL_POWER_READY", "READY" }, { "LEVEL_POWER_BELOW", "{0} below" },
        { "LEVEL_GEAR_DROPS", "GEAR DROPS" }, { "LEVEL_RUNES", "RUNES" }, { "LEVEL_ARTIFACTS", "ARTIFACTS" },
        { "LEVEL_START", "START RUN" }, { "LEVEL_LOCKED", "LOCKED" }, { "LEVEL_CLEAR_FIRST", "Clear {0} first" },
        { "LEVEL_BEST_CLEAR", "BEST CLEAR" }, { "LEVEL_CLEARED_TIMES", "Cleared {0} times" }, { "LEVEL_NOT_CLEARED", "Not cleared yet" },
        { "LEVEL_LOCKED_LEVEL", "Clear the previous level to unlock this one." }, { "LEVEL_BACK", "Back" },
        { "LEVEL_CANNOT_START", "Cannot start this level." },
        // route map (node map)
        { "NODEMAP_TITLE", "ROUTE MAP" }, { "NODEMAP_PROGRESS", "PROGRESS" }, { "NODEMAP_NODE_FMT", "Node {0} / {1}" },
        { "NODEMAP_SELECTED", "SELECTED NODE" }, { "NODEMAP_HOW_CAPTION", "HOW IT WORKS" },
        { "NODEMAP_HOW", "Walk to an exit of this area to choose the next node. Bright nodes are the ones you can reach from here." },
        { "NODEMAP_START", "START" }, { "NODEMAP_BOSS", "BOSS" }, { "NODEMAP_CLOSE", "Close" },
        { "NODEMAP_TAG_HERE", "YOU ARE HERE" }, { "NODEMAP_TAG_NEXT", "NEXT NODE" }, { "NODEMAP_TAG_VISITED", "VISITED" },
        { "NODEMAP_ENEMY_HP", "Enemy health" }, { "NODEMAP_ENEMY_DMG", "Enemy damage" }, { "NODEMAP_LOOT", "Loot" }, { "NODEMAP_LOOT_BETTER", "Better" },
        { "NODE_DESC_COMBAT", "Clear every enemy in the area to unlock the exits." },
        { "NODE_DESC_ELITE", "Stronger enemies guard a better reward. Fight carefully and heal before you go in." },
        { "NODE_DESC_REWARD", "A safe area with treasure to collect." },
        { "NODE_DESC_EVENT", "Something strange is here. It may help you, or cost you." },
        { "NODE_DESC_REST", "A quiet place to recover your health." },
        { "NODE_DESC_SHOP", "A merchant sells gear and buys what you do not need." },
        { "NODE_DESC_BOSS", "The final fight of this level." },
        { "PAUSE_ROUTE_MAP", "ROUTE MAP" },
        // pause menu
        { "PAUSE_TITLE", "PAUSED" }, { "PAUSE_LOBBY", "LOBBY" }, { "PAUSE_RESUME", "RESUME" }, { "PAUSE_SETTINGS", "SETTINGS" },
        { "PAUSE_ABANDON", "ABANDON RUN" }, { "PAUSE_ABANDON_SUB", "Return to lobby" }, { "PAUSE_QUIT", "QUIT TO MAIN MENU" },
        { "PAUSE_CURRENT_RUN", "CURRENT RUN" }, { "PAUSE_DIFFICULTY", "Difficulty" }, { "PAUSE_NODE", "Node" }, { "PAUSE_TIME", "Time" },
        { "PAUSE_KILLS", "Enemies defeated" }, { "PAUSE_GOLD_EARNED", "Gold earned" }, { "PAUSE_POWER", "Your power" },
        { "PAUSE_GOLD", "GOLD" }, { "PAUSE_GEM", "GEM" }, { "PAUSE_COMBAT_POWER", "COMBAT POWER" }, { "PAUSE_ATTRIBUTES", "ATTRIBUTES" },
        { "PAUSE_MAX_HP", "Max HP" }, { "PAUSE_ARMOR", "Armor" }, { "PAUSE_MOVE", "Move speed" }, { "PAUSE_CRIT_CHANCE", "Crit chance" },
        { "PAUSE_CRIT_DAMAGE", "Crit damage" }, { "PAUSE_LEVELS_CLEARED", "Levels cleared" }, { "PAUSE_LEVEL_FMT", "LV {0}" },
        { "PAUSE_CONFIRM_CAPTION", "ABANDON RUN" }, { "PAUSE_CONFIRM_TITLE", "Leave this run?" },
        { "PAUSE_CONFIRM_BODY", "The run ends here and you return to the lobby. Items and gold you picked up in this run are kept; the run counts as failed and the level is not cleared." },
        { "PAUSE_KEEP", "KEEP PLAYING" }, { "PAUSE_ABANDON_CONFIRM", "ABANDON" }, { "PAUSE_SELECT", "Select" }, { "PAUSE_CONFIRM", "Confirm" },
        // settings
        { "SET_TITLE", "SETTINGS" },
        { "SET_TAB_GENERAL", "GENERAL" }, { "SET_TAB_VIDEO", "VIDEO" }, { "SET_TAB_AUDIO", "AUDIO" }, { "SET_TAB_CONTROLS", "CONTROLS" },
        { "SET_SEC_LANGUAGE", "LANGUAGE & GAMEPLAY" }, { "SET_SEC_INTERFACE", "INTERFACE" }, { "SET_SEC_ACCOUNT", "ACCOUNT" },
        { "SET_SEC_DISPLAY", "DISPLAY" }, { "SET_SEC_QUALITY", "QUALITY" }, { "SET_SEC_VOLUME", "VOLUME" }, { "SET_SEC_OPTIONS", "OPTIONS" },
        { "SET_SEC_MOVEMENT", "MOVEMENT & NAVIGATION" }, { "SET_SEC_COMBAT", "COMBAT" },
        { "SET_LANGUAGE", "Language" }, { "SET_LANGUAGE_DESC", "The language of menus, items and dialogue." },
        { "SET_SHOW_FPS", "Show FPS" }, { "SET_SHOW_FPS_DESC", "Shows the frame rate in a corner of the screen." },
        { "SET_DAMAGE_NUMBERS", "Damage numbers" }, { "SET_DAMAGE_NUMBERS_DESC", "Shows floating numbers when something takes damage." },
        { "SET_SCREEN_SHAKE", "Screen shake" }, { "SET_SCREEN_SHAKE_DESC", "How strongly the camera shakes on hits and explosions. 0% turns it off." },
        { "SET_UI_SCALE", "UI scale" }, { "SET_UI_SCALE_DESC", "Makes menus and the HUD smaller or larger." },
        { "SET_TUTORIAL_HINTS", "Tutorial hints" }, { "SET_TUTORIAL_HINTS_DESC", "Shows short tips the first time something new happens." },
        { "SET_PLAYER_NAME", "Player name" }, { "SET_PLAYER_NAME_DESC", "The name other players and the lobby see." },
        { "SET_SIGN_OUT", "Account" }, { "SET_SIGN_OUT_DESC", "Sign out and return to the sign-in screen. Your progress stays saved." },
        { "SET_CHANGE", "CHANGE" }, { "SET_SIGN_OUT_BUTTON", "SIGN OUT" },
        { "SET_DISPLAY_MODE", "Display mode" }, { "SET_DISPLAY_MODE_DESC", "Fullscreen, borderless window or a normal window. Needs Apply." },
        { "SET_RESOLUTION", "Resolution" }, { "SET_RESOLUTION_DESC", "The size of the game image. Needs Apply; it reverts by itself if you do not keep it." },
        { "SET_VSYNC", "VSync" }, { "SET_VSYNC_DESC", "Matches the frame rate to your screen to avoid tearing. Overrides the frame rate limit." },
        { "SET_FPS_LIMIT", "Frame rate limit" }, { "SET_FPS_LIMIT_DESC", "The most frames per second the game will draw." },
        { "SET_QUALITY", "Quality preset" }, { "SET_QUALITY_DESC", "Overall graphics quality. Lower it if the game runs slowly." },
        { "SET_SHADOWS", "Shadows" }, { "SET_SHADOWS_DESC", "How far shadows are drawn." },
        { "SET_POST_PROCESSING", "Post-processing" }, { "SET_POST_PROCESSING_DESC", "Bloom, color grading and other screen effects." },
        { "SET_BRIGHTNESS", "Brightness" }, { "SET_BRIGHTNESS_DESC", "Makes the whole image darker or brighter." },
        { "SET_MASTER", "Master volume" }, { "SET_MASTER_DESC", "The volume of everything." },
        { "SET_MUSIC", "Music" }, { "SET_MUSIC_DESC", "The volume of the music." },
        { "SET_SFX", "Sound effects" }, { "SET_SFX_DESC", "The volume of combat, interface and world sounds." },
        { "SET_MUTE_UNFOCUSED", "Mute when unfocused" }, { "SET_MUTE_UNFOCUSED_DESC", "Silences the game while another window is in front." },
        { "SET_ON", "ON" }, { "SET_OFF", "OFF" }, { "SET_FULLSCREEN", "Fullscreen" }, { "SET_BORDERLESS", "Borderless" }, { "SET_WINDOWED", "Windowed" },
        { "SET_UNLIMITED", "Unlimited" }, { "SET_SHADOW_OFF", "Off" }, { "SET_SHADOW_LOW", "Low" }, { "SET_SHADOW_MEDIUM", "Medium" }, { "SET_SHADOW_HIGH", "High" },
        { "SET_SELECTED", "SELECTED" }, { "SET_BACK", "Back" }, { "SET_RESET", "Reset section" }, { "SET_APPLY", "Apply" },
        { "SET_KEEP_PROMPT", "Keep these display settings? Press Enter ({0}s)" }, { "SET_LISTENING", "Press a key..." },
        { "SET_CONTROL_DESC", "Click, then press a key or mouse button. Esc cancels. If the key is already used, the two actions swap keys." },
        { "CTRL_MOVE", "Move, attack, interact" }, { "CTRL_ROLL", "Dash" }, { "CTRL_SPRINT", "Sprint" }, { "CTRL_INVENTORY", "Inventory" },
        { "CTRL_MINIMAP", "Minimap" }, { "CTRL_NODEMAP", "Route map" }, { "CTRL_RANGED", "Ranged attack" }, { "CTRL_ARTIFACT1", "Artifact 1" }, { "CTRL_ARTIFACT2", "Artifact 2" },
        { "CTRL_ARTIFACT3", "Artifact 3" }, { "CTRL_HEAL", "Use potion" },
        // levels
        { "DIFFICULTY_NORMAL", "Normal" }, { "DIFFICULTY_HARD", "Hard" }, { "DIFFICULTY_NIGHTMARE", "Nightmare" },
        // node contents
        { "REST_HEALED", "+{0} HP" }, { "EVENT_SUCCESS", "The shrine rewards you!" }, { "EVENT_FAILED", "Nothing happens..." },
        { "EVENT_NOT_ENOUGH_GOLD", "Not enough gold!" }, { "EVENT_NOT_ENOUGH_GEM", "Not enough Gem!" },
        { "EVENT_NOT_ENOUGH_HP", "You are too weak for this." },
        { "DOOR_LOCKED", "Locked - find the key" }, { "DOOR_OPENED", "Unlocked" }, { "KEY_FOUND", "Key found" },
        // node map
        { "NODE_COMBAT", "Battle" }, { "NODE_REWARD", "Treasure" }, { "NODE_EVENT", "Event" }, { "NODE_REST", "Rest" },
        { "NODE_ELITE", "Elite" }, { "NODE_BOSS", "Boss" }, { "NODE_SHOP", "Shop" },
        // pickups
        { "PICKUP_GOT", "+ {0}" }, { "PICKUP_BAG_FULL", "Bag is full" },
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