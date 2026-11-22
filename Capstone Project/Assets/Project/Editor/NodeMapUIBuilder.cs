#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CapstoneMenuUI;

public static class NodeMapUIBuilder
{
    private const string StylePath = "Assets/UI/NodeMap/NodeMapStyle.asset";
    private static readonly NodeType[] LegendTypes =
        { NodeType.Combat, NodeType.Elite, NodeType.Reward, NodeType.Event, NodeType.Rest, NodeType.Shop, NodeType.Boss };

    private static readonly Color Soft = new Color(0.71f, 0.79f, 0.83f);
    private static readonly Color DarkText = new Color(0.04f, 0.086f, 0.125f);
    private static readonly Color Line = new Color(0.1f, 0.2f, 0.26f, 1f);

    [MenuItem("Tools/Map/Build Node Map UI")]
    public static void Build() => CapstoneUIPrefabTool.OpenNodeMap();

    public static void ValidateInputs(NodeMapStyle style = null, ControlsConfig controls = null)
    {
        ValidateLocalization();
        RequireFields<NodeMapView>("screenRoot", "mapArea", "edgeLayer", "nodeLayer", "nodePrefab", "columnLabelPrefab",
            "style", "subtitleText", "progressText", "infoDot", "infoName", "infoTagRoot", "infoTagBackground", "infoTagText",
            "infoDescription", "rowHealth", "healthValue", "rowDamage", "damageValue", "rowLoot", "lootValue");
        RequireFields<NodeMapNodeView>("ring", "body", "icon", "halo", "selectRing", "glyph", "check", "hereTag", "hereText", "group");
        SettingsUIBuilder.ValidateControls(controls);
        var existing = ExistingConfig(style, StylePath);
        if (existing != null)
            foreach (var type in LegendTypes) NodeColor(existing, type);
    }

