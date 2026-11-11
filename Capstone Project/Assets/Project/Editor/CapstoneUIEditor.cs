#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CapstoneUIBuildOptions
{
    public string IconFolder, Output;
    public TMP_FontAsset Font;
    public Sprite Gold, Gem;
    public RenderTexture CharacterPreview;
    public PlayerHUDController ExistingHUD;
    public int HealIndex, RollIndex;
    internal readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
    internal readonly Dictionary<string, Sprite> Shapes = new Dictionary<string, Sprite>();
    public Sprite Icon(string name) => Icons.TryGetValue(name, out var value) ? value : null;
    public Sprite Shape(string name) => Shapes[name];
}

public static class CapstoneUIEditor
{
    public static readonly Color Ivory = new Color(.96f, .94f, .88f, 1f);
    public static readonly Color Slate = new Color(.08f, .16f, .21f, 1f);
    public static readonly string[] IconPaths =
    {
        "Shared/UI_Sword", "Shared/UI_Bow", "Shared/UI_Armor", "Shared/UI_Rune", "Shared/UI_Artifact",
        "Shared/UI_Backpack", "Shared/UI_Heart", "Shared/UI_TotalStats", "Shared/UI_Close",
        "HUD/HUD_Arrow", "HUD/HUD_Map", "HUD/HUD_Heal", "HUD/HUD_Roll",
        "Stats/Stat_AttackSpeed", "Stats/Stat_CritChance", "Stats/Stat_CritDamage", "Stats/Stat_Cooldown",
        "Stats/Stat_Defense", "Stats/Stat_MoveSpeed", "Status/UI_Equipped", "Status/UI_Lock",
        "Rune/UI_SocketEmpty", "Rune/UI_Quality", "Rune/UI_Duration", "Rune/UI_Insert",
        "Rune/UI_Remove", "Rune/UI_Warning", "Rune/UI_EffectFire"
    };

    public static void ValidateInputs(CapstoneUIBuildOptions o, string output, bool hud, bool inventory)
    {
        ValidateAssetFolder(o.IconFolder);
        ValidateAssetFolder(output);
        if (output.TrimEnd('/') == "Assets" || output.StartsWith(o.IconFolder + "/", StringComparison.OrdinalIgnoreCase) || output.TrimEnd('/') == o.IconFolder)
            throw new InvalidOperationException("Choose a separate output folder, e.g. Assets/CapstoneUI/Generated.");
        if (o.Font == null) throw new InvalidOperationException("Assign a TMP Font Asset, or import TMP Essential Resources first.");
        var missing = IconPaths.Where(p => !File.Exists(o.IconFolder + "/" + p + ".png")).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("Missing icons in " + o.IconFolder + ":\n" + string.Join("\n", missing));
        if (hud)
        {
            if (o.HealIndex < 0 || o.RollIndex < 0 || o.HealIndex > 15 || o.RollIndex > 15 || o.HealIndex == o.RollIndex)
                throw new InvalidOperationException("Heal/Roll indices must be different, between 0 and 15.");
            RequireField(typeof(PlayerHUDView), "ammoRegenRightFill", typeof(Image));
            RequireField(typeof(PlayerHUDView), "artifactSlots", null);
        }
        if (inventory)
        {
            RequireField(typeof(InventoryScreenView), "popup", typeof(RuneSocketPopup));
            RequireField(typeof(RuneSocketPopup), "qualityFill", typeof(RectTransform));
            RequireField(typeof(RuneSocketRowView), "glyph", typeof(Image));
            RequireField(typeof(InventoryDetailView), "statIcons", null);
            RequireField(typeof(InventorySlotView), "emptyPlaceholder", typeof(Image));
        }
    }

