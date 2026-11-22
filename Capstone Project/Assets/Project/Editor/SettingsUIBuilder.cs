#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CapstoneMenuUI;

public static class SettingsUIBuilder
{
    internal const string ControlsPath = "Assets/Settings/ControlsConfig.asset";

    private static readonly Color RowInner = new Color(0.027f, 0.082f, 0.118f, 0.80f);
    private static readonly Color Soft = new Color(0.71f, 0.79f, 0.83f);

    [MenuItem("Tools/Settings/Build Settings UI")]
    public static void Build() => CapstoneUIPrefabTool.OpenSettings();

    public static void ValidateInputs(ControlsConfig controls = null)
    {
        ValidateLocalization();
        RequireFields<SettingsScreen>("screenRoot", "scroll", "content", "rowPrefab", "sectionPrefab",
            "descTitle", "descText", "toastText", "backButton", "resetButton", "applyButton", "controlsConfig",
            "tabButtons", "tabUnderlines");
        RequireFields<SettingRowView>("frame", "label", "selectorRoot", "prevButton", "nextButton", "selectorValue",
            "toggleRoot", "toggleButton", "toggleState", "pill", "knob", "sliderRoot", "slider", "sliderValue",
            "keyRoot", "keyButton", "keyText", "buttonRoot", "actionButton", "actionLabel");
        ValidateControls(controls);
    }

    internal static void ValidateControls(ControlsConfig selected)
    {
        var config = ExistingConfig(selected, ControlsPath);
        if (config != null && config.rows == null)
            throw new System.InvalidOperationException($"Initialize the ControlsConfig rows in {AssetDatabase.GetAssetPath(config)}.");
    }

