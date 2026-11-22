#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static CapstoneMenuUI;

public static class PauseMenuBuilder
{
    private const string BlurShaderPath = "Assets/Shaders/UI/UIBlurBackdrop.shader";

    private static readonly Color Shade = new Color(0.016f, 0.047f, 0.07f, 1f);
    private static readonly Color PanelColor = new Color(0.027f, 0.082f, 0.118f, 0.74f);
    private static readonly Color Soft = new Color(0.71f, 0.79f, 0.83f);
    private static readonly Color Line = new Color(0.1f, 0.2f, 0.26f, 1f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.48f);

    [MenuItem("Tools/Pause/Build Pause Menu")]
    public static void Build() => CapstoneUIPrefabTool.OpenPauseMenu();

    public static void ValidateInputs()
    {
        ValidateLocalization();
        RequireFields<PauseMenuController>("screenRoot", "runPanel", "lobbyPanel", "confirmRoot", "blur", "subtitleText",
            "resumeButton", "settingsButton", "routeMapButton", "abandonButton", "quitButton", "keepPlayingButton",
            "confirmAbandonButton", "runLevelText", "runDifficultyText", "runNodeText", "runTimeText", "runKillsText",
            "runGoldText", "runPowerText", "modelPreview", "lobbyNameText", "lobbyLevelText", "lobbyGoldText",
            "lobbyGemText", "lobbyPowerText", "lobbyHpText", "lobbyArmorText", "lobbyMoveText", "lobbyCritChanceText",
            "lobbyCritDamageText", "lobbyClearedText", "catalog", "powerConfig");
        RequireFields<UIBlurBackdrop>("blurImage", "dimImage", "blurShader");
        RequireFields<PlayerModelPreview>("target");
        RequireFields<MenuButtonView>("background", "label", "subLabel", "normalBackground", "normalLabel");
    }

    public static GameObject BuildToFolder(CapstoneUIBuildOptions options, LevelCatalog catalog = null,
        CombatPowerConfig powerConfig = null, Shader blurShader = null)
    {
        ValidateInputs();
        if (catalog == null) catalog = FindAsset<LevelCatalog>();
        if (powerConfig == null) powerConfig = FindAsset<CombatPowerConfig>();
        if (blurShader == null) blurShader = AssetDatabase.LoadAssetAtPath<Shader>(BlurShaderPath);
        if (blurShader == null)
            Debug.LogWarning("[Pause] No blur shader assigned. Set Pause blur shader in the tool to generate the blur reference.");

        using (var ui = new CapstoneMenuUI(options, "Pause"))
            return ui.Save(BuildCanvas(ui, catalog, powerConfig, blurShader));
    }

