#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using Object = UnityEngine.Object;

public static class ShopUIBuilder
{
    private static string PrefabDir;
    private static CapstoneUIBuildOptions Options;
    private static CapstoneUIEditor.BuildScene BuildScene;
    private const int StockCapacity = 8;

    private static readonly Color Gold = new Color(0.95f, 0.75f, 0.22f);
    private static readonly Color Gem = new Color(0.25f, 0.78f, 0.94f);
    private static readonly Color Dark = new Color(0.04f, 0.09f, 0.13f);
    private static readonly Color Translucent = new Color(0.059f, 0.141f, 0.188f, 0.88f);
    private static readonly Color Box = new Color(0.043f, 0.106f, 0.145f, 1f);
    private static readonly Color Border = new Color(.145f, .275f, .353f, 1);
    private static readonly Color Panel = new Color(.078f, .149f, .184f, .97f);
    private static readonly Color Grey = new Color(.62f, .71f, .75f, 1);
    private static readonly Color Orange = new Color(.94f, .50f, .16f, 1);

    [MenuItem("Tools/Shop/Build Shop UI")]
    public static void Build() => CapstoneUIPrefabTool.OpenShop();

    [MenuItem("Tools/Shop/Validate Selected Shop Prefab")]
    public static void ValidateSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) throw new InvalidOperationException("Select ShopCanvas first.");
        string path = AssetDatabase.GetAssetPath(selected);
        bool load = path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        var root = load ? PrefabUtility.LoadPrefabContents(path) : selected;
        try
        {
            ValidateBuiltRoot(root);
            Debug.Log("[Shop] Prefab references checked. Verify buy/sell, scrolling and scene flow in Play Mode.", selected);
        }
        finally { if (load) PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ValidateInputs()
    {
        CapstoneUIEditor.RequireField(typeof(ShopTileView), "rarityTint", typeof(Image));
        CapstoneUIEditor.RequireField(typeof(ShopTileView), "categoryFallbackIcons", typeof(Sprite[]));
        CapstoneUIEditor.RequireField(typeof(ShopScreenView), "backButton", typeof(Button));
        CapstoneUIEditor.RequireField(typeof(InventoryDetailView), "statIcons", null);
        CapstoneUIEditor.RequireField(typeof(InventoryDetailView), "socketIcons", typeof(Image[]));
        CapstoneUIEditor.RequireField(typeof(InventorySlotView), "emptyPlaceholder", typeof(Image));
    }

    public static GameObject BuildToFolder(CapstoneUIBuildOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        ValidateInputs();
        Options = options;
        PrefabDir = options.Output + "/Shop";
        ShopText.RegisterDefaults();
        try
        {
            InventoryUIBuilder.BuildSharedItemPrefabs(options, PrefabDir + "/Items", out var baseSlot, out var rowPrefab);
            using (var scene = new CapstoneUIEditor.BuildScene())
            {
                BuildScene = scene;
                var slot = BuildSellSlotPrefab(baseSlot);
                var tile = BuildTilePrefab();
                var root = BuildCanvas(slot, rowPrefab, tile);
                ValidateBuiltRoot(root);
                var saved = CapstoneUIEditor.Save(root, PrefabDir + "/ShopCanvas.prefab");
                InventoryLocalizationTool.AddKeys();
                return saved;
            }
        }
        finally { BuildScene = null; Options = null; }
    }

    private static GameObject BuildSellSlotPrefab(GameObject baseSlot)
    {
        var go = Object.Instantiate(baseSlot);
        BuildScene.Own(go);
        go.name = "ShopSellSlot";
        var slotData = new SerializedObject(go.GetComponent<InventorySlotView>());
        var itemIcon = (Image)slotData.FindProperty("icon").objectReferenceValue;
        itemIcon.rectTransform.anchorMin = new Vector2(.15f, .15f);
        itemIcon.rectTransform.anchorMax = new Vector2(.85f, .70f);
        itemIcon.rectTransform.offsetMin = itemIcon.rectTransform.offsetMax = Vector2.zero;
        var bar = Img("SellPrice", go.transform, new Color(0, 0, 0, .72f));
        TopLeft(bar.rectTransform, 3, 3, 74, 19);
        TopLeft(CurrencyIcon("Coin", bar.transform, Options.Gold, Gold).rectTransform, 2, 2, 14, 14);
        var value = Txt("Value", bar.transform, "0", 13, Color.white, TextAlignmentOptions.MidlineRight);
        TopLeft(value.rectTransform, 18, 0, 54, 19);
        Fit(value, 9, 13);
        bar.gameObject.SetActive(false);
        string path = PrefabDir + "/ShopSellSlot.prefab";
        CapstoneUIEditor.Save(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ------------------------------------------------------------------ tile prefab

    private static GameObject BuildTilePrefab()
    {
        var go = new GameObject("ShopTile", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(ShopTileView));
        BuildScene.Own(go);
        ((RectTransform)go.transform).sizeDelta = new Vector2(168, 190);

        var frame = go.GetComponent<Image>();
        frame.color = Border;
        var button = go.GetComponent<Button>();
        button.targetGraphic = frame;
        button.transition = Selectable.Transition.None;

        var highlight = Img("Highlight", go.transform, Color.white);
        Stretch(highlight.rectTransform, -4, -4, -4, -4);
        highlight.gameObject.SetActive(false);

        Stretch(Img("Inner", go.transform, new Color(0.078f, 0.149f, 0.184f, 1f)).rectTransform, 2, 2, 2, 2);

        var tint = Img("RarityTint", go.transform, new Color(1, 1, 1, .18f));
        Stretch(tint.rectTransform, 3, 3, 3, 3);

        var strip = Img("Strip", go.transform, Color.white);
        strip.rectTransform.anchorMin = new Vector2(0, 1);
        strip.rectTransform.anchorMax = new Vector2(1, 1);
        strip.rectTransform.offsetMin = new Vector2(2, -7);
        strip.rectTransform.offsetMax = new Vector2(-2, -2);

        var icon = Img("Icon", go.transform, Color.white);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0.5f, 1f);
        icon.rectTransform.anchoredPosition = new Vector2(0, -34);
        icon.rectTransform.sizeDelta = new Vector2(96, 96);
        icon.preserveAspect = true;

        var bar = Img("PriceBar", go.transform, Box);
        bar.rectTransform.anchorMin = new Vector2(0, 0);
        bar.rectTransform.anchorMax = new Vector2(1, 0);
        bar.rectTransform.offsetMin = new Vector2(2, 2);
        bar.rectTransform.offsetMax = new Vector2(-2, 44);
        var layout = HLG(bar.gameObject, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        var coin = CurrencyIcon("Coin", bar.transform, Options.Gold, Gold);
        LE(coin.gameObject, prefW: 20, prefH: 20);
        var price = Txt("Price", bar.transform, "0", 22, Color.white, TextAlignmentOptions.Center);
        LE(price.gameObject, prefW: 120, prefH: 32);

        SetRefs(go.GetComponent<ShopTileView>(),
            ("icon", icon), ("strip", strip), ("rarityTint", tint), ("coin", coin.gameObject), ("priceText", price),
            ("highlight", highlight.gameObject), ("group", go.GetComponent<CanvasGroup>()));

        SetArray(go.GetComponent<ShopTileView>(), "categoryFallbackIcons", CategoryIcons());
        Fit(price, 14, 22);
        string path = PrefabDir + "/ShopTile.prefab";
        CapstoneUIEditor.Save(go, path);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    // ------------------------------------------------------------------ canvas

    private static GameObject BuildCanvas(GameObject slotPrefab, GameObject rowPrefab, GameObject tilePrefab)
    {
        var canvasGo = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        BuildScene.Own(canvasGo);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var view = canvasGo.AddComponent<ShopScreenView>();
        var screen = NewRect("ShopScreen", canvasGo.transform);
        Stretch(screen);
        var inputSurface = screen.gameObject.AddComponent<Image>();
        inputSurface.color = Color.clear;
        inputSurface.raycastTarget = true;

        // ---- top bar
        var top = Img("TopBar", screen, new Color(0.027f, 0.082f, 0.118f, 0.88f));
        top.rectTransform.anchorMin = new Vector2(0, 1);
        top.rectTransform.anchorMax = new Vector2(1, 1);
        top.rectTransform.pivot = new Vector2(0.5f, 1);
        top.rectTransform.anchoredPosition = Vector2.zero;
        top.rectTransform.sizeDelta = new Vector2(0, 80);
        TopLeft(LTxt("Title", top.transform, "SHOP_TITLE", 30, Color.white).rectTransform, 100, 18, 300, 44);
        var buyTab = BuildTopTab(top.transform, "BuyTab", "SHOP_BUY", 420, out var buyLine);
        var sellTab = BuildTopTab(top.transform, "SellTab", "SHOP_SELL", 560, out var sellLine);
        var goldText = CurrencyChip(top.transform, 1420, Options.Gold, Gold, "0");
        var gemText = CurrencyChip(top.transform, 1670, Options.Gem, Gem, "0");

        // ---- left frame (stock / bag)
        var frame = Img("LeftFrame", screen, Border);
        TopLeft(frame.rectTransform, 100, 130, 620, 590);
        var inner = Img("Inner", frame.transform, Translucent, true);
        Stretch(inner.rectTransform, 2, 2, 2, 2);

        var buyPanel = NewRect("BuyPanel", inner.transform);
        Stretch(buyPanel);
        TopLeft(LTxt("Caption", buyPanel, "SHOP_FOR_SALE", 20, Grey).rectTransform, 40, 26, 300, 30);

        var stockContent = BuildScrollGrid(buyPanel, "StockScroll", 40, 70, 540, 404, new Vector2(168, 190), new Vector2(14, 14), 3);
        var tiles = new ShopTileView[StockCapacity];
        for (int i = 0; i < tiles.Length; i++)
        {
            var tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, stockContent);
            tile.name = "Tile" + (i + 1);
            tiles[i] = tile.GetComponent<ShopTileView>();
        }
        var stockCount = BuildInfoBox(buyPanel, "SHOP_STOCK_NOTE", "0 / 0");

        var sellPanel = NewRect("SellPanel", inner.transform);
        Stretch(sellPanel);
        TopLeft(LTxt("Caption", sellPanel, "SHOP_YOUR_ITEMS", 20, Grey).rectTransform, 40, 26, 300, 30);

        string[] categoryKeys = { "INV_MELEE", "INV_RANGED", "INV_ARMOR", "INV_RUNE", "INV_ARTIFACT" };
        var categoryButtons = new Button[categoryKeys.Length];
        var categoryLines = new GameObject[categoryKeys.Length];
        for (int i = 0; i < categoryKeys.Length; i++)
        {
            var tabImage = Img("Category_" + i, sellPanel, new Color(0, 0, 0, 0), true);
            TopLeft(tabImage.rectTransform, 40 + i * 108, 64, 104, 86);
            categoryButtons[i] = tabImage.gameObject.AddComponent<Button>();
            categoryButtons[i].targetGraphic = tabImage;
            var glyph = Img("Icon", tabImage.transform, Color.white);
            glyph.sprite = CategoryIcons()[i];
            glyph.preserveAspect = true;
            TopLeft(glyph.rectTransform, 28, 0, 48, 48);
            var label = LTxt("Label", tabImage.transform, categoryKeys[i], 16, Color.white, TextAlignmentOptions.Center);
            TopLeft(label.rectTransform, 0, 50, 104, 28);
            Fit(label, 11, 16);
            var line = Img("Underline", tabImage.transform, Orange);
            line.rectTransform.anchorMin = new Vector2(0, 0);
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.offsetMin = Vector2.zero;
            line.rectTransform.offsetMax = new Vector2(0, 4);
            categoryLines[i] = line.gameObject;
        }

        var content = BuildScrollGrid(sellPanel, "SellScroll", 40, 162, 540, 312, new Vector2(80, 80), new Vector2(10, 10), 6);

        var selection = Img("SelectionFrame", content, new Color(1, .78f, .32f, 1));
        selection.rectTransform.anchorMin = selection.rectTransform.anchorMax = selection.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        selection.rectTransform.sizeDelta = new Vector2(88, 88);
        selection.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        selection.gameObject.SetActive(false);

        var sellCount = BuildInfoBox(sellPanel, "SHOP_SELL_NOTE", "0 / 25");

        // ---- detail card (reuses the inventory detail panel) + price / action block
        var detailObject = InventoryUIBuilder.BuildSharedDetailPanel(screen, rowPrefab, Options, out var detailView);
        TopLeft((RectTransform)detailObject.transform, 780, 150, 560, 430);
        var header = detailObject.transform.Find("Header") as RectTransform;
        if (header != null)
        {
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0, 44);
        }

        var action = Img("ActionGroup", screen, Panel);
        TopLeft(action.rectTransform, 780, 590, 560, 160);
        var band = Img("Band", action.transform, new Color(0.063f, 0.149f, 0.227f, 1f));
        TopLeft(band.rectTransform, 0, 0, 560, 62);
        var priceLabel = Txt("PriceLabel", band.transform, "Cost", 22, Grey, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
        TopLeft(priceLabel.rectTransform, 20, 10, 200, 42);
        Fit(priceLabel, 14, 22);
        TopLeft(CurrencyIcon("Coin", band.transform, Options.Gold, Gold).rectTransform, 234, 17, 28, 28);
        var priceText = Txt("PriceText", band.transform, "0", 38, Color.white, TextAlignmentOptions.MidlineLeft);
        TopLeft(priceText.rectTransform, 280, 8, 260, 46);
        Fit(priceText, 20, 38);

        var buttonImage = Img("ActionButton", action.transform, Orange, true);
        TopLeft(buttonImage.rectTransform, 24, 78, 512, 56);
        var actionButton = buttonImage.gameObject.AddComponent<Button>();
        actionButton.targetGraphic = buttonImage;
        var actionLabel = Txt("Label", buttonImage.transform, "PURCHASE", 22, Dark, TextAlignmentOptions.Center);
        Stretch(actionLabel.rectTransform, 16, 0, 16, 0);
        Fit(actionLabel, 16, 22);
        var note = Txt("Note", action.transform, "", 17, Grey, TextAlignmentOptions.Center, FontStyles.Normal);
        TopLeft(note.rectTransform, 24, 136, 512, 22);
        Fit(note, 12, 17);

        // ---- merchant box (the merchant himself is the live 3D view behind the UI)
        var tipBox = Img("MerchantBox", screen, Panel);
        TopLeft(tipBox.rectTransform, 1400, 850, 420, 100);
        TopLeft(LTxt("Caption", tipBox.transform, "SHOP_MERCHANT", 16, Grey).rectTransform, 22, 12, 300, 24);
        var tip = Txt("Tip", tipBox.transform, "", 19, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Normal);
        TopLeft(tip.rectTransform, 22, 40, 376, 52);

        // ---- toast + hints
        var toast = Txt("Toast", screen, "", 24, new Color(1f, 0.82f, 0.48f), TextAlignmentOptions.Center);
        TopLeft(toast.rectTransform, 560, 935, 800, 44);
        toast.gameObject.SetActive(false);

        var backImage = Img("BackButton", screen, Color.clear, true);
        TopLeft(backImage.rectTransform, 100, 995, 250, 40);
        var backButton = backImage.gameObject.AddComponent<Button>();
        backButton.targetGraphic = backImage;
        TopLeft(Badge("BackKey", backImage.transform, "Esc", 70, 40), 0, 0, 70, 40);
        TopLeft(LTxt("BackLabel", backImage.transform, "SHOP_BACK", 28, Color.white).rectTransform, 85, 0, 160, 40);
        TopLeft(LTxt("SwitchLabel", screen, "SHOP_SWITCH_TAB", 28, Color.white, TextAlignmentOptions.Right).rectTransform, 1480, 995, 240, 40);
        TopLeft(Badge("SwitchKey", screen, "Q", 60, 40), 1740, 995, 60, 40);

        // ---- wiring
        SetRefs(view,
            ("screenRoot", screen.gameObject), ("backButton", backButton),
            ("goldText", goldText), ("gemText", gemText),
            ("buyTabButton", buyTab), ("sellTabButton", sellTab), ("buyUnderline", buyLine), ("sellUnderline", sellLine),
            ("buyPanel", buyPanel.gameObject), ("stockCountText", stockCount),
            ("sellPanel", sellPanel.gameObject), ("bagContent", content),
            ("bagSlotPrefab", slotPrefab.GetComponent<InventorySlotView>()),
            ("selectionFrame", selection.rectTransform), ("sellCountText", sellCount),
            ("detail", detailView), ("actionGroup", action.gameObject),
            ("priceLabel", priceLabel), ("priceText", priceText), ("actionButton", actionButton),
            ("actionLabel", actionLabel), ("noteText", note),
            ("tipText", tip), ("toastText", toast));
        SetArray(view, "tiles", tiles);
        SetArray(view, "categoryButtons", categoryButtons);
        SetArray(view, "categoryUnderlines", categoryLines);

        screen.gameObject.SetActive(false);
        return canvasGo;
    }

    private static Button BuildTopTab(Transform parent, string name, string key, float x, out GameObject underline)
    {
        var image = Img(name, parent, new Color(0, 0, 0, 0), true);
        TopLeft(image.rectTransform, x, 10, 120, 62);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        Stretch(LTxt("Label", image.transform, key, 26, Color.white, TextAlignmentOptions.Center).rectTransform, 0, 0, 0, 6);
        var line = Img("Underline", image.transform, Orange);
        line.rectTransform.anchorMin = new Vector2(0, 0);
        line.rectTransform.anchorMax = new Vector2(1, 0);
        line.rectTransform.offsetMin = Vector2.zero;
        line.rectTransform.offsetMax = new Vector2(0, 6);
        underline = line.gameObject;
        return button;
    }

    // Note box at the bottom of the left frame: a sentence on the left, a counter on the right. Returns the counter.
    private static TMP_Text BuildInfoBox(RectTransform parent, string noteKey, string counter)
    {
        var box = Img("InfoBox", parent, Box);
        TopLeft(box.rectTransform, 40, 490, 540, 70);
        TopLeft(LTxt("Note", box.transform, noteKey, 18, Grey, TextAlignmentOptions.Left, FontStyles.Normal).rectTransform, 20, 12, 380, 46);
        var count = Txt("Count", box.transform, counter, 20, Color.white, TextAlignmentOptions.MidlineRight);
        TopLeft(count.rectTransform, 410, 20, 110, 30);
        return count;
    }

    private static Sprite[] CategoryIcons() => new[] { Options.Icon("UI_Sword"), Options.Icon("UI_Bow"), Options.Icon("UI_Armor"), Options.Icon("UI_Rune"), Options.Icon("UI_Artifact") };

    private static RectTransform BuildScrollGrid(Transform parent, string name, float x, float y, float w, float h,
        Vector2 cell, Vector2 spacing, int columns)
    {
        var root = NewRect(name, parent);
        TopLeft(root, x, y, w, h);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = Img("Viewport", root, Color.clear, true);
        Stretch(viewport.rectTransform);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = NewRect("Content", viewport.transform);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cell; grid.spacing = spacing;
        grid.padding = new RectOffset(4, 4, 4, 4);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content; scroll.viewport = viewport.rectTransform;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        return content;
    }

    private static Image CurrencyIcon(string name, Transform parent, Sprite sprite, Color fallback)
    {
        var image = Img(name, parent, sprite != null ? Color.white : fallback);
        image.sprite = sprite; image.preserveAspect = true;
        return image;
    }

    private static TMP_Text CurrencyChip(Transform parent, float x, Sprite sprite, Color color, string value)
    {
        TopLeft(CurrencyIcon("CurrencyIcon", parent, sprite, color).rectTransform, x, 26, 28, 28);
        var text = Txt("CurrencyText", parent, value, 28, Color.white);
        TopLeft(text.rectTransform, x + 40, 20, 190, 40);
        Fit(text, 18, 28);
        return text;
    }

    private static void Fit(TMP_Text text, float min, float max)
    {
        text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
    }
    private static RectTransform NewRect(string name, Transform parent) => CapstoneUIEditor.Rect(name, parent, 0, 0, 100, 100);
    private static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h) => CapstoneUIEditor.Place(rt, x, y, w, h);
    private static void Stretch(RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }
    private static Image Img(string name, Transform parent, Color color, bool raycast = false) => CapstoneUIEditor.Image(name, parent, 0, 0, 100, 100, color, null, raycast);
    private static TextMeshProUGUI Txt(string name, Transform parent, string value, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold)
    {
        var text = CapstoneUIEditor.Text(name, parent, 0, 0, 100, 100, value, size, color, Options, align);
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }
    private static TextMeshProUGUI LTxt(string name, Transform parent, string key, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left, FontStyles style = FontStyles.Bold)
    {
        var text = Txt(name, parent, InventoryText.Defaults.TryGetValue(key, out var value) ? value : key, size, color, align, style);
        var loc = text.gameObject.AddComponent<LocalizationText>();
        CapstoneUIEditor.Ref(loc, "text", text);
        var field = typeof(LocalizationText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new InvalidOperationException("LocalizationText.localizedString is missing.");
        field.SetValue(loc, InventoryText.Ref(key));
        EditorUtility.SetDirty(loc);
        return text;
    }
    private static RectTransform Badge(string name, Transform parent, string label, float w, float h)
    {
        var image = Img(name, parent, new Color(.027f, .082f, .118f, 1));
        image.rectTransform.sizeDelta = new Vector2(w, h);
        Stretch(Txt("Label", image.transform, label, h * .5f, Color.white, TextAlignmentOptions.Center).rectTransform);
        return image.rectTransform;
    }
    private static HorizontalLayoutGroup HLG(GameObject go, float spacing)
    {
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        return layout;
    }
    private static LayoutElement LE(GameObject go, float prefW = -1, float prefH = -1)
    {
        var layout = go.AddComponent<LayoutElement>();
        if (prefW >= 0) { layout.preferredWidth = prefW; layout.minWidth = prefW; }
        if (prefH >= 0) layout.preferredHeight = prefH;
        return layout;
    }
    private static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        foreach (var pair in refs) CapstoneUIEditor.Ref(target, pair.field, pair.value);
    }
    private static void SetArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field) ?? throw new InvalidOperationException("Missing field: " + field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ValidateBuiltRoot(GameObject root)
    {
        var view = root.GetComponent<ShopScreenView>();
        if (view == null) throw new InvalidOperationException("Select the ShopCanvas root with ShopScreenView.");
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0)
                throw new InvalidOperationException("Missing script on " + node.name);
        var so = new SerializedObject(view);
        foreach (string field in new[] { "screenRoot", "backButton", "goldText", "gemText", "buyTabButton", "sellTabButton",
            "buyUnderline", "sellUnderline", "buyPanel", "stockCountText", "sellPanel", "bagContent", "bagSlotPrefab",
            "selectionFrame", "sellCountText", "detail", "actionGroup", "priceLabel", "priceText", "actionButton",
            "actionLabel", "noteText", "tipText", "toastText" })
            if (so.FindProperty(field)?.objectReferenceValue == null) throw new InvalidOperationException("Shop reference missing: " + field);
        foreach (string field in new[] { "tiles", "categoryButtons", "categoryUnderlines" })
        {
            var array = so.FindProperty(field);
            int expected = field == "tiles" ? StockCapacity : 5;
            if (array == null || array.arraySize != expected) throw new InvalidOperationException("Shop array has the wrong size: " + field);
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new InvalidOperationException("Shop array contains an empty reference: " + field);
        }
        var slot = (InventorySlotView)so.FindProperty("bagSlotPrefab").objectReferenceValue;
        if (slot.transform.Find("SellPrice/Value")?.GetComponent<TMP_Text>() == null)
            throw new InvalidOperationException("ShopSellSlot price overlay is missing. Rebuild Shop UI.");
        var detail = new SerializedObject(so.FindProperty("detail").objectReferenceValue);
        if (detail.FindProperty("statRowPrefab")?.objectReferenceValue == null)
            throw new InvalidOperationException("Shop detail stat row prefab is missing.");
    }
}
#endif