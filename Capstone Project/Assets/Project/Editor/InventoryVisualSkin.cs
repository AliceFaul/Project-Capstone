#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public static class InventoryVisualSkin
{
    private static readonly string[] StatNames = { "Health", "Damage", "Defense", "Move Speed", "Crit Chance", "Crit Damage", "Attack Speed" };
    private static readonly string[] StatIcons = { "UI_Heart", "UI_Sword", "Stat_Defense", "Stat_MoveSpeed", "Stat_CritChance", "Stat_CritDamage", "Stat_AttackSpeed" };

    public static void Apply(GameObject root, CapstoneUIBuildOptions o)
    {
        var view = root.GetComponentInChildren<InventoryScreenView>(true);
        if (view == null) throw new InvalidOperationException("No InventoryScreenView in selected prefab.");
        Transform screen = view.transform;
        var scaler = root.GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f; }
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.font = o.Font;
        foreach (var slot in root.GetComponentsInChildren<InventorySlotView>(true)) ApplySlot(slot, o);

        Place(screen, "ExpLabel", 284, 120, 560, 30);
        Place(screen, "ExpBar", 284, 156, 560, 22);
        Place(screen, "ExpText", 284, 186, 560, 32);
        var statsButton = Need(screen, "PlayerStatsButton");
        Glyph(statsButton, "StatsIcon", o.Icon("UI_TotalStats"), 14, 9, 48, 48);
        Place(statsButton, "Label", 78, 12, 230, 40);

        var preview = Need(screen, "CharacterPreview").GetComponent<RawImage>();
        preview.texture = o.CharacterPreview;
        preview.color = o.CharacterPreview != null ? Color.white : new Color(0,0,0,.12f);
        var previewLabel = preview.transform.Find("Label");
        if (previewLabel != null) previewLabel.gameObject.SetActive(false);

        string[] tabs = { "MELEE", "RANGED", "ARMOR", "RUNE", "ARTIFACT" };
        string[] icons = { "UI_Sword", "UI_Bow", "UI_Armor", "UI_Rune", "UI_Artifact" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var tab = Need(screen, "Tab_" + tabs[i]);
            CapstoneUIEditor.Place((RectTransform)tab, 960 + i * 88, 188, 84, 98);
            Glyph(tab, "Icon", o.Icon(icons[i]), 11, 0, 62, 62);
            Place(tab, "Label", 0, 62, 84, 28);
        }
        Glyph(screen, "BagIcon", o.Icon("UI_Backpack"), 1285, 764, 46, 46);
        Place(screen, "BagCount", 1336, 774, 64, 30);
        var bagCount = Need(screen, "BagCount").GetComponent<TMP_Text>();
        bagCount.enableAutoSizing = true; bagCount.fontSizeMin = 14; bagCount.fontSizeMax = 20;
        var viewport = Need(screen, "BagScroll/Viewport");
        var hit = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear; hit.raycastTarget = true;
        ConfigureCurrency(Need(screen, "TopBar"), o);

        var detail = root.GetComponentInChildren<InventoryDetailView>(true);
        if (detail == null) throw new InvalidOperationException("No InventoryDetailView in selected prefab.");
        ConfigureDetail(detail, o);
        var stats = Need(screen, "StatsPanel/Rows");
        for (int i = 0; i < StatNames.Length; i++)
        {
            var row = Need(stats, "Row_" + StatNames[i]);
            Glyph(row, "Icon", o.Icon(StatIcons[i]), 2, 4, 38, 38);
            var label = (RectTransform)Need(row, "Label");
            label.offsetMin = new Vector2(48, 0); label.offsetMax = new Vector2(-76, 0);
            var text = label.GetComponent<TMP_Text>();
            text.fontSize = 19; text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 19;
            var value = (RectTransform)Need(row, "Value");
            value.anchorMin = new Vector2(1,0); value.anchorMax = Vector2.one;
            value.offsetMin = new Vector2(-74,0); value.offsetMax = new Vector2(-10,0);
        }
        ValidateReferences(view, detail);
    }

    public static void ApplySlot(InventorySlotView view, CapstoneUIBuildOptions o)
    {
        var so = new SerializedObject(view);
        var icon = (Image)so.FindProperty("icon").objectReferenceValue;
        var inner = view.transform.Find("Inner");
        var tint = EnsureImage(view.transform, "RarityTint");
        CapstoneUIEditor.Stretch(tint.rectTransform, 3);
        tint.color = new Color(1,1,1,.18f); tint.enabled = false;
        tint.transform.SetSiblingIndex(inner != null ? inner.GetSiblingIndex() + 1 : 0);
        var empty = EnsureImage(view.transform, "EmptyPlaceholder");
        CapstoneUIEditor.Stretch(empty.rectTransform, 3);
        empty.preserveAspect = true; empty.color = new Color(1,1,1,.27f);
        empty.sprite = view.kind != SlotKind.Equipped ? null : o.Icon(view.equipSlot == EquipSlot.Melee ? "UI_Sword" : view.equipSlot == EquipSlot.Ranged ? "UI_Bow" : view.equipSlot == EquipSlot.Armor ? "UI_Armor" : "UI_Artifact");
        empty.enabled = empty.sprite != null;
        empty.transform.SetSiblingIndex(tint.transform.GetSiblingIndex() + 1);
        var badge = Glyph(view.transform, "EquippedBadge", o.Icon("UI_Equipped"), 0, 0, 25, 25);
        badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1,1);
        badge.rectTransform.pivot = new Vector2(1,1);
        badge.rectTransform.anchoredPosition = new Vector2(-2,-2);
        badge.gameObject.SetActive(false);
        icon.preserveAspect = true;
        so.FindProperty("emptyPlaceholder").objectReferenceValue = empty;
        so.FindProperty("rarityTint").objectReferenceValue = tint;
        so.FindProperty("equippedBadge").objectReferenceValue = badge.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDetail(InventoryDetailView detail, CapstoneUIBuildOptions o)
    {
        var so = new SerializedObject(detail);
        ConfigureStatIcons(so, o);
        Set(so, "cooldownIcon", o.Icon("Stat_Cooldown")); Set(so, "qualityIcon", o.Icon("UI_Quality"));
        Set(so, "durationIcon", o.Icon("UI_Duration")); Set(so, "effectIcon", o.Icon("UI_Rune"));
        Set(so, "emptySocketIcon", o.Icon("UI_SocketEmpty")); Set(so, "lockedSocketIcon", o.Icon("UI_Lock")); Set(so, "runeFallbackIcon", o.Icon("UI_Rune"));
        var squares = so.FindProperty("socketSquares");
        var glyphs = so.FindProperty("socketIcons"); glyphs.arraySize = squares.arraySize;
        for (int i = 0; i < squares.arraySize; i++)
        {
            var square = (Image)squares.GetArrayElementAtIndex(i).objectReferenceValue;
            var glyph = Glyph(square.transform, "Icon", o.Icon("UI_SocketEmpty"), 0, 0, 56, 44);
            glyph.color = new Color(1,1,1,.35f);
            glyphs.GetArrayElementAtIndex(i).objectReferenceValue = glyph;
            var label = square.transform.Find("Label") as RectTransform;
            if (label != null) CapstoneUIEditor.Place(label, 0, 38, 56, 18);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        // Long item/rune descriptions scroll instead of spilling out of the panel.
        var panel = detail.transform;
        var body = panel.Find("Body") as RectTransform;
        if (body != null)
        {
            var viewport = CapstoneUIEditor.Rect("Viewport", panel, 0, 44, 380, 636);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero; viewport.offsetMax = new Vector2(0,-44);
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            body.SetParent(viewport, false);
            body.anchorMin = new Vector2(0,1); body.anchorMax = Vector2.one; body.pivot = new Vector2(.5f,1);
            body.anchoredPosition = Vector2.zero; body.sizeDelta = Vector2.zero;
            var fitter = body.GetComponent<ContentSizeFitter>() ?? body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.GetComponent<ScrollRect>() ?? panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = body; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        }
    }

    public static void ConfigureStatIcons(SerializedObject so, CapstoneUIBuildOptions o)
    {
        string[] stats = { "Health", "Damage", "Defense", "MoveSpeed", "CritChance", "CritDamage", "AttackSpeed", "AttackRange" };
        var entries = so.FindProperty("statIcons"); entries.arraySize = stats.Length;
        for (int i = 0; i < stats.Length; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("stat").intValue = (int)(BonusStat)Enum.Parse(typeof(BonusStat), stats[i]);
            entry.FindPropertyRelative("icon").objectReferenceValue = o.Icon(i < StatIcons.Length ? StatIcons[i] : "UI_Bow");
        }
    }

    private static void ConfigureCurrency(Transform top, CapstoneUIBuildOptions o)
    {
        int index = 0;
        foreach (Transform child in top)
        {
            if (child.name != "CurrencyIcon") continue;
            var image = child.GetComponent<Image>();
            Sprite sprite = index++ == 0 ? o.Gold : o.Gem;
            if (sprite != null) { image.sprite = sprite; image.color = Color.white; image.preserveAspect = true; }
        }
    }
    private static void ValidateReferences(InventoryScreenView view, InventoryDetailView detail)
    {
        var so = new SerializedObject(view);
        foreach (string field in new[]{"meleeSlot","rangedSlot","armorSlot","bagContent","bagSlotPrefab","detail","detailPanel","statsPanel","toastText","popup"})
            if (so.FindProperty(field)?.objectReferenceValue == null) throw new InvalidOperationException("Inventory reference missing: " + field);
        if (so.FindProperty("tabButtons").arraySize != 5 || so.FindProperty("artifactSlots").arraySize != 3)
            throw new InvalidOperationException("Expected five inventory tabs and three artifact slots.");
        var ds = new SerializedObject(detail);
        if (ds.FindProperty("statRowPrefab").objectReferenceValue == null) throw new InvalidOperationException("Stat row prefab is missing.");
    }
    private static void Set(SerializedObject so, string name, UnityEngine.Object value) => so.FindProperty(name).objectReferenceValue = value;
    private static Transform Need(Transform root, string path) => root.Find(path) ?? throw new InvalidOperationException("Inventory hierarchy not recognized: " + path);
    private static void Place(Transform root, string path, float x, float y, float w, float h) => CapstoneUIEditor.Place((RectTransform)Need(root,path),x,y,w,h);
    private static Image EnsureImage(Transform parent, string name)
    {
        var child = parent.Find(name);
        var image = child != null ? child.GetComponent<Image>() : CapstoneUIEditor.Image(name,parent,0,0,32,32,Color.white);
        if (image == null) throw new InvalidOperationException("Expected Image at " + name);
        image.raycastTarget = false;
        return image;
    }
    private static Image Glyph(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
    {
        var image = EnsureImage(parent,name);
        CapstoneUIEditor.Place(image.rectTransform,x,y,w,h);
        image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
        return image;
    }
}
#endif