    private static GameObject BuildCanvas(CapstoneMenuUI ui, LevelCatalog catalog, CombatPowerConfig powerConfig, Shader blurShader)
    {
        var canvasGo = ui.Canvas("PauseMenuCanvas", 250);
        var controller = canvasGo.AddComponent<PauseMenuController>();
        var blur = canvasGo.AddComponent<UIBlurBackdrop>();

        var screen = CreateRect("PauseScreen", canvasGo.transform);
        Stretch(screen);

        // ---- backdrop: blurred screenshot + dim + a darker left side
        var rawGo = new GameObject("Blur", typeof(RectTransform), typeof(RawImage));
        rawGo.transform.SetParent(screen, false);
        var raw = rawGo.GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.enabled = false;
        Stretch((RectTransform)rawGo.transform);

        var dim = Box("Dim", screen, new Color(Shade.r, Shade.g, Shade.b, 0.3f), true);
        Stretch(dim.rectTransform);
        Place(Box("LeftShade", screen, new Color(Shade.r, Shade.g, Shade.b, 0.55f)).rectTransform, 0, 0, 1100, 1080);

        SetRefs(blur, ("blurImage", raw), ("dimImage", dim), ("blurShader", blurShader));

        // ---- title and menu
        var subtitle = ui.Text("Subtitle", screen, "", 18, Grey);
        Place(subtitle.rectTransform, 120, 150, 900, 26);
        Place(ui.LocalizedText("Title", screen, "PAUSE_TITLE", 72, Color.white).rectTransform, 120, 182, 900, 88);

        var menu = CreateRect("Menu", screen);
        Place(menu, 120, 330, 560, 480);
        VerticalList(menu.gameObject, 14);
        var resume = MenuButton(ui, menu, "ResumeButton", "PAUSE_RESUME", null, "Esc");
        var settings = MenuButton(ui, menu, "SettingsButton", "PAUSE_SETTINGS", null, null);
        var routeMap = MenuButton(ui, menu, "RouteMapButton", "PAUSE_ROUTE_MAP", null, null);
        var abandon = MenuButton(ui, menu, "AbandonButton", "PAUSE_ABANDON", "PAUSE_ABANDON_SUB", null);
        var quit = MenuButton(ui, menu, "QuitButton", "PAUSE_QUIT", null, null);

        // ---- run panel
        var runPanel = Box("RunPanel", screen, PanelColor, true);
        Place(runPanel.rectTransform, 1200, 150, 600, 570);
        Place(ui.LocalizedText("Caption", runPanel.transform, "PAUSE_CURRENT_RUN", 18, Grey).rectTransform, 36, 28, 400, 24);
        var runLevel = ui.Text("Level", runPanel.transform, "", 34, Color.white);
        Place(runLevel.rectTransform, 36, 58, 528, 48);
        var runDifficulty = StatRow(ui, runPanel.transform, "PAUSE_DIFFICULTY", 36, 124, 528);
        var runNode = StatRow(ui, runPanel.transform, "PAUSE_NODE", 36, 188, 528, icon: ui.Options.Icon("HUD_Map"));
        var runTime = StatRow(ui, runPanel.transform, "PAUSE_TIME", 36, 252, 528, icon: ui.Options.Icon("UI_Duration"));
        var runKills = StatRow(ui, runPanel.transform, "PAUSE_KILLS", 36, 316, 528, icon: ui.Options.Icon("UI_Sword"));
        var runGold = StatRow(ui, runPanel.transform, "PAUSE_GOLD_EARNED", 36, 380, 528, icon: ui.Options.Gold);
        runGold.color = Gold;
        var runPower = StatRow(ui, runPanel.transform, "PAUSE_POWER", 36, 444, 528, false, icon: ui.Options.Icon("UI_TotalStats"));

        // ---- lobby panel: model (RawImage) + player summary
        var lobbyPanel = CreateRect("LobbyPanel", screen);
        Stretch(lobbyPanel);

        var modelBox = Box("ModelBox", lobbyPanel, PanelColor, true);
        Place(modelBox.rectTransform, 800, 110, 440, 860);
        var modelGo = new GameObject("ModelImage", typeof(RectTransform), typeof(RawImage));
        modelGo.transform.SetParent(modelBox.transform, false);
        Stretch((RectTransform)modelGo.transform, 2, 2, 2, 2);
        var modelImage = modelGo.GetComponent<RawImage>();
        modelImage.color = Color.white;
        var preview = modelGo.AddComponent<PlayerModelPreview>();
        SetRefs(preview, ("target", modelImage));

        var stats = Box("StatsBox", lobbyPanel, PanelColor, true);
        Place(stats.rectTransform, 1260, 110, 540, 860);
        var lobbyName = ui.Text("Name", stats.transform, "", 38, Color.white);
        Place(lobbyName.rectTransform, 36, 30, 340, 56);
        var chip = Box("LevelChip", stats.transform, new Color(0.03f, 0.082f, 0.118f, 1f));
        Place(chip.rectTransform, 396, 38, 108, 40);
        var lobbyLevel = ui.Text("Text", chip.transform, "", 20, Color.white, TextAlignmentOptions.Center);
        Stretch(lobbyLevel.rectTransform);

        var lobbyGold = Tile(ui, stats.transform, "GoldTile", "PAUSE_GOLD", 36, 108, Gold, ui.Options.Gold);
        var lobbyGem = Tile(ui, stats.transform, "GemTile", "PAUSE_GEM", 278, 108, new Color(0.5f, 0.88f, 1f), ui.Options.Gem);

        var powerBorder = Box("PowerBorder", stats.transform, Orange);
        Place(powerBorder.rectTransform, 36, 214, 468, 124);
        var powerInner = Box("Inner", powerBorder.transform, new Color(0.16f, 0.1f, 0.07f, 1f));
        Stretch(powerInner.rectTransform, 2, 2, 2, 2);
        Place(ui.LocalizedText("Caption", powerInner.transform, "PAUSE_COMBAT_POWER", 18, new Color(1f, 0.85f, 0.72f)).rectTransform, 24, 18, 400, 24);
        var lobbyPower = ui.Text("Value", powerInner.transform, "0", 60, Color.white, TextAlignmentOptions.Left);
        Place(lobbyPower.rectTransform, 24, 46, 420, 70);

        Place(ui.LocalizedText("AttributesCaption", stats.transform, "PAUSE_ATTRIBUTES", 18, Grey).rectTransform, 36, 366, 400, 24);
        var hp = StatRow(ui, stats.transform, "PAUSE_MAX_HP", 36, 396, 468, true, 52, icon: ui.Options.Icon("UI_Heart"));
        var armor = StatRow(ui, stats.transform, "PAUSE_ARMOR", 36, 448, 468, true, 52, icon: ui.Options.Icon("Stat_Defense"));
        var move = StatRow(ui, stats.transform, "PAUSE_MOVE", 36, 500, 468, true, 52, icon: ui.Options.Icon("Stat_MoveSpeed"));
        var critChance = StatRow(ui, stats.transform, "PAUSE_CRIT_CHANCE", 36, 552, 468, true, 52, icon: ui.Options.Icon("Stat_CritChance"));
        var critDamage = StatRow(ui, stats.transform, "PAUSE_CRIT_DAMAGE", 36, 604, 468, false, 52, icon: ui.Options.Icon("Stat_CritDamage"));
        Place(Box("Divider", stats.transform, Border).rectTransform, 36, 676, 468, 2);
        var cleared = StatRow(ui, stats.transform, "PAUSE_LEVELS_CLEARED", 36, 692, 468, false, 52);

        // ---- abandon confirmation
        var confirm = CreateRect("Confirm", screen);
        Stretch(confirm);
        Stretch(Box("Dim", confirm, new Color(0f, 0f, 0f, 0.6f), true).rectTransform);
        Place(Box("Border", confirm, new Color(0.23f, 0.35f, 0.42f)).rectTransform, 618, 328, 684, 364);
        var dialog = Box("Dialog", confirm, new Color(0.05f, 0.13f, 0.19f, 1f), true);
        Place(dialog.rectTransform, 620, 330, 680, 360);
        Place(ui.LocalizedText("Caption", dialog.transform, "PAUSE_CONFIRM_CAPTION", 18, Grey).rectTransform, 48, 38, 500, 24);
        Place(ui.LocalizedText("Title", dialog.transform, "PAUSE_CONFIRM_TITLE", 38, Color.white).rectTransform, 48, 66, 584, 52);
        Place(ui.LocalizedText("Body", dialog.transform, "PAUSE_CONFIRM_BODY", 22, Soft, TextAlignmentOptions.TopLeft, FontStyles.Normal).rectTransform, 48, 128, 584, 130);
        var keep = ConfirmButton(ui, dialog.transform, "KeepButton", "PAUSE_KEEP", 48, new Color(0.16f, 0.26f, 0.32f, 1f));
        var abandonConfirm = ConfirmButton(ui, dialog.transform, "AbandonConfirmButton", "PAUSE_ABANDON_CONFIRM", 358, new Color(0.54f, 0.23f, 0.18f, 1f));

        // ---- hints
        Place(ui.KeyBadge("KeyW", screen, "W", 40, 40), 120, 1010, 40, 40);
        Place(ui.KeyBadge("KeyS", screen, "S", 40, 40), 168, 1010, 40, 40);
        Place(ui.LocalizedText("SelectLabel", screen, "PAUSE_SELECT", 24, Color.white).rectTransform, 224, 1010, 130, 40);
        Place(ui.KeyBadge("KeyEnter", screen, "Enter", 90, 40), 370, 1010, 90, 40);
        Place(ui.LocalizedText("ConfirmLabel", screen, "PAUSE_CONFIRM", 24, Color.white).rectTransform, 476, 1010, 160, 40);

        SetRefs(controller,
            ("screenRoot", screen.gameObject), ("runPanel", runPanel.gameObject), ("lobbyPanel", lobbyPanel.gameObject), ("confirmRoot", confirm.gameObject),
            ("blur", blur), ("subtitleText", subtitle),
            ("resumeButton", resume), ("settingsButton", settings), ("routeMapButton", routeMap), ("abandonButton", abandon), ("quitButton", quit),
            ("keepPlayingButton", keep), ("confirmAbandonButton", abandonConfirm),
            ("runLevelText", runLevel), ("runDifficultyText", runDifficulty), ("runNodeText", runNode), ("runTimeText", runTime),
            ("runKillsText", runKills), ("runGoldText", runGold), ("runPowerText", runPower),
            ("modelPreview", preview), ("lobbyNameText", lobbyName), ("lobbyLevelText", lobbyLevel),
            ("lobbyGoldText", lobbyGold), ("lobbyGemText", lobbyGem), ("lobbyPowerText", lobbyPower),
            ("lobbyHpText", hp), ("lobbyArmorText", armor), ("lobbyMoveText", move),
            ("lobbyCritChanceText", critChance), ("lobbyCritDamageText", critDamage), ("lobbyClearedText", cleared),
            ("catalog", catalog), ("powerConfig", powerConfig));

        lobbyPanel.gameObject.SetActive(false);
        confirm.gameObject.SetActive(false);

        return canvasGo;
    }

