#if UNITY_EDITOR
using System;
using System.Reflection;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LevelSelectUIBuilder
{
    private static string PrefabDir;
    private static CapstoneUIBuildOptions Options;
    private static CapstoneUIEditor.BuildScene BuildScene;
    private const int DifficultyCount = 3;

    private static readonly Color Border = new Color(.145f, .275f, .353f, 1f);
    private static readonly Color SlotBg = new Color(.078f, .172f, .231f, 1f);
    private static readonly Color Grey = new Color(.62f, .71f, .75f, 1f);
    private static readonly Color Orange = new Color(.94f, .50f, .16f, 1f);

    private static readonly Color Panel = new Color(.078f, .149f, .184f, .97f);
    private static readonly Color Dark = new Color(0.04f, 0.09f, 0.13f);
    private static readonly Color Core = new Color(0.078f, 0.149f, 0.184f, 1f);

    [MenuItem("Tools/Level/Build Level Select UI")]
    public static void Build() => CapstoneUIPrefabTool.OpenLevelSelect();

    public static GameObject BuildToFolder(CapstoneUIBuildOptions options, Sprite mapSprite = null)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        ValidateInputs();
        CapstoneUIEditor.ValidateAssetFolder(options.Output);
        if (options.Font == null) throw new InvalidOperationException("Assign a TMP Font Asset before building Level Select.");
        Options = options;
        PrefabDir = options.Output.TrimEnd('/') + "/LevelSelect";
        try
        {
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();
            RegisterLabelDefaults();
            using var scene = new CapstoneUIEditor.BuildScene();
            BuildScene = scene;
            var markerPrefab = BuildMarkerPrefab();
            var slotPrefab = BuildRewardSlotPrefab();
            var root = BuildCanvas(markerPrefab, slotPrefab, mapSprite);
            ValidateBuiltRoot(root);
            var saved = CapstoneUIEditor.Save(root, PrefabDir + "/LevelSelectCanvas.prefab");
            InventoryLocalizationTool.AddKeys();
            return saved;
        }
        finally { BuildScene = null; Options = null; PrefabDir = null; }
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject BuildMarkerPrefab()
    {
        var go = new GameObject("LevelMarker", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(LevelMarkerView));
        BuildScene.Own(go);
        ((RectTransform)go.transform).sizeDelta = new Vector2(250, 190);

        var background = go.GetComponent<Image>();
        background.color = new Color(0, 0, 0, 0);
        var button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.None;

        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // round markers
        var ring = Img("Ring", go.transform, Color.white);
        TopLeft(ring.rectTransform, 93, 50, 64, 64);
        ring.sprite = knob;
        var core = Img("Core", ring.transform, Core);
        Stretch(core.rectTransform, 7, 7, 7, 7);
        core.sprite = knob;
        Glyph("MapIcon", core.transform, "HUD_Map", 12, 12, 26, 26);

        var bracket = NewRect("Bracket", go.transform);
        Stretch(bracket);
        foreach (var right in new[] { false, true })
        {
            foreach (var bottom in new[] { false, true })
            {
                TopLeft(Img("H", bracket, Color.white).rectTransform, right ? 206 : 0, bottom ? 184 : 0, 44, 6);
                TopLeft(Img("V", bracket, Color.white).rectTransform, right ? 244 : 0, bottom ? 146 : 0, 6, 44);
            }
        }
        bracket.gameObject.SetActive(false);

        var label = Txt("Label", go.transform, "", 26, Color.white, TextAlignmentOptions.Center);
        TopLeft(label.rectTransform, 0, 128, 250, 32);
        var lockText = Txt("Lock", go.transform, "", 18, Grey, TextAlignmentOptions.Center);
        TopLeft(lockText.rectTransform, 0, 160, 250, 24);

        SetRefs(go.GetComponent<LevelMarkerView>(),
            ("ring", ring), ("bracket", bracket.gameObject), ("label", label), ("lockText", lockText),
            ("group", go.GetComponent<CanvasGroup>()));
        Fit(label, 18, 26);
        Fit(lockText, 12, 18);
        return Save(go, "LevelMarker");
    }

    private static GameObject BuildRewardSlotPrefab()
    {
        var go = new GameObject("RewardSlot", typeof(RectTransform), typeof(Image), typeof(RewardSlotView));
        BuildScene.Own(go);
        ((RectTransform)go.transform).sizeDelta = new Vector2(56, 56);

        var frame = go.GetComponent<Image>();
        frame.color = Border;
        frame.raycastTarget = true; // hover

        var highlight = Img("Highlight", go.transform, Color.white);
        Stretch(highlight.rectTransform, -3, -3, -3, -3);
        highlight.gameObject.SetActive(false);
        Stretch(Img("Inner", go.transform, SlotBg).rectTransform, 2, 2, 2, 2);
        var icon = Img("Icon", go.transform, Color.white);
        Stretch(icon.rectTransform, 11, 11, 11, 11);
        icon.preserveAspect = true;

        SetRefs(go.GetComponent<RewardSlotView>(), ("icon", icon), ("highlight", highlight.gameObject));
        return Save(go, "RewardSlot");
    }

    private static GameObject Save(GameObject go, string name)
    {
        string path = PrefabDir + "/" + name + ".prefab";
        CapstoneUIEditor.Save(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ------------------------------------------------------------------ canvas

    private static GameObject BuildCanvas(GameObject markerPrefab, GameObject slotPrefab, Sprite mapSprite)
    {
        var canvasGo = CapstoneUIEditor.Canvas("LevelSelectCanvas", 60);
        BuildScene.Own(canvasGo);

        var view = canvasGo.AddComponent<LevelSelectView>();
        var screen = NewRect("LevelSelectScreen", canvasGo.transform);
        Stretch(screen);

        // ---- map side
        var mapImage = Img("MapImage", screen, new Color(0.09f, 0.19f, 0.23f, 1f), true);
        Stretch(mapImage.rectTransform);
        if (mapSprite != null) { mapImage.sprite = mapSprite; mapImage.color = Color.white; }

        var markerRoot = NewRect("MarkerRoot", screen);
        markerRoot.anchorMin = new Vector2(820f / 1920f, 0f);
        markerRoot.anchorMax = Vector2.one;
        markerRoot.offsetMin = markerRoot.offsetMax = Vector2.zero;

        var best = Img("BestClear", screen, new Color(0.027f, 0.082f, 0.118f, 0.88f));
        TopLeft(best.rectTransform, 1500, 900, 340, 124);
        TopLeft(LTxt("Caption", best.transform, "LEVEL_BEST_CLEAR", 18, Grey).rectTransform, 22, 14, 300, 24);
        var bestBadge = Img("Badge", best.transform, new Color(0.66f, 0.32f, 0.05f, 1f));
        TopLeft(bestBadge.rectTransform, 22, 52, 150, 36);
        var bestBadgeText = Txt("Text", bestBadge.transform, "-", 20, Color.white, TextAlignmentOptions.Center);
        Stretch(bestBadgeText.rectTransform);
        var bestCount = Txt("Count", best.transform, "", 20, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
        TopLeft(bestCount.rectTransform, 186, 54, 150, 32);

        // ---- left panel
        var left = Img("LeftPanel", screen, Panel, true);
        TopLeft(left.rectTransform, 0, 0, 820, 1080);

        TopLeft(Img("PanelEdge", left.transform, Border).rectTransform, 818, 0, 2, 1080);
        var header = Img("Header", left.transform, Color.white);
        header.sprite = Options.Shape("Panel"); // Neutral preview until the runtime assigns the level art.
        TopLeft(header.rectTransform, 0, 0, 818, 188);
        TopLeft(Img("HeaderEdge", left.transform, Border).rectTransform, 0, 186, 818, 2);
        var badge = Img("Badge", left.transform, new Color(0.02f, 0.08f, 0.11f, 0.85f));
        TopLeft(badge.rectTransform, 30, 20, 130, 36);
        var badgeText = Txt("Text", badge.transform, "LEVEL 1", 18, Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform);

        var nameText = Txt("Name", left.transform, "FOREST", 62, Color.white);
        TopLeft(nameText.rectTransform, 40, 208, 740, 64);
        Fit(nameText, 36, 62);
        var description = Txt("Description", left.transform, "", 21, new Color(0.71f, 0.79f, 0.83f), TextAlignmentOptions.TopLeft, FontStyles.Normal);
        TopLeft(description.rectTransform, 40, 280, 740, 56);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.overflowMode = TextOverflowModes.Ellipsis;

        // difficulty row + track
        TopLeft(Badge("KeyA", left.transform, "A", 36, 32), 40, 354, 36, 32);
        TopLeft(LTxt("EasyLabel", left.transform, "LEVEL_EASY", 18, Grey).rectTransform, 84, 354, 120, 32);
        var selectedDifficulty = Txt("SelectedDifficulty", left.transform, "", 24, Orange, TextAlignmentOptions.Center);
        TopLeft(selectedDifficulty.rectTransform, 260, 350, 300, 40);
        TopLeft(LTxt("HardLabel", left.transform, "LEVEL_HARD", 18, Grey, TextAlignmentOptions.Right).rectTransform, 600, 354, 120, 32);
        TopLeft(Badge("KeyD", left.transform, "D", 36, 32), 730, 354, 36, 32);

        var track = NewRect("Track", left.transform);
        TopLeft(track, 130, 410, 560, 114);
        var line = Img("Line", track, Border);
        TopLeft(line.rectTransform, 0, 25, 560, 8);
        var fill = Img("Fill", line.transform, Orange);
        Stretch(fill.rectTransform);

        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var buttons = new Button[DifficultyCount];
        var rings = new Image[DifficultyCount];
        var labels = new TMP_Text[DifficultyCount];
        var hints = new TMP_Text[DifficultyCount];
        for (int i = 0; i < DifficultyCount; i++)
        {
            var node = Img("Difficulty_" + i, track, new Color(0, 0, 0, 0), true);
            TopLeft(node.rectTransform, i * 280 - 100, 0, 200, 110);
            buttons[i] = node.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = node;
            buttons[i].transition = Selectable.Transition.None;

            var ring = Img("Ring", node.transform, Color.white);
            ring.sprite = knob;
            ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            ring.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ring.rectTransform.anchoredPosition = new Vector2(0, -29);
            ring.rectTransform.sizeDelta = new Vector2(56, 56);
            var core = Img("Core", ring.transform, Core);
            core.sprite = knob;
            Stretch(core.rectTransform, 6, 6, 6, 6);
            rings[i] = ring;

            labels[i] = Txt("Label", node.transform, "", 17, Color.white, TextAlignmentOptions.Center);
            TopLeft(labels[i].rectTransform, 0, 68, 200, 26);
            hints[i] = Txt("Hint", node.transform, "", 15, Grey, TextAlignmentOptions.Center, FontStyles.Normal);
            TopLeft(hints[i].rectTransform, 0, 92, 200, 20);
        }

        // power and enemies
        Glyph("PowerIcon", left.transform, "UI_TotalStats", 40, 560, 28, 28);
        Glyph("EnemyIcon", left.transform, "UI_Sword", 42, 605, 22, 22);
        Glyph("LootIcon", left.transform, "UI_Backpack", 42, 639, 22, 22);

        var recommended = Txt("Recommended", left.transform, "", 23, Color.white);
        TopLeft(recommended.rectTransform, 80, 556, 450, 36);
        var enemies = Txt("Enemies", left.transform, "", 20, new Color(0.71f, 0.79f, 0.83f), TextAlignmentOptions.Left, FontStyles.Normal);
        TopLeft(enemies.rectTransform, 80, 600, 450, 30);
        var loot = Txt("Loot", left.transform, "", 20, new Color(0.71f, 0.79f, 0.83f), TextAlignmentOptions.Left, FontStyles.Normal);
        TopLeft(loot.rectTransform, 80, 634, 450, 30);

        TopLeft(LTxt("YourPowerCaption", left.transform, "LEVEL_YOUR_POWER", 18, Grey, TextAlignmentOptions.Right).rectTransform, 550, 556, 230, 26);
        var power = Txt("Power", left.transform, "0", 54, Color.white, TextAlignmentOptions.Right);
        TopLeft(power.rectTransform, 550, 588, 230, 64);
        var powerDelta = Txt("PowerDelta", left.transform, "", 20, Color.white, TextAlignmentOptions.Right);
        TopLeft(powerDelta.rectTransform, 550, 656, 230, 28);

        TopLeft(Img("Divider", left.transform, Border).rectTransform, 40, 700, 740, 2);

        // rewards
        var gearGroup = BuildRewardGroup(left.transform, "GearGroup", "LEVEL_GEAR_DROPS", "UI_Sword", 40, 718, 352, 202, out var gearRoot);
        var runeGroup = BuildRewardGroup(left.transform, "RuneGroup", "LEVEL_RUNES", "UI_Rune", 410, 718, 370, 96, out var runeRoot);
        var artifactGroup = BuildRewardGroup(left.transform, "ArtifactGroup", "LEVEL_ARTIFACTS", "UI_Artifact", 410, 824, 370, 96, out var artifactRoot);
        var rewardName = Txt("RewardName", left.transform, "", 18, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
        TopLeft(rewardName.rectTransform, 40, 932, 740, 24);

        // start
        var startImage = Img("StartButton", left.transform, Orange, true);
        TopLeft(startImage.rectTransform, 40, 974, 740, 70);
        var startButton = startImage.gameObject.AddComponent<Button>();
        startButton.targetGraphic = startImage;
        Stretch(LTxt("Label", startImage.transform, "LEVEL_START", 32, Dark, TextAlignmentOptions.Center).rectTransform);

        // back hint + toast
        TopLeft(Badge("BackKey", screen, "Esc", 70, 40), 860, 1010, 70, 40);
        TopLeft(LTxt("BackLabel", screen, "LEVEL_BACK", 24, Color.white).rectTransform, 945, 1010, 160, 40);
        var toast = Txt("Toast", screen, "", 24, new Color(1f, 0.82f, 0.48f), TextAlignmentOptions.Center);
        TopLeft(toast.rectTransform, 860, 940, 600, 44);
        toast.gameObject.SetActive(false);

        SetRefs(view,
            ("screenRoot", screen.gameObject),
            ("mapImage", mapImage), ("markerRoot", markerRoot), ("markerPrefab", markerPrefab.GetComponent<LevelMarkerView>()),
            ("headerImage", header), ("badgeText", badgeText), ("nameText", nameText), ("descriptionText", description),
            ("selectedDifficultyText", selectedDifficulty), ("trackFill", fill.rectTransform),
            ("recommendedText", recommended), ("enemyText", enemies), ("lootText", loot),
            ("powerText", power), ("powerDeltaText", powerDelta),
            ("rewardSlotPrefab", slotPrefab.GetComponent<RewardSlotView>()),
            ("gearGroup", gearGroup), ("gearRoot", gearRoot), ("runeGroup", runeGroup), ("runeRoot", runeRoot),
            ("artifactGroup", artifactGroup), ("artifactRoot", artifactRoot), ("rewardNameText", rewardName),
            ("startButton", startButton), ("bestBadgeText", bestBadgeText), ("bestCountText", bestCount), ("toastText", toast));
        SetArray(view, "difficultyButtons", buttons);
        SetArray(view, "difficultyRings", rings);
        SetArray(view, "difficultyLabels", labels);
        SetArray(view, "difficultyHints", hints);

        screen.gameObject.SetActive(false);
        return canvasGo;
    }

    // Keep the runtime grid root empty; the view owns its dynamically created slots.
    private static GameObject BuildRewardGroup(Transform parent, string name, string captionKey, string iconName,
        float x, float y, float w, float h, out Transform gridRoot)
    {
        var group = NewRect(name, parent);
        TopLeft(group, x, y, w, h);
        Glyph("CategoryIcon", group, iconName, 0, 0, 24, 24);
        TopLeft(LTxt("Caption", group, captionKey, 18, Grey).rectTransform, 32, 0, w - 32, 24);

        var scrollRoot = NewRect("Scroll", group);
        TopLeft(scrollRoot, 0, 32, w, h - 32);
        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        var viewport = Img("Viewport", scrollRoot, Color.clear, true);
        Stretch(viewport.rectTransform);
        viewport.gameObject.AddComponent<RectMask2D>();
        var grid = NewRect("Grid", viewport.transform);
        grid.anchorMin = new Vector2(0, 1);
        grid.anchorMax = Vector2.one;
        grid.pivot = new Vector2(.5f, 1);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = Vector2.zero;
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(56, 56);
        layout.spacing = new Vector2(10, 10);
        layout.padding = new RectOffset(4, 4, 4, 4); // Space for the 3px hover frame inside the mask.
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = grid;
        scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;

        gridRoot = grid;
        return group.gameObject;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = CapstoneUIEditor.Rect(name, parent, 0, 0, 100, 100);
        // Match the old builder's centered default, especially MarkerRoot and selection brackets.
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }
    private static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h) => CapstoneUIEditor.Place(rt, x, y, w, h);
    private static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }
    private static Image Img(string name, Transform parent, Color color, bool raycast = false)
    {
        var image = CapstoneUIEditor.Image(name, parent, 0, 0, 100, 100, color, null, raycast);
        image.rectTransform.pivot = new Vector2(.5f, .5f);
        return image;
    }
    private static void Glyph(string name, Transform parent, string iconName, float x, float y, float w, float h)
    {
        var sprite = Options.Icon(iconName);
        if (sprite == null) throw new InvalidOperationException("Missing shared UI icon: " + iconName);
        CapstoneUIEditor.Icon(name, parent, x, y, w, h, sprite);
    }
    private static TextMeshProUGUI Txt(string name, Transform parent, string value, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold)
    {
        var text = CapstoneUIEditor.Text(name, parent, 0, 0, 100, 100, value, size, color, Options, align);
        text.rectTransform.pivot = new Vector2(.5f, .5f);
        text.fontStyle = style;
        Fit(text, size * .8f, size);
        return text;
    }
    private static void Fit(TMP_Text text, float min, float max)
    {
        text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }
    private static TextMeshProUGUI LTxt(string name, Transform parent, string key, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold)
    {
        var text = Txt(name, parent, InventoryText.Defaults.TryGetValue(key, out var value) ? value : key, size, color, align, style);
        var loc = text.gameObject.AddComponent<LocalizationText>();
        CapstoneUIEditor.Ref(loc, "text", text);
        typeof(LocalizationText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .SetValue(loc, InventoryText.Ref(key));
        EditorUtility.SetDirty(loc);
        return text;
    }
    private static RectTransform Badge(string name, Transform parent, string label, float w, float h)
    {
        var image = Img(name, parent, Color.white);
        image.sprite = Options.Shape("Key");
        image.type = Image.Type.Sliced;
        image.rectTransform.sizeDelta = new Vector2(w, h);
        Stretch(Txt("Label", image.transform, label, h * .5f, Dark, TextAlignmentOptions.Center).rectTransform);
        return image.rectTransform;
    }
    private static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        foreach (var pair in refs) CapstoneUIEditor.Ref(target, pair.field, pair.value);
    }
    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field);
        if (property == null || !property.isArray) throw new InvalidOperationException("Missing serialized array: " + field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Only add missing defaults; preserve the project's existing keys and translations.
    private static void RegisterLabelDefaults()
    {
        var labels = new (string key, string value)[]
        {
            ("LEVEL_BEST_CLEAR", "BEST CLEAR"), ("LEVEL_EASY", "EASY"), ("LEVEL_HARD", "HARD"),
            ("LEVEL_YOUR_POWER", "YOUR POWER"), ("LEVEL_GEAR_DROPS", "GEAR DROPS"),
            ("LEVEL_RUNES", "RUNES"), ("LEVEL_ARTIFACTS", "ARTIFACTS"),
            ("LEVEL_START", "START MISSION"), ("LEVEL_BACK", "Back")
        };
        foreach (var pair in labels)
            if (!InventoryText.Defaults.ContainsKey(pair.key)) InventoryText.Defaults.Add(pair.key, pair.value);
    }

    private static readonly string[] ViewReferences =
    {
        "screenRoot", "mapImage", "markerRoot", "markerPrefab", "headerImage", "badgeText", "nameText", "descriptionText",
        "selectedDifficultyText", "trackFill", "recommendedText", "enemyText", "lootText", "powerText", "powerDeltaText",
        "rewardSlotPrefab", "gearGroup", "gearRoot", "runeGroup", "runeRoot", "artifactGroup", "artifactRoot",
        "rewardNameText", "startButton", "bestBadgeText", "bestCountText", "toastText"
    };
    private static readonly string[] DifficultyArrays = { "difficultyButtons", "difficultyRings", "difficultyLabels", "difficultyHints" };
    private static readonly string[] MarkerReferences = { "ring", "bracket", "label", "lockText", "group" };
    private static readonly string[] SlotReferences = { "icon", "highlight" };

    public static void ValidateInputs()
    {
        foreach (string field in ViewReferences) CapstoneUIEditor.RequireField(typeof(LevelSelectView), field, null);
        foreach (string field in DifficultyArrays) CapstoneUIEditor.RequireField(typeof(LevelSelectView), field, null);
        foreach (string field in MarkerReferences) CapstoneUIEditor.RequireField(typeof(LevelMarkerView), field, null);
        foreach (string field in SlotReferences) CapstoneUIEditor.RequireField(typeof(RewardSlotView), field, null);
        CapstoneUIEditor.RequireField(typeof(LocalizationText), "text", null);
        CapstoneUIEditor.RequireField(typeof(LocalizationText), "localizedString", null);
    }

    [MenuItem("Tools/Level/Validate Selected Level Select Prefab")]
    public static void ValidateSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) throw new InvalidOperationException("Select the LevelSelectCanvas root first.");
        string path = AssetDatabase.GetAssetPath(selected);
        bool load = path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        var root = load ? PrefabUtility.LoadPrefabContents(path) : selected;
        try
        {
            ValidateBuiltRoot(root);
            Debug.Log("[Level] Prefab references are valid. Check selection, difficulty, rewards and start/back flow in Play Mode.", selected);
        }
        finally { if (load) PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ValidateBuiltRoot(GameObject root)
    {
        var view = root.GetComponent<LevelSelectView>();
        if (view == null) throw new InvalidOperationException("Select a canvas root containing LevelSelectView.");
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
                throw new InvalidOperationException("Missing script on " + node.name);
        ValidateReferences(view, ViewReferences);
        var so = new SerializedObject(view);
        foreach (string field in DifficultyArrays)
        {
            var array = so.FindProperty(field);
            if (array == null || !array.isArray || array.arraySize != DifficultyCount)
                throw new InvalidOperationException("Level Select needs 3 difficulty references: " + field);
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    throw new InvalidOperationException("Empty difficulty reference: " + field + "[" + i + "]");
        }
        ValidateReferences(so.FindProperty("markerPrefab").objectReferenceValue, MarkerReferences);
        ValidateReferences(so.FindProperty("rewardSlotPrefab").objectReferenceValue, SlotReferences);
        if (!root.activeSelf) throw new InvalidOperationException("Keep LevelSelectCanvas active so its view can receive open events.");
    }

    private static void ValidateReferences(Object target, string[] fields)
    {
        var so = new SerializedObject(target);
        foreach (string field in fields)
        {
            var property = so.FindProperty(field);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                throw new InvalidOperationException("Missing UI reference: " + target.GetType().Name + "." + field);
        }
    }
}
#endif