    public static GameObject BuildToFolder(CapstoneUIBuildOptions options, ControlsConfig controls = null)
    {
        ValidateInputs(controls);
        using (var ui = new CapstoneMenuUI(options, "Settings"))
        {
            controls = ResolveConfig(controls, ControlsPath);
            var rowPrefab = BuildRowPrefab(ui);
            var sectionPrefab = BuildSectionPrefab(ui);
            return ui.Save(BuildCanvas(ui, rowPrefab, sectionPrefab, controls));
        }
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject BuildSectionPrefab(CapstoneMenuUI ui)
    {
        var text = ui.Text("SettingSection", null, "SECTION", 19, Grey);
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.margin = new Vector4(8, 0, 0, 8);
        PreferredHeight(text.gameObject, 52);
        return ui.Save(text.gameObject);
    }

    private static GameObject BuildRowPrefab(CapstoneMenuUI ui)
    {
        var go = ui.Root("SettingRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SettingRowView));
        ((RectTransform)go.transform).sizeDelta = new Vector2(1240, 60);
        PreferredHeight(go, 60);

        var frame = go.GetComponent<Image>(); // white border while focused (alpha 0 otherwise)
        frame.color = new Color(1, 1, 1, 0);
        frame.raycastTarget = true;
        go.GetComponent<Button>().targetGraphic = frame;

        Stretch(Box("Inner", go.transform, RowInner).rectTransform, 2, 2, 2, 2);

        var label = ui.Text("Label", go.transform, "Setting", 24, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
        Place(label.rectTransform, 24, 0, 680, 60);

        // selector
        var selector = Group(go.transform, "Selector");
        var prev = ArrowButton(ui, selector, "Prev", "<", 0);
        var selectorValue = ui.Text("Value", selector, "", 22, Color.white, TextAlignmentOptions.Center);
        Place(selectorValue.rectTransform, 52, 0, 376, 44);
        var next = ArrowButton(ui, selector, "Next", ">", 436);

        // toggle
        var toggle = Group(go.transform, "Toggle");
        var toggleHit = toggle.gameObject.AddComponent<Image>();
        toggleHit.color = new Color(0, 0, 0, 0);
        var toggleButton = toggle.gameObject.AddComponent<Button>();
        toggleButton.targetGraphic = toggleHit;
        toggleButton.transition = Selectable.Transition.None;
        var state = ui.Text("State", toggle, "OFF", 20, Grey, TextAlignmentOptions.MidlineRight);
        Place(state.rectTransform, 250, 0, 110, 44);
        var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var pill = Box("Pill", toggle, Border);
        pill.sprite = knobSprite;
        Place(pill.rectTransform, 380, 2, 100, 40);
        var knob = Box("Knob", pill.transform, Color.white);
        knob.sprite = knobSprite;
        knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0, 0.5f);
        knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        knob.rectTransform.sizeDelta = new Vector2(32, 32);
        knob.rectTransform.anchoredPosition = new Vector2(18, 0);

        // slider
        var sliderRoot = Group(go.transform, "SliderGroup");
        var slider = BuildSlider(sliderRoot);
        var sliderValue = ui.Text("Value", sliderRoot, "", 22, Color.white, TextAlignmentOptions.MidlineRight);
        Place(sliderValue.rectTransform, 410, 0, 70, 44);

        // key
        var keyRoot = Group(go.transform, "Key");
        var keyBox = Box("KeyButton", keyRoot, Border, true);
        Place(keyBox.rectTransform, 200, 0, 280, 44);
        var keyButton = keyBox.gameObject.AddComponent<Button>();
        keyButton.targetGraphic = keyBox;
        var keyText = ui.Text("Text", keyBox.transform, "-", 22, Color.white, TextAlignmentOptions.Center);
        Stretch(keyText.rectTransform);

        // button
        var buttonRoot = Group(go.transform, "ButtonGroup");
        var actionBox = Box("ActionButton", buttonRoot, Border, true);
        Place(actionBox.rectTransform, 280, 0, 200, 44);
        var actionButton = actionBox.gameObject.AddComponent<Button>();
        actionButton.targetGraphic = actionBox;
        var actionLabel = ui.Text("Label", actionBox.transform, "", 20, Color.white, TextAlignmentOptions.Center);
        Stretch(actionLabel.rectTransform);

        SetRefs(go.GetComponent<SettingRowView>(),
            ("frame", frame), ("label", label),
            ("selectorRoot", selector.gameObject), ("prevButton", prev), ("nextButton", next), ("selectorValue", selectorValue),
            ("toggleRoot", toggle.gameObject), ("toggleButton", toggleButton), ("toggleState", state), ("pill", pill), ("knob", knob.rectTransform),
            ("sliderRoot", sliderRoot.gameObject), ("slider", slider), ("sliderValue", sliderValue),
            ("keyRoot", keyRoot.gameObject), ("keyButton", keyButton), ("keyText", keyText),
            ("buttonRoot", buttonRoot.gameObject), ("actionButton", actionButton), ("actionLabel", actionLabel));

        selector.gameObject.SetActive(false);
        toggle.gameObject.SetActive(false);
        sliderRoot.gameObject.SetActive(false);
        keyRoot.gameObject.SetActive(false);
        buttonRoot.gameObject.SetActive(false);
        return ui.Save(go);
    }

    // the control area at the right end of a row
    private static RectTransform Group(Transform parent, string name)
    {
        var rt = CreateRect(name, parent);
        Place(rt, 740, 8, 480, 44);
        return rt;
    }

    private static Button ArrowButton(CapstoneMenuUI ui, Transform parent, string name, string symbol, float x)
    {
        var image = Box(name, parent, Border, true);
        Place(image.rectTransform, x, 0, 44, 44);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var text = ui.Text("Symbol", image.transform, symbol, 26, Color.white, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        return button;
    }

    private static Slider BuildSlider(Transform parent)
    {
        var root = CreateRect("Slider", parent);
        Place(root, 0, 0, 400, 44);
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        hit.raycastTarget = true;
        var slider = root.gameObject.AddComponent<Slider>();

        var background = Box("Background", root, Border);
        background.rectTransform.anchorMin = new Vector2(0, 0.5f);
        background.rectTransform.anchorMax = new Vector2(1, 0.5f);
        background.rectTransform.offsetMin = new Vector2(0, -4);
        background.rectTransform.offsetMax = new Vector2(0, 4);

        var fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0, 0.5f);
        fillArea.anchorMax = new Vector2(1, 0.5f);
        fillArea.offsetMin = new Vector2(5, -4);
        fillArea.offsetMax = new Vector2(-5, 4);
        var fill = Box("Fill", fillArea, Orange);
        Stretch(fill.rectTransform);

        var handleArea = CreateRect("Handle Slide Area", root);
        handleArea.anchorMin = new Vector2(0, 0.5f);
        handleArea.anchorMax = new Vector2(1, 0.5f);
        handleArea.offsetMin = new Vector2(10, -16);
        handleArea.offsetMax = new Vector2(-10, 16);
        var handle = Box("Handle", handleArea, Color.white, true);
        handle.rectTransform.anchorMin = new Vector2(0, 0);
        handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.sizeDelta = new Vector2(16, 0);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.wholeNumbers = true;
        return slider;
    }

    // ------------------------------------------------------------------ canvas

    private static GameObject BuildCanvas(CapstoneMenuUI ui, GameObject rowPrefab, GameObject sectionPrefab, ControlsConfig controls)
    {
        var canvasGo = ui.Canvas("SettingsCanvas", 300);
        var view = canvasGo.AddComponent<SettingsScreen>();
        var screen = CreateRect("SettingsScreenRoot", canvasGo.transform);
        Stretch(screen);
        Stretch(Box("Background", screen, Bg, true).rectTransform);

        // ---- top bar: title, Q, tabs, E
        var topBar = Box("TopBar", screen, TopBarColor);
        Place(topBar.rectTransform, 0, 0, 1920, 76);
        Place(ui.LocalizedText("Title", topBar.transform, "SET_TITLE", 36, Color.white).rectTransform, 60, 16, 400, 46);
        Place(ui.KeyBadge("KeyQ", topBar.transform, "Q", 40, 40), 520, 18, 40, 40);
        Place(ui.KeyBadge("KeyE", topBar.transform, "E", 40, 40), 1360, 18, 40, 40);

        string[] tabKeys = { "SET_TAB_GENERAL", "SET_TAB_VIDEO", "SET_TAB_AUDIO", "SET_TAB_CONTROLS" };
        var tabButtons = new Button[4];
        var underlines = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            var hit = Box("Tab_" + i, topBar.transform, new Color(0, 0, 0, 0), true);
            Place(hit.rectTransform, 580 + 190 * i, 0, 190, 76);
            tabButtons[i] = hit.gameObject.AddComponent<Button>();
            tabButtons[i].targetGraphic = hit;
            tabButtons[i].transition = Selectable.Transition.None;

            Stretch(ui.LocalizedText("Label", hit.transform, tabKeys[i], 24, Color.white, TextAlignmentOptions.Center).rectTransform);
            var underline = Box("Underline", hit.transform, Orange);
            Place(underline.rectTransform, 0, 72, 190, 4);
            underlines[i] = underline.gameObject;
        }

        // ---- option list
        var scrollRoot = Box("OptionScroll", screen, new Color(0, 0, 0, 0), true);
        Place(scrollRoot.rectTransform, 60, 110, 1240, 850);
        var viewport = CreateRect("Viewport", scrollRoot.transform);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        VerticalList(content.gameObject, 8);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;

        // ---- description panel
        var panel = Box("DescriptionPanel", screen, Panel, true);
        Place(panel.rectTransform, 1360, 110, 500, 360);
        Place(ui.LocalizedText("Caption", panel.transform, "SET_SELECTED", 18, Grey).rectTransform, 24, 20, 400, 24);
        var descTitle = ui.Text("Title", panel.transform, "", 32, Color.white);
        Place(descTitle.rectTransform, 24, 52, 452, 90);
        var descText = ui.Text("Text", panel.transform, "", 21, Soft, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        Place(descText.rectTransform, 24, 150, 452, 190);

        // ---- bottom hints
        var back = HintButton(ui, screen, "BackButton", "Esc", 70, "SET_BACK", 60, 1004, 260);
        var reset = HintButton(ui, screen, "ResetButton", "R", 40, "SET_RESET", 1330, 1004, 300);
        var apply = HintButton(ui, screen, "ApplyButton", "Enter", 90, "SET_APPLY", 1650, 1004, 230);

        var toast = ui.Text("Toast", screen, "", 26, new Color(1f, 0.82f, 0.48f), TextAlignmentOptions.Center);
        Place(toast.rectTransform, 360, 960, 1200, 44);
        toast.gameObject.SetActive(false);

        SetRefs(view,
            ("screenRoot", screen.gameObject), ("scroll", scroll), ("content", content),
            ("rowPrefab", rowPrefab.GetComponent<SettingRowView>()), ("sectionPrefab", sectionPrefab.GetComponent<TMP_Text>()),
            ("descTitle", descTitle), ("descText", descText), ("toastText", toast),
            ("backButton", back), ("resetButton", reset), ("applyButton", apply), ("controlsConfig", controls));
        SetArray(view, "tabButtons", tabButtons);
        SetArray(view, "tabUnderlines", underlines);

        return canvasGo;
    }

    // clickable "[key] label" at the bottom of the screen
    private static Button HintButton(CapstoneMenuUI ui, Transform parent, string name, string key, float keyWidth, string labelKey, float x, float y, float width)
    {
        var hit = Box(name, parent, new Color(0, 0, 0, 0), true);
        Place(hit.rectTransform, x, y, width, 52);
        var button = hit.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        Place(ui.KeyBadge("Key", hit.transform, key, keyWidth, 40), 0, 6, keyWidth, 40);
        Place(ui.LocalizedText("Label", hit.transform, labelKey, 24, Color.white).rectTransform, keyWidth + 16, 6, width - keyWidth - 16, 40);
        return button;
    }
}
#endif
