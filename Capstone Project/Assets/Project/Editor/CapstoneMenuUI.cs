#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One build owns one preview scene. Transient UI objects never become instances in a gameplay scene.
internal sealed class CapstoneMenuUI : IDisposable
{
    internal static readonly Color Bg = new Color(.055f, .133f, .188f, 1f);
    internal static readonly Color TopBarColor = new Color(.03f, .082f, .118f, 1f);
    internal static readonly Color Panel = new Color(.078f, .149f, .184f, .97f);
    internal static readonly Color Border = new Color(.145f, .275f, .353f, 1f);
    internal static readonly Color Grey = new Color(.62f, .71f, .75f, 1f);
    internal static readonly Color Orange = new Color(.94f, .50f, .16f, 1f);

    private readonly CapstoneUIEditor.BuildScene preview;
    private readonly string folder;
    internal CapstoneUIBuildOptions Options { get; }

    internal CapstoneMenuUI(CapstoneUIBuildOptions options, string subfolder)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        CapstoneUIEditor.ValidateAssetFolder(options.Output);
        if (options.Font == null) throw new InvalidOperationException("Assign a TMP font for the UI build.");
        folder = options.Output.TrimEnd('/') + "/" + subfolder;
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        preview = new CapstoneUIEditor.BuildScene();
    }

    public void Dispose() => preview.Dispose();

    internal GameObject Canvas(string name, int sortingOrder)
    {
        var root = CapstoneUIEditor.Canvas(name, sortingOrder);
        preview.Own(root);
        return root;
    }

    internal GameObject Root(string name, params Type[] components)
    {
        var root = new GameObject(name, components);
        preview.Own(root);
        return root;
    }

    internal GameObject Save(GameObject root)
    {
        var prefab = CapstoneUIEditor.Save(root, folder + "/" + root.name + ".prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    // The original menu layouts rely on centered anchors and pivots for unplaced rects,
    // especially the graph's node/edge layers, column labels and slider handle.
    private static RectTransform Center(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    internal static RectTransform CreateRect(string name, Transform parent) =>
        Center(CapstoneUIEditor.Rect(name, parent, 0, 0, 100, 100));

    internal static RectTransform Place(RectTransform rect, float x, float y, float w, float h) =>
        CapstoneUIEditor.Place(rect, x, y, w, h);

    internal static void Stretch(RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    internal static Image Box(string name, Transform parent, Color color, bool raycast = false)
    {
        var image = CapstoneUIEditor.Image(name, parent, 0, 0, 100, 100, color, null, raycast);
        Center(image.rectTransform);
        return image;
    }

    internal TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Bold)
    {
        var text = CapstoneUIEditor.Text(name, parent, 0, 0, 100, 100, value, size, color, Options, align);
        if (parent == null) preview.Own(text.gameObject); // Standalone section and column-label prefabs.
        Center(text.rectTransform);
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    internal TextMeshProUGUI LocalizedText(string name, Transform parent, string key, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Bold)
    {
        string value = LabelDefault(key);
        var text = Text(name, parent, value, size, color, align, style);
        var localized = text.gameObject.AddComponent<LocalizationText>();
        CapstoneUIEditor.Ref(localized, "text", text);
        LocalizedStringField.SetValue(localized, InventoryText.Ref(key));
        EditorUtility.SetDirty(localized);
        return text;
    }

    internal RectTransform KeyBadge(string name, Transform parent, string key, float width, float height)
    {
        var badge = Box(name, parent, Color.white);
        badge.sprite = Options.Shape("Key");
        badge.type = Image.Type.Sliced;
        badge.rectTransform.sizeDelta = new Vector2(width, height);
        var text = Text("Label", badge.transform, key, height * .5f, TopBarColor, TextAlignmentOptions.Center);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(text.rectTransform);
        return badge.rectTransform;
    }

    internal static void VerticalList(GameObject target, float spacing)
    {
        var layout = target.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;
    }

    internal static void PreferredHeight(GameObject target, float height) =>
        target.AddComponent<LayoutElement>().preferredHeight = height;

    internal static void SetRefs(Object target, params (string name, Object value)[] bindings)
    {
        var data = new SerializedObject(target);
        foreach (var binding in bindings)
        {
            var property = data.FindProperty(binding.name);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException($"Expected a serialized reference at {target.GetType().Name}.{binding.name}.");
            var field = target.GetType().GetField(binding.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (binding.value != null && field != null && !field.FieldType.IsInstanceOfType(binding.value))
                throw new InvalidOperationException($"{target.GetType().Name}.{binding.name} expects {field.FieldType.Name}, got {binding.value.GetType().Name}.");
            property.objectReferenceValue = binding.value;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        data.Update();
        foreach (var binding in bindings)
            if (data.FindProperty(binding.name).objectReferenceValue != binding.value)
                throw new InvalidOperationException($"Could not assign {target.GetType().Name}.{binding.name}.");
    }

    internal static void SetArray(Object target, string name, Object[] values)
    {
        var data = new SerializedObject(target);
        var array = data.FindProperty(name);
        if (array == null || !array.isArray)
            throw new InvalidOperationException($"Expected a serialized array at {target.GetType().Name}.{name}.");
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            var element = array.GetArrayElementAtIndex(i);
            if (element.propertyType != SerializedPropertyType.ObjectReference || values[i] == null)
                throw new InvalidOperationException($"Invalid UI reference at {name}[{i}].");
            element.objectReferenceValue = values[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        data.Update();
        array = data.FindProperty(name);
        for (int i = 0; i < values.Length; i++)
            if (array.GetArrayElementAtIndex(i).objectReferenceValue != values[i])
                throw new InvalidOperationException($"Could not assign {target.GetType().Name}.{name}[{i}].");
    }

    internal static void RequireFields<T>(params string[] names)
    {
        foreach (string name in names) CapstoneUIEditor.RequireField(typeof(T), name, null);
    }

    private static readonly FieldInfo LocalizedStringField = typeof(LocalizationText).GetField(
        "localizedString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    internal static void ValidateLocalization() => RequireFields<LocalizationText>("text", "localizedString");

    // Read-only lookup for validation. A different asset at the canonical path must never be overwritten.
    internal static T ExistingConfig<T>(T selected, string defaultPath) where T : ScriptableObject
    {
        if (selected != null)
        {
            if (!EditorUtility.IsPersistent(selected))
                throw new InvalidOperationException($"Select a saved {typeof(T).Name} asset for the prefab.");
            return selected;
        }
        var asset = AssetDatabase.LoadAssetAtPath<T>(defaultPath);
        if (asset == null && (File.Exists(defaultPath) || AssetDatabase.LoadMainAssetAtPath(defaultPath) != null))
            throw new InvalidOperationException($"Expected {typeof(T).Name} at {defaultPath}. Select the intended asset in the tool.");
        return asset;
    }

    internal static T ResolveConfig<T>(T selected, string defaultPath) where T : ScriptableObject
    {
        var asset = ExistingConfig(selected, defaultPath);
        if (asset != null) return asset;
        Directory.CreateDirectory(Path.GetDirectoryName(defaultPath));
        AssetDatabase.Refresh();
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, defaultPath);
        if (!EditorUtility.IsPersistent(asset))
        {
            Object.DestroyImmediate(asset);
            throw new IOException("Could not create " + defaultPath);
        }
        return asset;
    }

    // Only supply missing English defaults. The existing localization tool preserves entries in each locale.
    internal static string LabelDefault(string key)
    {
        if (InventoryText.Defaults.TryGetValue(key, out var value)) return value;
        if (!Fallbacks.TryGetValue(key, out value)) return key;
        InventoryText.Defaults.Add(key, value);
        return value;
    }

    private static readonly Dictionary<string, string> Fallbacks = new Dictionary<string, string>
    {
        { "SET_TITLE", "Settings" }, { "SET_TAB_GENERAL", "General" }, { "SET_TAB_VIDEO", "Video" },
        { "SET_TAB_AUDIO", "Audio" }, { "SET_TAB_CONTROLS", "Controls" }, { "SET_SELECTED", "Selected setting" },
        { "SET_BACK", "Back" }, { "SET_RESET", "Reset" }, { "SET_APPLY", "Apply" },
        { "PAUSE_TITLE", "Pause" }, { "PAUSE_RESUME", "Resume" }, { "PAUSE_SETTINGS", "Settings" },
        { "PAUSE_ROUTE_MAP", "Route map" }, { "PAUSE_ABANDON", "Abandon run" }, { "PAUSE_ABANDON_SUB", "Return to lobby" },
        { "PAUSE_QUIT", "Quit" }, { "PAUSE_CURRENT_RUN", "Current run" }, { "PAUSE_DIFFICULTY", "Difficulty" },
        { "PAUSE_NODE", "Node" }, { "PAUSE_TIME", "Time" }, { "PAUSE_KILLS", "Kills" },
        { "PAUSE_GOLD_EARNED", "Gold earned" }, { "PAUSE_POWER", "Power" }, { "PAUSE_GOLD", "Gold" },
        { "PAUSE_GEM", "Gems" }, { "PAUSE_COMBAT_POWER", "Combat power" }, { "PAUSE_ATTRIBUTES", "Attributes" },
        { "PAUSE_MAX_HP", "Max HP" }, { "PAUSE_ARMOR", "Armor" }, { "PAUSE_MOVE", "Move speed" },
        { "PAUSE_CRIT_CHANCE", "Crit chance" }, { "PAUSE_CRIT_DAMAGE", "Crit damage" },
        { "PAUSE_LEVELS_CLEARED", "Levels cleared" }, { "PAUSE_CONFIRM_CAPTION", "Current run" },
        { "PAUSE_CONFIRM_TITLE", "Abandon this run?" }, { "PAUSE_CONFIRM_BODY", "Leave the current run and return to the lobby?" },
        { "PAUSE_KEEP", "Keep playing" }, { "PAUSE_ABANDON_CONFIRM", "Abandon run" },
        { "PAUSE_SELECT", "Select" }, { "PAUSE_CONFIRM", "Confirm" },
        { "NODEMAP_TITLE", "Route map" }, { "NODEMAP_PROGRESS", "Progress" }, { "NODEMAP_SELECTED", "Selected node" },
        { "NODEMAP_ENEMY_HP", "Enemy HP" }, { "NODEMAP_ENEMY_DMG", "Enemy damage" }, { "NODEMAP_LOOT", "Loot" },
        { "NODEMAP_HOW_CAPTION", "Route" }, { "NODEMAP_HOW", "Lines show the paths between areas. Select a node to see its details." },
        { "NODEMAP_CLOSE", "Close" }, { "NODE_COMBAT", "Combat" }, { "NODE_ELITE", "Elite" }, { "NODE_REWARD", "Reward" },
        { "NODE_EVENT", "Event" }, { "NODE_REST", "Rest" }, { "NODE_SHOP", "Shop" }, { "NODE_BOSS", "Boss" },
        { "CTRL_NODEMAP", "Route map" }
    };
}
#endif