    public static void ValidateAssetFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..") || path.Contains('\\') || path.Contains(':'))
            throw new InvalidOperationException("Use a project folder below Assets/ with forward slashes.");
    }

    public static void RequireField(Type type, string name, Type expected)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null || (expected != null && field.FieldType != expected))
            throw new InvalidOperationException(type.Name + " is not the expected uGUI version: missing/incompatible '" + name + "'. Replace the supplied view patches first.");
    }

    public static void ConfigureIconImports(CapstoneUIBuildOptions o)
    {
        foreach (string relative in IconPaths)
        {
            string path = o.IconFolder + "/" + relative + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("No texture importer: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    public static void ResolveIcons(CapstoneUIBuildOptions o)
    {
        foreach (string relative in IconPaths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(o.IconFolder + "/" + relative + ".png");
            if (sprite == null) throw new InvalidOperationException("Import as Sprite / Single: " + relative);
            o.Icons[Path.GetFileName(relative)] = sprite;
        }
    }

    public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return Place((RectTransform)go.transform, x, y, w, h);
    }
    public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }
    public static void Stretch(RectTransform rt, float inset = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one * inset; rt.offsetMax = Vector2.one * -inset;
    }
    public static Image Image(string name, Transform parent, float x, float y, float w, float h, Color color, Sprite sprite = null, bool raycast = false)
    {
        var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
        image.color = color; image.sprite = sprite; image.raycastTarget = raycast;
        return image;
    }
    public static Image Icon(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, float alpha = 1)
    {
        var image = Image(name, parent, x, y, w, h, new Color(1, 1, 1, alpha), sprite);
        image.preserveAspect = true;
        return image;
    }
    public static TextMeshProUGUI Text(string name, Transform parent, float x, float y, float w, float h, string value, float size, Color color, CapstoneUIBuildOptions o, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var text = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = o.Font; text.text = value; text.fontSize = size; text.color = color;
        text.alignment = align; text.fontStyle = FontStyles.Bold; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
    public static void Ref(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) throw new InvalidOperationException(target.GetType().Name + "." + field + " is missing.");
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static GameObject Save(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
        if (!success || prefab == null) throw new IOException("Could not save prefab: " + path);
        return prefab;
    }
    public static GameObject Canvas(string name, int sortingOrder)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = sortingOrder;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        return root;
    }

    // Temporary preview scene, never adds objects to the user's gameplay scene.
    public sealed class BuildScene : IDisposable
    {
        private readonly Scene scene = EditorSceneManager.NewPreviewScene();
        public void Own(GameObject root) => SceneManager.MoveGameObjectToScene(root, scene);
        public void Dispose() => EditorSceneManager.ClosePreviewScene(scene);
    }

    public static void CreateShapes(CapstoneUIBuildOptions o)
    {
        string folder = o.Output + "/Shapes";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var square = new[] { new Vector2(.06f,.06f), new Vector2(.94f,.06f), new Vector2(.94f,.94f), new Vector2(.06f,.94f) };
        var clipped = new[] { new Vector2(0,.25f), new Vector2(.25f,0), new Vector2(.75f,0), new Vector2(1,.25f), new Vector2(1,.75f), new Vector2(.75f,1), new Vector2(.25f,1), new Vector2(0,.75f) };
        var heart = new[] { new Vector2(.5f,.03f), new Vector2(.04f,.38f), new Vector2(.04f,.76f), new Vector2(.23f,.92f), new Vector2(.5f,.77f), new Vector2(.77f,.92f), new Vector2(.96f,.76f), new Vector2(.96f,.38f) };
        WriteShape(o, folder, "Slot", 64, square, new Color(.09f,.10f,.11f,.98f), new Color(.60f,.62f,.63f), .045f, new Vector4(8,8,8,8));
        WriteShape(o, folder, "Panel", 128, clipped, new Color(.035f,.045f,.045f,.90f), new Color(.58f,.60f,.60f), .025f, new Vector4(36,36,36,36));
        WriteShape(o, folder, "Key", 64, clipped, Ivory, new Color(.15f,.16f,.15f), .055f, new Vector4(20,20,20,20));
        WriteShape(o, folder, "HeartBackground", 256, heart, new Color(.15f,.025f,.035f), new Color(.08f,.09f,.10f), .055f, Vector4.zero);
        WriteShape(o, folder, "HeartFill", 256, Scale(heart,.86f), Color.white, Color.white, 0, Vector4.zero);
        WriteShape(o, folder, "HeartFrame", 256, heart, Color.clear, new Color(.75f,.77f,.76f), .035f, Vector4.zero);
        WriteShape(o, folder, "White", 16, new[]{Vector2.zero, Vector2.right, Vector2.one, Vector2.up}, Color.white, Color.white, 0, Vector4.zero);
    }
    private static Vector2[] Scale(Vector2[] points, float scale) => points.Select(p => Vector2.one * .5f + (p - Vector2.one * .5f) * scale).ToArray();
    private static bool Inside(Vector2 p, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
        }
        return inside;
    }
    private static void WriteShape(CapstoneUIBuildOptions o, string folder, string name, int size, Vector2[] outline, Color fill, Color edge, float inset, Vector4 border)
    {
        var inner = Scale(outline, 1 - 2 * inset);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            // 2x2 coverage for clean polygon edges; RGB is unpremultiplied for uGUI.
            Color sum = Color.clear; float coverage = 0;
            for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
            {
                var p = new Vector2((x + (sx + .5f) / 2) / size, (y + (sy + .5f) / 2) / size);
                if (!Inside(p, outline)) continue;
                Color c = inset > 0 && !Inside(p, inner) ? edge : fill;
                if (c.a <= 0) continue;
                sum.r += c.r * c.a; sum.g += c.g * c.a; sum.b += c.b * c.a; coverage += c.a;
            }
            pixels[y * size + x] = coverage > 0 ? new Color(sum.r / coverage, sum.g / coverage, sum.b / coverage, coverage / 4) : Color.clear;
        }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        try
        {
            tex.SetPixels(pixels); tex.Apply();
            string path = folder + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.spriteBorder = border;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            o.Shapes[name] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        finally { Object.DestroyImmediate(tex); }
    }
}
#endif
