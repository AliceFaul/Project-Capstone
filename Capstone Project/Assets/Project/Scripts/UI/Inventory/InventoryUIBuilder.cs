using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

/// <summary>
/// Tools > UI > Build Inventory Screen
/// Builds the approved layout (reference 1920x1080) and saves prefabs to Assets/UI/Inventory/Prefabs.
/// Art is placeholder (flat colors); items without an icon show a rarity-colored square.
/// </summary>
public static class InventoryUIBuilder
{
    private const string PrefabDir = "Assets/UI/Inventory/Prefabs";

    private static readonly Color Bg = new Color(0.055f, 0.133f, 0.188f, 1f);
    private static readonly Color TopBarColor = new Color(0.03f, 0.082f, 0.118f, 1f);
    private static readonly Color Panel = new Color(0.078f, 0.149f, 0.184f, 0.97f);
    private static readonly Color PanelHeader = new Color(0.239f, 0.353f, 0.42f, 1f);
    private static readonly Color Border = new Color(0.145f, 0.275f, 0.353f, 1f);
    private static readonly Color SlotBg = new Color(0.078f, 0.172f, 0.231f, 1f);
    private static readonly Color Grey = new Color(0.62f, 0.71f, 0.75f, 1f);
    private static readonly Color Orange = new Color(0.94f, 0.50f, 0.16f, 1f);
    private static readonly Color Purple = new Color(0.79f, 0.57f, 1f, 1f);
    private static readonly Color Cyan = new Color(0.37f, 0.82f, 1f, 1f);