    public static GameObject BuildToFolder(CapstoneUIBuildOptions options, NodeMapStyle style = null, ControlsConfig controls = null)
    {
        ValidateInputs(style, controls);
        using (var ui = new CapstoneMenuUI(options, "NodeMap"))
        {
            style = ResolveConfig(style, StylePath);
            controls = ResolveConfig(controls, SettingsUIBuilder.ControlsPath);
            var nodePrefab = BuildNodePrefab(ui);
            var labelPrefab = BuildColumnLabelPrefab(ui);
            var canvas = ui.Save(BuildCanvas(ui, nodePrefab, labelPrefab, style));
            AddControlsRow(controls);
            return canvas;
        }
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject BuildNodePrefab(CapstoneMenuUI ui)
    {
        var go = ui.Root("MapNode", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(NodeMapNodeView));
        ((RectTransform)go.transform).sizeDelta = new Vector2(72, 72);

        var hit = go.GetComponent<Image>(); // invisible, receives the mouse
        hit.color = new Color(0, 0, 0, 0);
        hit.raycastTarget = true;
        var button = go.GetComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;

        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var select = Box("Select", go.transform, Orange);
        select.sprite = knob;
        Stretch(select.rectTransform, -8, -8, -8, -8);
        var halo = Box("Halo", go.transform, new Color(1, 1, 1, 0.18f));
        halo.sprite = knob;
        Stretch(halo.rectTransform, -10, -10, -10, -10);
        var ring = Box("Ring", go.transform, Color.white);
        ring.sprite = knob;
        Stretch(ring.rectTransform);
        var body = Box("Body", go.transform, new Color(0.05f, 0.13f, 0.19f));
        body.sprite = knob;
        Stretch(body.rectTransform, 4, 4, 4, 4);

        var icon = Box("Icon", go.transform, Color.white);
        icon.preserveAspect = true;
        icon.enabled = false;
        Stretch(icon.rectTransform, 18, 18, 18, 18);
        var glyph = ui.Text("Glyph", go.transform, "X", 30, Color.white, TextAlignmentOptions.Center);
        Stretch(glyph.rectTransform);

        var check = Box("Check", go.transform, Orange);
        check.sprite = knob;
        check.rectTransform.anchorMin = check.rectTransform.anchorMax = new Vector2(1, 1);
        check.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        check.rectTransform.anchoredPosition = new Vector2(-2, -2);
        check.rectTransform.sizeDelta = new Vector2(28, 28);
        var checkText = ui.Text("Text", check.transform, "OK", 11, DarkText, TextAlignmentOptions.Center);
        Stretch(checkText.rectTransform);

        var here = Box("HereTag", go.transform, Color.white);
        here.rectTransform.anchorMin = here.rectTransform.anchorMax = new Vector2(0.5f, 1);
        here.rectTransform.pivot = new Vector2(0.5f, 0);
        here.rectTransform.anchoredPosition = new Vector2(0, 16);
        here.rectTransform.sizeDelta = new Vector2(150, 28);
        var hereText = ui.Text("Text", here.transform, "YOU ARE HERE", 15, DarkText, TextAlignmentOptions.Center);
        Stretch(hereText.rectTransform);

        SetRefs(go.GetComponent<NodeMapNodeView>(),
            ("ring", ring), ("body", body), ("icon", icon), ("halo", halo), ("selectRing", select), ("glyph", glyph),
            ("check", check.gameObject), ("hereTag", here.gameObject), ("hereText", hereText), ("group", go.GetComponent<CanvasGroup>()));

        select.gameObject.SetActive(false);
        halo.gameObject.SetActive(false);
        check.gameObject.SetActive(false);
        here.gameObject.SetActive(false);
        return ui.Save(go);
    }

    private static GameObject BuildColumnLabelPrefab(CapstoneMenuUI ui)
    {
        var label = ui.Text("ColumnLabel", null, "1", 16, new Color(0.44f, 0.53f, 0.59f), TextAlignmentOptions.Center);
        label.rectTransform.sizeDelta = new Vector2(120, 24);
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        return ui.Save(label.gameObject);
    }

    // ------------------------------------------------------------------ canvas

    private static GameObject BuildCanvas(CapstoneMenuUI ui, GameObject nodePrefab, GameObject labelPrefab, NodeMapStyle style)
    {
        var canvasGo = ui.Canvas("NodeMapCanvas", 260);
        var view = canvasGo.AddComponent<NodeMapView>();
        var screen = CreateRect("NodeMapScreen", canvasGo.transform);
        Stretch(screen);
        Stretch(Box("Background", screen, new Color(0.016f, 0.047f, 0.07f, 0.93f), true).rectTransform);

        // ---- header
        var subtitle = ui.Text("Subtitle", screen, "", 18, Grey);
        Place(subtitle.rectTransform, 60, 34, 900, 24);
        CapstoneUIEditor.Icon("MapIcon", screen, 60, 74, 42, 42, ui.Options.Icon("HUD_Map"));
        Place(ui.LocalizedText("Title", screen, "NODEMAP_TITLE", 54, Color.white).rectTransform, 118, 60, 842, 70);
        Place(ui.LocalizedText("ProgressCaption", screen, "NODEMAP_PROGRESS", 18, Grey, TextAlignmentOptions.Right).rectTransform, 1520, 44, 340, 24);
        var progress = ui.Text("Progress", screen, "", 34, Color.white, TextAlignmentOptions.Right);
        Place(progress.rectTransform, 1520, 70, 340, 48);

        // ---- map area (edges below, nodes above)
        var mapArea = CreateRect("MapArea", screen);
        Place(mapArea, 60, 200, 1400, 780);
        var edgeLayer = CreateRect("Edges", mapArea);
        Stretch(edgeLayer);
        var nodeLayer = CreateRect("Nodes", mapArea);
        Stretch(nodeLayer);

        // ---- selected node panel
        var info = Box("InfoPanel", screen, Panel, true);
        Place(info.rectTransform, 1520, 170, 340, 540);
        Place(ui.LocalizedText("Caption", info.transform, "NODEMAP_SELECTED", 18, Grey).rectTransform, 30, 26, 280, 24);

        var dot = Box("Dot", info.transform, Color.white);
        dot.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Place(dot.rectTransform, 30, 78, 20, 20);
        var infoName = ui.Text("Name", info.transform, "", 32, Color.white);
        Place(infoName.rectTransform, 62, 62, 250, 48);

        var tag = Box("Tag", info.transform, Color.white);
        Place(tag.rectTransform, 30, 124, 170, 30);
        var tagText = ui.Text("Text", tag.transform, "", 15, DarkText, TextAlignmentOptions.Center);
        Stretch(tagText.rectTransform);

        var description = ui.Text("Description", info.transform, "", 20, Soft, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        Place(description.rectTransform, 30, 172, 280, 150);

        var rowHealth = InfoRow(ui, info.transform, "NODEMAP_ENEMY_HP", 332, out var healthValue);
        var rowDamage = InfoRow(ui, info.transform, "NODEMAP_ENEMY_DMG", 386, out var damageValue);
        var rowLoot = InfoRow(ui, info.transform, "NODEMAP_LOOT", 440, out var lootValue);
        lootValue.color = new Color(1f, 0.82f, 0.48f);

        // ---- how it works
        var how = Box("HowPanel", screen, Panel, true);
        Place(how.rectTransform, 1520, 730, 340, 210);
        Place(ui.LocalizedText("Caption", how.transform, "NODEMAP_HOW_CAPTION", 18, Grey).rectTransform, 30, 22, 280, 24);
        Place(ui.LocalizedText("Text", how.transform, "NODEMAP_HOW", 19, Soft, TextAlignmentOptions.TopLeft, FontStyles.Normal).rectTransform, 30, 56, 280, 140);

        // ---- legend (colors come from the style asset)
        var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        for (int i = 0; i < LegendTypes.Length; i++)
        {
            float x = 60 + i * 190;
            var legendDot = Box("LegendDot_" + LegendTypes[i], screen, NodeColor(style, LegendTypes[i]));
            legendDot.sprite = knob;
            Place(legendDot.rectTransform, x, 1021, 18, 18);
            Place(ui.LocalizedText("LegendLabel_" + LegendTypes[i], screen, "NODE_" + LegendTypes[i].ToString().ToUpperInvariant(), 21, Color.white,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal).rectTransform, x + 28, 1010, 150, 40);
        }

        // ---- close hint
        Place(ui.KeyBadge("KeyM", screen, "M", 40, 40), 1640, 1010, 40, 40);
        Place(ui.KeyBadge("KeyEsc", screen, "Esc", 70, 40), 1690, 1010, 70, 40);
        Place(ui.LocalizedText("CloseLabel", screen, "NODEMAP_CLOSE", 24, Color.white).rectTransform, 1776, 1010, 100, 40);

        SetRefs(view,
            ("screenRoot", screen.gameObject), ("mapArea", mapArea), ("edgeLayer", edgeLayer), ("nodeLayer", nodeLayer),
            ("nodePrefab", nodePrefab.GetComponent<NodeMapNodeView>()), ("columnLabelPrefab", labelPrefab.GetComponent<TMP_Text>()),
            ("style", style), ("subtitleText", subtitle), ("progressText", progress),
            ("infoDot", dot), ("infoName", infoName), ("infoTagRoot", tag.gameObject), ("infoTagBackground", tag), ("infoTagText", tagText),
            ("infoDescription", description),
            ("rowHealth", rowHealth), ("healthValue", healthValue), ("rowDamage", rowDamage), ("damageValue", damageValue),
            ("rowLoot", rowLoot), ("lootValue", lootValue));

        return canvasGo;
    }

    // "label ........ value" with a line under it
    private static GameObject InfoRow(CapstoneMenuUI ui, Transform parent, string labelKey, float y, out TMP_Text value)
    {
        var row = CreateRect("Row_" + labelKey, parent);
        Place(row, 30, y, 280, 52);
        Place(ui.LocalizedText("Label", row, labelKey, 21, Soft, TextAlignmentOptions.MidlineLeft, FontStyles.Normal).rectTransform, 0, 0, 162, 44);
        value = ui.Text("Value", row, "-", 22, Color.white, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, 170, 0, 110, 44);
        Place(Box("Line", row, Line).rectTransform, 0, 48, 280, 2);
        return row.gameObject;
    }

    private static Color NodeColor(NodeMapStyle style, NodeType type)
    {
        var appearance = style.Get(type);
        if (object.ReferenceEquals(appearance, null))
            throw new System.InvalidOperationException($"NodeMapStyle '{style.name}' has no entry for {type}.");
        return appearance.color;
    }

    private static void AddControlsRow(ControlsConfig controls)
    {
        const string action = "Player/ToggleNodeMap";
        LabelDefault("CTRL_NODEMAP");
        var rows = controls.rows;
        if (rows == null)
            throw new System.InvalidOperationException($"ControlsConfig '{controls.name}' has no rows list.");

        if (rows.Exists(row => !object.ReferenceEquals(row, null) && row.actionPath == action)) return;
        int previous = rows.FindIndex(row => !object.ReferenceEquals(row, null) && row.actionPath == "Player/ToggleMinimap");
        Undo.RecordObject(controls, "Add Node Map control");
        rows.Insert(previous < 0 ? rows.Count : previous + 1,
            new ControlsConfig.Row { labelKey = "CTRL_NODEMAP", actionPath = action, section = ControlSection.Movement });
        EditorUtility.SetDirty(controls);
        AssetDatabase.SaveAssetIfDirty(controls);
    }
}
#endif
