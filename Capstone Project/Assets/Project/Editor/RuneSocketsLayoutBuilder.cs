#if UNITY_EDITOR
using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using static CapstoneUIEditor;

// Creates a wired dialog using the project's existing popup and socket row components.
public static class RuneSocketsLayoutBuilder
{
    private static readonly Color Panel = new Color(.045f,.11f,.15f,.98f);
    private static readonly Color Border = new Color(.27f,.42f,.48f);
    private static readonly Color Blue = new Color(.60f,.77f,.85f);
    private static readonly Color Orange = new Color(1,.47f,.055f);

    public static void Attach(GameObject inventory, CapstoneUIBuildOptions o, GameObject slotPrefab, GameObject statRowPrefab)
    {
        var view = inventory.GetComponentInChildren<InventoryScreenView>(true);
        if (view == null) throw new InvalidOperationException("InventoryScreenView is missing.");
        var viewData = new SerializedObject(view);
        var previous = viewData.FindProperty("popup").objectReferenceValue as RuneSocketPopup;
        Material backdropMaterial = previous != null && previous.Backdrop != null ? previous.Backdrop.material : null;
        var prefab = Build(o, slotPrefab, statRowPrefab);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, view.transform);
        instance.name = "RuneSocketPopup";
        Stretch((RectTransform)instance.transform);
        var popup = instance.GetComponent<RuneSocketPopup>();
        if (backdropMaterial != null) popup.Backdrop.material = backdropMaterial;
        Ref(view, "popup", popup);
        // Only replace the dialog inside the new inventory copy; never delete a prefab asset.
        if (previous != null && !EditorUtility.IsPersistent(previous) && previous.transform.IsChildOf(view.transform))
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var toast = viewData.FindProperty("toastText").objectReferenceValue as TMP_Text;
        if (toast != null) toast.transform.SetAsLastSibling();
        instance.SetActive(false);
        CapstoneUIPrefabValidation.ValidatePopup(popup);
    }

    private static GameObject Build(CapstoneUIBuildOptions o, GameObject slotPrefab, GameObject statRowPrefab)
    {
        using (var scene = new BuildScene())
        {
            var root = new GameObject("RuneSocketPopup", typeof(RectTransform), typeof(RuneSocketPopup));
            scene.Own(root);
            ((RectTransform)root.transform).sizeDelta = new Vector2(1920,1080);
            var popup = root.GetComponent<RuneSocketPopup>();
            var backdrop = Image("Backdrop",root.transform,0,0,1920,1080,new Color(0,0,0,.66f),null,true);
            Stretch(backdrop.rectTransform);
            var panel = Box("Dialog",root.transform,0,0,1500,960,Panel);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f,.5f);
            panel.anchoredPosition = Vector2.zero;
            var header = Image("Header",panel,0,0,1500,78,new Color(.18f,.30f,.36f));
            Icon("RuneIcon",header.transform,530,0,80,78,o.Icon("UI_Rune"));
            Label("Title",header.transform,622,10,650,58,"POPUP_TITLE",40,Color.white,o);
            Text("Esc",header.transform,1340,17,70,42,"Esc",26,Color.white,o,TextAlignmentOptions.Center);
            var closeIcon = Icon("Close",header.transform,1418,12,56,56,o.Icon("UI_Close"));
            var close = MakeButton(closeIcon.gameObject);
            UnityEventTools.AddPersistentListener(close.onClick, popup.Close);
            Image("Divider",panel,652,100,2,765,Border);

            var weapon = Box("Weapon",panel,36,110,132,132,Panel);
            var weaponIcon = Icon("Icon",weapon,4,4,124,124,o.Icon("UI_Sword"));
            var weaponLevel = Text("Level",weapon,84,96,40,28,"—",22,Color.white,o,TextAlignmentOptions.Right);
            var weaponName = Text("WeaponName",panel,190,120,430,80,"—",34,Color.white,o);
            weaponName.enableAutoSizing = true; weaponName.fontSizeMin = 24; weaponName.fontSizeMax = 34;
            var weaponSub = Text("WeaponType",panel,190,202,430,36,"—",24,Blue,o);
            Label("SocketsLabel",panel,36,272,570,42,"POPUP_SOCKETS",30,Blue,o);
            var rows = new RuneSocketRowView[3];
            for (int i = 0; i < rows.Length; i++) rows[i] = BuildRow(panel, i, o);

            Icon("BagIcon",panel,686,94,60,60,o.Icon("UI_Backpack"));
            Label("BagLabel",panel,756,100,510,46,"POPUP_RUNES_IN_BAG",30,Blue,o);
            var bagCount = Text("BagCount",panel,1322,100,142,46,"0 / 0",28,Color.white,o,TextAlignmentOptions.Right);
            var bag = Rect("RuneBag",panel,686,160,778,228);
            var bagScroll = Scroll(bag, out var bagContent);
            var grid = bagContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(116,104); grid.spacing = new Vector2(12,12);
            grid.padding = new RectOffset(4,4,4,4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 6;
            var selection = Rect("SelectionFrame",bag,0,0,116,104);
            selection.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Ring(selection);
            selection.gameObject.SetActive(false);

            var info = Box("RuneDetails",panel,664,398,810,372,Panel);
            var hint = Label("Hint",info,24,30,762,300,"POPUP_NO_RUNE",25,Blue,o);
            hint.textWrappingMode = TextWrappingModes.Normal;
            var card = Rect("CardContent",info,0,0,810,372);
            Stretch(card);
            var runeName = Text("Name",card,22,10,530,48,"—",32,Color.white,o);
            runeName.enableAutoSizing = true; runeName.fontSizeMin = 22; runeName.fontSizeMax = 32;
            var rarity = Image("RarityBadge",card,566,16,218,38,Border);
            var rarityText = Text("Label",rarity.transform,0,0,218,38,"—",23,Color.white,o,TextAlignmentOptions.Center);
            var details = Rect("DetailsScroll",card,18,62,774,298);
            Scroll(details, out var body);
            Vertical(body,8);
            var qualityGroup = Rect("Quality",body,0,0,774,44);
            Height(qualityGroup,44);
            Icon("Icon",qualityGroup,0,0,40,40,o.Icon("UI_Quality"));
            Label("Label",qualityGroup,52,0,170,44,"DETAIL_QUALITY",24,Color.white,o);
            var qualityTrack = Image("Track",qualityGroup,230,14,420,16,Border);
            var qualityFill = Image("Fill",qualityTrack.transform,0,0,420,16,new Color(.15f,.82f,.91f));
            Stretch(qualityFill.rectTransform);
            var qualityValue = Text("Value",qualityGroup,664,0,100,44,"—",25,Color.white,o,TextAlignmentOptions.Right);
            var rowsRoot = Rect("StatRows",body,0,0,774,0);
            Vertical(rowsRoot,6);
            var description = Rect("Description",body,0,0,774,0);
            Vertical(description,4);
            var descHeader = Label("Header",description,0,0,774,32,"POPUP_DESCRIPTION",22,Blue,o);
            Height(descHeader.rectTransform,32);
            var descText = Text("Text",description,0,0,774,44,"",22,Color.white,o);
            Wrap(descText);
            var warning = Image("ConflictWarning",body,0,0,774,0,new Color(.25f,.18f,.055f));
            warning.gameObject.AddComponent<LayoutElement>().minHeight = 76;
            var warningLayout = Vertical(warning.rectTransform,0);
            warningLayout.padding = new RectOffset(72,14,12,12);
            var warningIcon = Icon("Icon",warning.transform,8,12,52,52,o.Icon("UI_Warning"));
            warningIcon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var warningText = Text("Text",warning.transform,0,0,674,64,"",22,new Color(1,.78f,.32f),o);
            Wrap(warningText);
            warning.gameObject.SetActive(false);
            card.gameObject.SetActive(false);

            var insert = Box("InsertButton",panel,664,794,554,76,Orange);
            Icon("Icon",insert,42,6,62,62,o.Icon("UI_Insert")).color = Color.black;
            Label("Label",insert,128,10,400,56,"POPUP_INSERT",29,Color.black,o);
            var insertButton = MakeButton(insert.gameObject);
            insertButton.interactable = false;
            var cancel = Box("CancelButton",panel,1240,794,234,76,new Color(.18f,.30f,.36f));
            Label("Label",cancel,14,10,138,56,"POPUP_CANCEL",26,Color.white,o);
            Text("Esc",cancel,166,18,58,40,"Esc",20,Blue,o,TextAlignmentOptions.Center);
            var cancelButton = MakeButton(cancel.gameObject);
            var gemIcon = Icon("Gem",panel,1322,900,36,36,o.Gem);
            if (o.Gem == null) gemIcon.color = new Color(.15f,.82f,.91f);
            var gems = Text("GemCount",panel,1368,894,106,48,"0",28,Color.white,o,TextAlignmentOptions.Right);

            Ref(popup,"backdrop",backdrop); Ref(popup,"weaponIcon",weaponIcon);
            Ref(popup,"weaponLevel",weaponLevel); Ref(popup,"weaponName",weaponName); Ref(popup,"weaponSub",weaponSub);
            Ref(popup,"bagContent",bagContent); Ref(popup,"bagSlotPrefab",slotPrefab.GetComponent<InventorySlotView>());
            Ref(popup,"bagCountText",bagCount); Ref(popup,"selectionFrame",selection); Ref(popup,"bagScroll",bagScroll);
            Ref(popup,"cardContent",card.gameObject); Ref(popup,"hintText",hint);
            Ref(popup,"runeName",runeName); Ref(popup,"rarityBg",rarity); Ref(popup,"rarityText",rarityText);
            Ref(popup,"rowsRoot",rowsRoot); Ref(popup,"statRowPrefab",statRowPrefab);
            Ref(popup,"descGroup",description.gameObject); Ref(popup,"descText",descText);
            Ref(popup,"warnGroup",warning.gameObject); Ref(popup,"warnText",warningText);
            Ref(popup,"qualityGroup",qualityGroup.gameObject); Ref(popup,"qualityFill",qualityFill.rectTransform); Ref(popup,"qualityValue",qualityValue);
            Ref(popup,"insertButton",insertButton); Ref(popup,"cancelButton",cancelButton); Ref(popup,"gemText",gems);
            Ref(popup,"durationIcon",o.Icon("UI_Duration")); Ref(popup,"effectIcon",o.Icon("UI_Rune"));
            Ref(popup,"meleeFallbackIcon",o.Icon("UI_Sword")); Ref(popup,"rangedFallbackIcon",o.Icon("UI_Bow"));
            var so = new SerializedObject(popup);
            var array = so.FindProperty("rows"); array.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            InventoryVisualSkin.ConfigureStatIcons(so,o);
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            CapstoneUIPrefabValidation.ValidatePopup(popup);
            return Save(root,o.Output+"/Inventory/RuneSocketPopup.prefab");
        }
    }

    private static RuneSocketRowView BuildRow(Transform parent,int index,CapstoneUIBuildOptions o)
    {
        var row = Box("Socket"+(index+1),parent,36,334+index*174,600,158,Panel);
        MakeButton(row.gameObject);
        var group = row.gameObject.AddComponent<CanvasGroup>();
        var view = row.gameObject.AddComponent<RuneSocketRowView>();
        var icon = Image("IconBackground",row,14,20,116,116,Panel);
        var glyph = Icon("Glyph",icon.transform,4,4,108,92,o.Icon("UI_SocketEmpty"));
        var iconText = Text("UnlockLevel",icon.transform,0,90,116,26,"",20,Blue,o,TextAlignmentOptions.Center);
        var socket = Text("SocketLabel",row,146,10,288,34,"Socket "+(index+1),27,Color.white,o);
        var name = Text("Name",row,146,46,288,34,"—",23,Color.white,o);
        name.enableAutoSizing = true; name.fontSizeMin = 17; name.fontSizeMax = 23;
        var detail = Text("Description",row,146,86,288,62,"—",18,Blue,o);
        detail.textWrappingMode = TextWrappingModes.Normal;
        var state = Text("State",row,448,12,138,30,"",18,Blue,o,TextAlignmentOptions.Right);
        var remove = Box("RemoveButton",row,448,48,138,54,new Color(.18f,.30f,.36f));
        Icon("Icon",remove,4,9,32,32,o.Icon("UI_Remove"));
        var removeLabel = Text("Label",remove,40,4,94,46,"Remove",18,Color.white,o,TextAlignmentOptions.Center);
        removeLabel.enableAutoSizing = true; removeLabel.fontSizeMin = 13; removeLabel.fontSizeMax = 18;
        var removeButton = MakeButton(remove.gameObject);
        var cost = Text("Cost",row,448,108,138,32,"",20,Color.white,o,TextAlignmentOptions.Center);
        Ref(view,"frame",row.GetComponent<Image>()); Ref(view,"icon",icon); Ref(view,"glyph",glyph);
        Ref(view,"iconText",iconText); Ref(view,"socketText",socket); Ref(view,"nameText",name); Ref(view,"detailText",detail);
        Ref(view,"stateText",state); Ref(view,"removeButton",removeButton); Ref(view,"removeLabel",removeLabel);
        Ref(view,"costText",cost); Ref(view,"group",group);
        Ref(view,"emptySprite",o.Icon("UI_SocketEmpty")); Ref(view,"lockedSprite",o.Icon("UI_Lock")); Ref(view,"runeSprite",o.Icon("UI_Rune"));
        remove.gameObject.SetActive(false); cost.gameObject.SetActive(false); state.gameObject.SetActive(false);
        return view;
    }

    private static RectTransform Box(string name,Transform parent,float x,float y,float w,float h,Color fill)
    {
        var border = Image(name,parent,x,y,w,h,Border,null,true);
        var inner = Image("Background",border.transform,1.5f,1.5f,w-3,h-3,fill);
        Stretch(inner.rectTransform,1.5f);
        return border.rectTransform;
    }
    private static Button MakeButton(GameObject go)
    {
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.transform.Find("Background")?.GetComponent<Image>() ?? go.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        var colors = button.colors; colors.highlightedColor = new Color(1.3f,1.3f,1.3f);
        colors.selectedColor = new Color(1.6f,1.6f,1.6f); colors.disabledColor = new Color(.45f,.45f,.45f,.65f);
        button.colors = colors;
        return button;
    }
    private static ScrollRect Scroll(RectTransform parent,out RectTransform content)
    {
        var scroll = parent.gameObject.AddComponent<ScrollRect>();
        var viewport = Rect("Viewport",parent,0,0,parent.rect.width,parent.rect.height);
        Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
        var hit = viewport.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
        content = Rect("Content",viewport,0,0,parent.rect.width,0);
        content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0,1); content.sizeDelta = Vector2.zero;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        return scroll;
    }
    private static VerticalLayoutGroup Vertical(RectTransform rt,float spacing)
    {
        var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = spacing;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        return layout;
    }
    private static void Height(RectTransform rt,float height) => rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
    private static void Wrap(TMP_Text text)
    {
        text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
        text.fontStyle = FontStyles.Normal;
    }
    private static TextMeshProUGUI Label(string name,Transform parent,float x,float y,float w,float h,string key,float size,Color color,CapstoneUIBuildOptions o)
    {
        var text = Text(name,parent,x,y,w,h,InventoryText.Defaults[key],size,color,o);
        var loc = text.gameObject.AddComponent<LocalizationText>();
        Ref(loc,"text",text);
        var field = typeof(LocalizationText).GetField("localizedString",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
        if (field == null) throw new InvalidOperationException("LocalizationText.localizedString is missing.");
        field.SetValue(loc,InventoryText.Ref(key)); EditorUtility.SetDirty(loc);
        return text;
    }
    private static void Ring(RectTransform root)
    {
        var top = Image("Top",root,0,0,0,3,Color.white);
        top.rectTransform.anchorMax = new Vector2(1,1);
        var bottom = Image("Bottom",root,0,0,0,3,Color.white);
        bottom.rectTransform.anchorMin = Vector2.zero; bottom.rectTransform.anchorMax = new Vector2(1,0);
        bottom.rectTransform.pivot = Vector2.zero;
        var left = Image("Left",root,0,0,3,0,Color.white);
        left.rectTransform.anchorMin = Vector2.zero; left.rectTransform.anchorMax = new Vector2(0,1);
        var right = Image("Right",root,0,0,3,0,Color.white);
        right.rectTransform.anchorMin = new Vector2(1,0); right.rectTransform.anchorMax = Vector2.one;
        right.rectTransform.pivot = Vector2.one;
    }
}
#endif