    [MenuItem("Tools/UI/Build Inventory Screen")]
    public static void Build()
    {
        Directory.CreateDirectory(PrefabDir);
        AssetDatabase.Refresh();

        var slotPrefab = BuildSlotPrefab();
        var pipPrefab = BuildPipPrefab();
        var rowPrefab = BuildStatRowPrefab();
        var root = BuildScreen(slotPrefab, pipPrefab, rowPrefab);

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabDir + "/InventoryScreen.prefab", InteractionMode.UserAction);
        Undo.RegisterCreatedObjectUndo(root, "Build Inventory Screen");
        Selection.activeGameObject = root;
        Debug.Log("Inventory screen built. Prefabs saved in " + PrefabDir);
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject BuildSlotPrefab()
    {
        var go = new GameObject("InventorySlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(InventorySlotView));
        ((RectTransform)go.transform).sizeDelta = new Vector2(80, 80);

        var frame = go.GetComponent<Image>();
        frame.color = Border;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = frame;
        btn.transition = Selectable.Transition.None;

        var highlight = Img("Highlight", go.transform, Color.white);
        Stretch(highlight.rectTransform, -4, -4, -4, -4);
        highlight.gameObject.SetActive(false);

        Stretch(Img("Inner", go.transform, SlotBg).rectTransform, 2, 2, 2, 2);

        var icon = Img("Icon", go.transform, Color.white);
        icon.rectTransform.anchorMin = new Vector2(0.15f, 0.15f);
        icon.rectTransform.anchorMax = new Vector2(0.85f, 0.85f);
        icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
        icon.preserveAspect = true;

        var strip = Img("RarityStrip", go.transform, Color.white);
        strip.rectTransform.anchorMin = new Vector2(0, 0);
        strip.rectTransform.anchorMax = new Vector2(1, 0);
        strip.rectTransform.offsetMin = new Vector2(2, 2);
        strip.rectTransform.offsetMax = new Vector2(-2, 6);

        var levelBadge = Img("LevelBadge", go.transform, new Color(0, 0, 0, 0.65f));
        var lbr = levelBadge.rectTransform;
        lbr.anchorMin = lbr.anchorMax = lbr.pivot = new Vector2(1, 0);
        lbr.anchoredPosition = new Vector2(-4, 9);
        lbr.sizeDelta = new Vector2(30, 22);
        var levelText = Txt("Text", levelBadge.transform, "1", 16, Color.white, TextAlignmentOptions.Center);
        Stretch(levelText.rectTransform);

        var hotkey = Badge("HotkeyBadge", go.transform, "1", 36, 30);
        hotkey.anchorMin = hotkey.anchorMax = new Vector2(0.5f, 1f);
        hotkey.pivot = new Vector2(0.5f, 0f);
        hotkey.anchoredPosition = new Vector2(0, 6);
        hotkey.gameObject.SetActive(false);

        SetRefs(go.GetComponent<InventorySlotView>(),
            ("icon", icon), ("rarityStrip", strip), ("levelBadge", levelBadge.gameObject), ("levelText", levelText),
            ("hotkeyBadge", hotkey.gameObject), ("hotkeyText", hotkey.GetComponentInChildren<TMP_Text>()),
            ("highlight", highlight.gameObject));

        return SaveAsPrefab(go, "InventorySlot");
    }

    private static GameObject BuildPipPrefab()
    {
        var go = new GameObject("SocketPip", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(SocketPipView));
        ((RectTransform)go.transform).sizeDelta = new Vector2(30, 30);

        var frame = go.GetComponent<Image>();
        frame.color = new Color(0.227f, 0.353f, 0.431f, 1f);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = frame;
        btn.transition = Selectable.Transition.None;

        var highlight = Img("Highlight", go.transform, Color.white);
        Stretch(highlight.rectTransform, -3, -3, -3, -3);
        highlight.gameObject.SetActive(false);
        var fill = Img("Fill", go.transform, SlotBg);
        Stretch(fill.rectTransform, 2, 2, 2, 2);

        SetRefs(go.GetComponent<SocketPipView>(),
            ("fill", fill), ("highlight", highlight.gameObject), ("group", go.GetComponent<CanvasGroup>()));

        return SaveAsPrefab(go, "SocketPip");
    }

    private static GameObject BuildStatRowPrefab()
    {
        var go = new GameObject("StatRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        var h = go.GetComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        h.childAlignment = TextAnchor.MiddleLeft;
        go.GetComponent<LayoutElement>().minHeight = 34;

        var label = Txt("Label", go.transform, "Stat", 22, Color.white, TextAlignmentOptions.Left, FontStyles.Normal);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        Txt("Value", go.transform, "0", 22, Color.white, TextAlignmentOptions.Right);
        var tier = Txt("Tier", go.transform, "", 16, Grey, TextAlignmentOptions.Right, FontStyles.Normal);
        LE(tier.gameObject, prefW: 70);

        return SaveAsPrefab(go, "StatRow");
    }

    private static GameObject SaveAsPrefab(GameObject go, string name)
    {
        var path = PrefabDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ------------------------------------------------------------------ screen

    private static GameObject BuildScreen(GameObject slotPrefab, GameObject pipPrefab, GameObject rowPrefab)
    {
        var canvasGO = new GameObject("InventoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var screen = NewRect("InventoryScreen", canvasGO.transform);
        Stretch(screen);
        var bgImage = screen.gameObject.AddComponent<Image>();
        bgImage.color = Bg;
        bgImage.raycastTarget = true; // swallows clicks so they never reach the world
        var view = screen.gameObject.AddComponent<InventoryScreenView>();
        var controller = canvasGO.AddComponent<InventoryScreenController>();
        SetRefs(controller, ("screenRoot", screen.gameObject), ("screenView", view));

        // ---- top bar
        var top = Img("TopBar", screen, TopBarColor);
        top.rectTransform.anchorMin = new Vector2(0, 1);
        top.rectTransform.anchorMax = new Vector2(1, 1);
        top.rectTransform.pivot = new Vector2(0.5f, 1);
        top.rectTransform.anchoredPosition = Vector2.zero;
        top.rectTransform.sizeDelta = new Vector2(0, 80);
        TopLeft(Txt("Tab", top.transform, "INVENTORY", 30, Color.white).rectTransform, 100, 18, 260, 44);
        TopLeft(Img("TabUnderline", top.transform, Orange).rectTransform, 100, 70, 190, 6);
        var goldText = CurrencyChip(top.transform, 1420, new Color(0.95f, 0.75f, 0.22f), "Gold 12,480");
        var gemText = CurrencyChip(top.transform, 1670, new Color(0.25f, 0.78f, 0.94f), "Gem 320");

        // ---- header: level, exp, stats button
        TopLeft(Txt("LevelLabel", screen, "LEVEL", 26, Grey).rectTransform, 100, 110, 200, 34);
        var levelValue = Txt("LevelValue", screen, "29", 92, Purple);
        TopLeft(levelValue.rectTransform, 100, 140, 260, 110);

        TopLeft(Txt("ExpLabel", screen, "EXPERIENCE", 20, Grey).rectTransform, 440, 120, 300, 30);
        var bar = Img("ExpBar", screen, SlotBg);
        TopLeft(bar.rectTransform, 440, 156, 560, 22);
        var expFill = Img("Fill", bar.transform, Cyan);
        Stretch(expFill.rectTransform);
        var expText = Txt("ExpText", screen, "1,250 / 2,400", 22, Color.white);
        TopLeft(expText.rectTransform, 440, 186, 400, 32);

        var statsBtnImg = Img("PlayerStatsButton", screen, new Color(0.165f, 0.259f, 0.322f, 1f), true);
        TopLeft(statsBtnImg.rectTransform, 1440, 150, 380, 64);
        var statsBtn = statsBtnImg.gameObject.AddComponent<Button>();
        statsBtn.targetGraphic = statsBtnImg;
        TopLeft(Txt("Label", statsBtnImg.transform, "Player Stats", 26, Color.white).rectTransform, 22, 12, 280, 40);
        TopLeft(Badge("Key", statsBtnImg.transform, "Y", 44, 36), 316, 14, 44, 36);
        UnityEventTools.AddPersistentListener(statsBtn.onClick, new UnityAction(view.ToggleStats));

        // ---- character preview placeholder
        var previewRt = NewRect("CharacterPreview", screen);
        TopLeft(previewRt, 100, 250, 400, 680);
        var raw = previewRt.gameObject.AddComponent<RawImage>();
        raw.color = new Color(0, 0, 0, 0.18f);
        raw.raycastTarget = false;
        var previewLabel = Txt("Label", previewRt, "Character preview\n(assign a RenderTexture)", 24, Grey, TextAlignmentOptions.Center, FontStyles.Normal);
        Stretch(previewLabel.rectTransform, 20, 20, 20, 20);

        // ---- equipment
        TopLeft(Txt("Cap_Weapons", screen, "WEAPONS", 20, Grey).rectTransform, 540, 250, 300, 30);
        var melee = PlaceEquipSlot(screen, slotPrefab, "MeleeSlot", EquipSlot.Melee, 0, 540, 290, null);
        var ranged = PlaceEquipSlot(screen, slotPrefab, "RangedSlot", EquipSlot.Ranged, 0, 675, 290, null);
        TopLeft(Txt("Label_Melee", screen, "MELEE", 18, Grey).rectTransform, 540, 404, 110, 26);
        TopLeft(Txt("Label_Ranged", screen, "RANGED", 18, Grey).rectTransform, 675, 404, 110, 26);
        for (int i = 0; i < EquipmentInstance.SocketCount; i++)
        {
            PlacePip(screen, pipPrefab, EquipSlot.Melee, i, 540 + i * 38, 438);
            PlacePip(screen, pipPrefab, EquipSlot.Ranged, i, 675 + i * 38, 438);
        }

        TopLeft(Txt("Cap_Armor", screen, "ARMOR", 20, Grey).rectTransform, 540, 500, 300, 30);
        var armor = PlaceEquipSlot(screen, slotPrefab, "ArmorSlot", EquipSlot.Armor, 0, 540, 540, null);
        TopLeft(Txt("Label_Armor", screen, "ARMOR", 18, Grey).rectTransform, 540, 656, 110, 26);

        TopLeft(Txt("Cap_Artifacts", screen, "ARTIFACTS", 20, Grey).rectTransform, 540, 700, 300, 30);
        var artifacts = new InventorySlotView[Loadout.ArtifactSlotCount];
        for (int i = 0; i < artifacts.Length; i++)
            artifacts[i] = PlaceEquipSlot(screen, slotPrefab, "ArtifactSlot" + (i + 1), EquipSlot.Artifact, i, 540 + i * 135, 770, (i + 1).ToString());

        // ---- bag: tabs + scrolling grid
        string[] tabNames = { "MELEE", "RANGED", "ARMOR", "RUNE", "ARTIFACT" };
        var tabButtons = new Button[tabNames.Length];
        var tabLines = new GameObject[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            var tabImg = Img("Tab_" + tabNames[i], screen, new Color(0, 0, 0, 0), true);
            TopLeft(tabImg.rectTransform, 960 + i * 88, 250, 84, 44);
            tabButtons[i] = tabImg.gameObject.AddComponent<Button>();
            tabButtons[i].targetGraphic = tabImg;
            var label = Txt("Label", tabImg.transform, tabNames[i], 16, Color.white, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            var line = Img("Underline", tabImg.transform, Orange);
            line.rectTransform.anchorMin = new Vector2(0, 0);
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.offsetMin = Vector2.zero;
            line.rectTransform.offsetMax = new Vector2(0, 4);
            tabLines[i] = line.gameObject;
        }

        var scrollRt = NewRect("BagScroll", screen);
        TopLeft(scrollRt, 960, 314, 440, 440);
        var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
        var viewport = NewRect("Viewport", scrollRt);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        TopLeft(Txt("BagHint", screen, "Click an item to equip", 20, Grey, TextAlignmentOptions.Left, FontStyles.Normal).rectTransform, 960, 774, 260, 30);
        var bagCount = Txt("BagCount", screen, "0 / 25", 20, Color.white, TextAlignmentOptions.Right);
        TopLeft(bagCount.rectTransform, 1200, 774, 200, 30);

        // ---- right panels
        var detailPanel = BuildDetailPanel(screen, rowPrefab, out var detailView);
        var statsPanel = BuildStatsPanel(screen);

        // ---- toast + bottom hints
        var toast = Txt("Toast", screen, "", 24, new Color(1f, 0.82f, 0.48f), TextAlignmentOptions.Center);
        TopLeft(toast.rectTransform, 560, 935, 800, 44);
        toast.gameObject.SetActive(false);

        TopLeft(Badge("BackKey", screen, "Esc", 70, 40), 100, 995, 70, 40);
        TopLeft(Txt("BackLabel", screen, "Back", 28, Color.white).rectTransform, 185, 995, 160, 40);
        TopLeft(Txt("SelectLabel", screen, "Equip / Unequip", 28, Color.white, TextAlignmentOptions.Right).rectTransform, 1480, 995, 240, 40);
        TopLeft(Badge("SelectKey", screen, "Click", 90, 40), 1730, 995, 90, 40);

        // ---- wiring
        SetRefs(view,
            ("levelText", levelValue), ("expText", expText), ("expFill", expFill.rectTransform),
            ("goldText", goldText), ("gemText", gemText),
            ("meleeSlot", melee), ("rangedSlot", ranged), ("armorSlot", armor),
            ("bagContent", content), ("bagSlotPrefab", slotPrefab.GetComponent<InventorySlotView>()), ("bagCountText", bagCount),
            ("detail", detailView), ("detailPanel", detailPanel), ("statsPanel", statsPanel), ("toastText", toast));
        SetArray(view, "artifactSlots", artifacts);
        SetArray(view, "tabButtons", tabButtons);
        SetArray(view, "tabUnderlines", tabLines);
        UnityEventTools.AddPersistentListener(view.onBack, new UnityAction(controller.Close));

        statsPanel.SetActive(false);
        return canvasGO;
    }

    private static GameObject BuildDetailPanel(RectTransform screen, GameObject rowPrefab, out InventoryDetailView view)
    {
        var panel = Img("DetailPanel", screen, Panel);
        TopLeft(panel.rectTransform, 1440, 250, 380, 680);
        view = panel.gameObject.AddComponent<InventoryDetailView>();

        var header = Img("Header", panel.transform, PanelHeader);
        TopLeft(header.rectTransform, 0, 0, 380, 44);
        var headerText = Txt("Text", header.transform, "EQUIPPED", 20, Color.white, TextAlignmentOptions.Center);
        Stretch(headerText.rectTransform, 8, 0, 8, 0);

        var body = NewRect("Body", panel.transform);
        Stretch(body, 0, 44, 0, 0);
        var vlg = VLG(body.gameObject, 10);
        vlg.padding = new RectOffset(22, 22, 18, 18);

        // level + rarity
        var levelRow = NewRect("LevelRow", body);
        HLG(levelRow.gameObject, 14);
        LE(levelRow.gameObject, prefH: 56);
        var levelText = Txt("Level", levelRow, "Lv 7", 44, Grey);
        LE(levelText.gameObject, prefW: 150);
        var badge = Img("RarityBadge", levelRow, Orange);
        LE(badge.gameObject, prefW: 140, prefH: 36);
        var rarityText = Txt("Text", badge.transform, "UNIQUE", 20, Color.white, TextAlignmentOptions.Center);
        Stretch(rarityText.rectTransform);

        var nameText = Txt("Name", body, "ITEM NAME", 34, Color.white);
        var subText = Txt("Sub", body, "", 18, Grey, TextAlignmentOptions.Left, FontStyles.Normal);

        // exp bar
        var expGroup = NewRect("ExpGroup", body);
        LE(expGroup.gameObject, prefH: 40);
        var expBar = Img("Bar", expGroup, SlotBg);
        expBar.rectTransform.anchorMin = new Vector2(0, 1);
        expBar.rectTransform.anchorMax = new Vector2(1, 1);
        expBar.rectTransform.pivot = new Vector2(0.5f, 1);
        expBar.rectTransform.anchoredPosition = Vector2.zero;
        expBar.rectTransform.sizeDelta = new Vector2(0, 12);
        var expFill = Img("Fill", expBar.transform, Cyan);
        Stretch(expFill.rectTransform);
        var expText = Txt("ExpText", expGroup, "", 16, Grey, TextAlignmentOptions.Left, FontStyles.Normal);
        expText.rectTransform.anchorMin = new Vector2(0, 0);
        expText.rectTransform.anchorMax = new Vector2(1, 0);
        expText.rectTransform.pivot = new Vector2(0.5f, 0);
        expText.rectTransform.anchoredPosition = Vector2.zero;
        expText.rectTransform.sizeDelta = new Vector2(0, 22);

        // stats
        var statsGroup = NewRect("StatsGroup", body);
        VLG(statsGroup.gameObject, 4);
        Txt("StatsLabel", statsGroup, "STATS", 20, Grey);
        var statsRows = NewRect("Rows", statsGroup);
        VLG(statsRows.gameObject, 2);

        // sockets (weapons only)
        var socketsGroup = NewRect("SocketsGroup", body);
        VLG(socketsGroup.gameObject, 8);
        Txt("SocketsLabel", socketsGroup, "SOCKETS", 20, Grey);
        var socketRow = NewRect("Squares", socketsGroup);
        HLG(socketRow.gameObject, 14);
        LE(socketRow.gameObject, prefH: 56);
        var squares = new Image[EquipmentInstance.SocketCount];
        var squareLabels = new TMP_Text[EquipmentInstance.SocketCount];
        for (int i = 0; i < squares.Length; i++)
        {
            squares[i] = Img("Socket" + (i + 1), socketRow, SlotBg);
            LE(squares[i].gameObject, prefW: 56, prefH: 56);
            squareLabels[i] = Txt("Label", squares[i].transform, "", 16, Grey, TextAlignmentOptions.Center);
            Stretch(squareLabels[i].rectTransform);
        }
        var runeText = Txt("RuneText", socketsGroup, "", 18, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal);

        var descText = Txt("Desc", body, "", 19, Grey, TextAlignmentOptions.TopLeft, FontStyles.Normal);

        SetRefs(view,
            ("headerText", headerText), ("levelRow", levelRow.gameObject), ("levelText", levelText),
            ("rarityBg", badge), ("rarityText", rarityText), ("nameText", nameText), ("subText", subText),
            ("expGroup", expGroup.gameObject), ("expFill", expFill.rectTransform), ("expText", expText),
            ("statsGroup", statsGroup.gameObject), ("statsRoot", statsRows), ("statRowPrefab", rowPrefab),
            ("socketsGroup", socketsGroup.gameObject), ("runeText", runeText), ("descText", descText));
        SetArray(view, "socketSquares", squares);
        SetArray(view, "socketLabels", squareLabels);

        return panel.gameObject;
    }

    private static GameObject BuildStatsPanel(RectTransform screen)
    {
        var panel = Img("StatsPanel", screen, Panel);
        TopLeft(panel.rectTransform, 1440, 250, 380, 680);
        var view = panel.gameObject.AddComponent<PlayerStatsView>();

        var header = Img("Header", panel.transform, PanelHeader);
        TopLeft(header.rectTransform, 0, 0, 380, 44);
        Stretch(Txt("Text", header.transform, "PLAYER STATS", 20, Color.white, TextAlignmentOptions.Center).rectTransform);

        var list = NewRect("Rows", panel.transform);
        TopLeft(list, 22, 64, 336, 600);
        VLG(list.gameObject, 4);

        string[] names = { "Health", "Damage", "Defense", "Move Speed", "Crit Chance", "Crit Damage", "Attack Speed" };
        var values = new TMP_Text[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var row = NewRect("Row_" + names[i], list);
            LE(row.gameObject, prefH: 46);
            var bg = row.gameObject.AddComponent<Image>();
            bg.color = i % 2 == 0 ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0);
            bg.raycastTarget = false;

            var label = Txt("Label", row, names[i], 22, Grey, TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            Stretch(label.rectTransform, 12, 0, 12, 0);
            values[i] = Txt("Value", row, "0", 22, Color.white, TextAlignmentOptions.MidlineRight);
            Stretch(values[i].rectTransform, 12, 0, 12, 0);
        }
        SetArray(view, "values", values);
        return panel.gameObject;
    }

    private static InventorySlotView PlaceEquipSlot(Transform parent, GameObject slotPrefab, string name,
        EquipSlot slot, int artifactIndex, float x, float y, string hotkey)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, parent);
        go.name = name;
        TopLeft((RectTransform)go.transform, x, y, 110, 110);

        var view = go.GetComponent<InventorySlotView>();
        var so = new SerializedObject(view);
        so.FindProperty("kind").enumValueIndex = (int)SlotKind.Equipped;
        so.FindProperty("equipSlot").enumValueIndex = (int)slot;
        so.FindProperty("artifactIndex").intValue = artifactIndex;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!string.IsNullOrEmpty(hotkey))
        {
            var badge = so.FindProperty("hotkeyBadge").objectReferenceValue as GameObject;
            var text = so.FindProperty("hotkeyText").objectReferenceValue as TMP_Text;
            if (badge != null) badge.SetActive(true);
            if (text != null) text.text = hotkey;
        }
        return view;
    }

    private static void PlacePip(Transform parent, GameObject pipPrefab, EquipSlot weapon, int index, float x, float y)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pipPrefab, parent);
        go.name = $"Pip_{weapon}_{index + 1}";
        TopLeft((RectTransform)go.transform, x, y, 30, 30);

        var so = new SerializedObject(go.GetComponent<SocketPipView>());
        so.FindProperty("weaponSlot").enumValueIndex = (int)weapon;
        so.FindProperty("socketIndex").intValue = index;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CurrencyChip(Transform parent, float x, Color color, string text)
    {
        TopLeft(Img("CurrencyIcon", parent, color).rectTransform, x, 26, 28, 28);
        var t = Txt("CurrencyText", parent, text, 28, Color.white);
        TopLeft(t.rectTransform, x + 40, 20, 200, 40);
        return t;
    }

    // ------------------------------------------------------------------ helpers

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Positions rt by its top-left corner, in pixels from the parent's top-left.</summary>
    private static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    private static Image Img(string name, Transform parent, Color c, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var i = rt.gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = raycast;
        return i;
    }

    private static TextMeshProUGUI Txt(string name, Transform parent, string s, float size, Color c,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = align;
        t.fontStyle = style;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    private static RectTransform Badge(string name, Transform parent, string label, float w, float h)
    {
        var img = Img(name, parent, new Color(0.027f, 0.082f, 0.118f, 1f));
        img.rectTransform.sizeDelta = new Vector2(w, h);
        var t = Txt("Label", img.transform, label, h * 0.5f, Color.white, TextAlignmentOptions.Center);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        Stretch(t.rectTransform);
        return img.rectTransform;
    }

    private static VerticalLayoutGroup VLG(GameObject go, float spacing)
    {
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = spacing;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        v.childAlignment = TextAnchor.UpperLeft;
        return v;
    }

    private static HorizontalLayoutGroup HLG(GameObject go, float spacing)
    {
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = spacing;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        h.childAlignment = TextAnchor.MiddleLeft;
        return h;
    }

    private static LayoutElement LE(GameObject go, float prefW = -1, float prefH = -1)
    {
        var l = go.AddComponent<LayoutElement>();
        if (prefW >= 0) l.preferredWidth = prefW;
        if (prefH >= 0) l.preferredHeight = prefH;
        return l;
    }

    private static void SetRefs(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
        {
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError($"Field '{field}' not found on {target.GetType().Name}");
                continue;
            }
            p.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"Field '{field}' not found on {target.GetType().Name}");
            return;
        }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