    // ------------------------------------------------------------------ pieces

    private static T FindAsset<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        if (guids.Length == 0) return null;
        System.Array.Sort(guids, System.StringComparer.Ordinal);
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (guids.Length > 1)
            Debug.LogWarning($"[Pause] Several {typeof(T).Name} assets exist. Using {path}. Choose one in the tool to override.");
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    // 560 x 76 menu button; a small key badge ("Esc") and a sub label are optional
    private static Button MenuButton(CapstoneMenuUI ui, Transform parent, string name, string labelKey, string subKey, string keyBadge)
    {
        var image = Box(name, parent, new Color(0.027f, 0.082f, 0.118f, 0.7f), true);
        PreferredHeight(image.gameObject, 76);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        var label = ui.LocalizedText("Label", image.transform, labelKey, 28, new Color(0.91f, 0.94f, 0.96f), TextAlignmentOptions.MidlineLeft);
        Place(label.rectTransform, 28, 0, subKey != null ? 288 : 380, 76);

        TMP_Text sub = null;
        if (subKey != null)
        {
            sub = ui.LocalizedText("Sub", image.transform, subKey, 16, Grey, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            Place(sub.rectTransform, 330, 0, 206, 76);
        }
        if (keyBadge != null) Place(ui.KeyBadge("Key", image.transform, keyBadge, 64, 36), 468, 20, 64, 36);

        var view = image.gameObject.AddComponent<MenuButtonView>();
        SetRefs(view, ("background", image), ("label", label), ("subLabel", sub));
        return button;
    }

    private static Button ConfirmButton(CapstoneMenuUI ui, Transform parent, string name, string labelKey, float x, Color normal)
    {
        var image = Box(name, parent, normal, true);
        Place(image.rectTransform, x, 276, 274, 64);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        var label = ui.LocalizedText("Label", image.transform, labelKey, 24, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);

        var view = image.gameObject.AddComponent<MenuButtonView>();
        SetRefs(view, ("background", image), ("label", label));
        var so = new SerializedObject(view);
        so.FindProperty("normalBackground").colorValue = normal;
        so.FindProperty("normalLabel").colorValue = Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // "label ........ value" with an optional divider line under it; returns the value text
    private static TMP_Text StatRow(CapstoneMenuUI ui, Transform parent, string labelKey, float x, float y, float width, bool divider = true, float height = 64, Sprite icon = null)
    {
        float inset = icon != null ? 34f : 0f;
        if (icon != null)
            CapstoneUIEditor.Icon("StatIcon", parent, x, y + (height - 34f) * .5f, 24, 24, icon);
        float split = width * .62f;
        Place(ui.LocalizedText("Label", parent, labelKey, 23, Soft, TextAlignmentOptions.MidlineLeft, FontStyles.Normal).rectTransform,
            x + inset, y, split - inset - 8f, height - 10);
        var value = ui.Text("Value", parent, "-", 25, Color.white, TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, x + split, y, width - split, height - 10);
        if (divider) Place(Box("Line", parent, Line).rectTransform, x, y + height - 8, width, 2);
        return value;
    }

    private static TMP_Text Tile(CapstoneMenuUI ui, Transform parent, string name, string captionKey, float x, float y, Color valueColor, Sprite icon)
    {
        var tile = Box(name, parent, new Color(0.02f, 0.055f, 0.08f, 0.9f));
        Place(tile.rectTransform, x, y, 226, 86);
        Place(ui.LocalizedText("Caption", tile.transform, captionKey, 16, Grey).rectTransform, 18, 10, 190, 22);
        var value = ui.Text("Value", tile.transform, "0", 34, valueColor, TextAlignmentOptions.Left);
        float valueX = icon == null ? 18 : 54;
        if (icon != null) CapstoneUIEditor.Icon("CurrencyIcon", tile.transform, 18, 42, 28, 28, icon);
        Place(value.rectTransform, valueX, 34, 208 - valueX, 44);
        return value;
    }
}
#endif
