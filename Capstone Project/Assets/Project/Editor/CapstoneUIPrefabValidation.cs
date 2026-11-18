#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CapstoneUIPrefabValidation
{
    [MenuItem("Tools/UI/Validate Selected Capstone Prefab")]
    public static void ValidateSelected()
    {
        var selected = Selection.activeGameObject;
        if (selected == null) throw new InvalidOperationException("Select a HUD or Inventory prefab first.");
        string path = AssetDatabase.GetAssetPath(selected);
        bool load = !string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        GameObject root = load ? PrefabUtility.LoadPrefabContents(path) : selected;
        try
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0)
                    throw new InvalidOperationException("Missing script on " + t.name);
            var hud = root.GetComponentInChildren<PlayerHUDView>(true);
            var inventory = root.GetComponentInChildren<InventoryScreenView>(true);
            var popup = root.GetComponentInChildren<RuneSocketPopup>(true);
            if (hud == null && inventory == null && popup == null)
                throw new InvalidOperationException("Select a HUD, Inventory or RuneSocketPopup prefab.");
            if (hud != null)
            {
                PlayerHUDPrefabBuilder.Validate(hud);
                var so = new SerializedObject(hud);
                int artifacts = ValidateSlots(so.FindProperty("artifactSlots"));
                int abilities = ValidateSlots(so.FindProperty("abilitySlots"));
                if (artifacts != 3 || abilities != 2) throw new InvalidOperationException("Expected three wired artifact slots and two wired ability slots.");
                var controller = root.GetComponentInChildren<PlayerHUDController>(true);
                Require(controller, "view");
            }
            if (inventory != null)
            {
                foreach (string field in new[]{"levelText","expText","expFill","goldText","gemText","meleeSlot","rangedSlot","armorSlot","bagContent","bagSlotPrefab","bagCountText","detail","detailPanel","statsPanel","toastText"}) Require(inventory,field);
                Require(inventory,"popup");
                var detail = root.GetComponentInChildren<InventoryDetailView>(true);
                Require(detail,"statRowPrefab"); Require(detail,"canvasGroup");
                var so = new SerializedObject(inventory);
                var bagPrefab = so.FindProperty("bagSlotPrefab").objectReferenceValue as InventorySlotView;
                if (bagPrefab == null || bagPrefab.kind != SlotKind.Bag) throw new InvalidOperationException("Bag slot prefab must use SlotKind.Bag.");
                var wiredPopup = so.FindProperty("popup").objectReferenceValue as RuneSocketPopup;
                if (wiredPopup == null || !wiredPopup.transform.IsChildOf(inventory.transform))
                    throw new InvalidOperationException("Popup must be a child of InventoryScreenView for modal navigation.");
                RequireArray(inventory,"artifactSlots",3); RequireArray(inventory,"tabButtons",5); RequireArray(inventory,"tabUnderlines",5);
                ValidatePopup(wiredPopup);
            }
            else if (popup != null) ValidatePopup(popup);
            Debug.Log("[Capstone UI] Serialized references and structural checks passed: " + root.name + ". Play Mode is still required to check gameplay/input/localization.", selected);
        }
        finally { if (load) PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ValidatePopup(RuneSocketPopup popup)
    {
        foreach (string field in new[]{"backdrop","weaponIcon","weaponLevel","weaponName","weaponSub","bagContent","bagSlotPrefab","bagCountText","selectionFrame","bagScroll","cardContent","hintText","runeName","rarityBg","rarityText","rowsRoot","statRowPrefab","descGroup","descText","warnGroup","warnText","insertButton","cancelButton","gemText","qualityGroup","qualityFill","qualityValue","durationIcon","effectIcon","meleeFallbackIcon","rangedFallbackIcon"})
            Require(popup,field);
        RequireArray(popup,"rows",3);
        var so = new SerializedObject(popup);
        var rows = so.FindProperty("rows");
        for (int i = 0; i < rows.arraySize; i++)
        {
            var row = rows.GetArrayElementAtIndex(i).objectReferenceValue as RuneSocketRowView;
            if (row == null || !row.transform.IsChildOf(popup.transform)) throw new InvalidOperationException("Socket row must be inside the popup.");
            foreach (string field in new[]{"frame","icon","iconText","nameText","detailText","removeButton","removeLabel","costText","group","glyph","emptySprite","lockedSprite","runeSprite","socketText","stateText"})
                Require(row,field);
        }
        var backdrop = so.FindProperty("backdrop").objectReferenceValue as Image;
        if (!backdrop.raycastTarget) throw new InvalidOperationException("Popup backdrop must block raycasts.");
        var scroll = so.FindProperty("bagScroll").objectReferenceValue as ScrollRect;
        if (scroll.content != so.FindProperty("bagContent").objectReferenceValue || scroll.viewport == null || scroll.horizontal)
            throw new InvalidOperationException("Rune bag scroll references are inconsistent.");
        var grid = scroll.content.GetComponent<GridLayoutGroup>();
        if (grid == null || grid.constraint != GridLayoutGroup.Constraint.FixedColumnCount || grid.constraintCount != 6)
            throw new InvalidOperationException("Rune bag must have six fixed columns.");
        var slot = so.FindProperty("bagSlotPrefab").objectReferenceValue as InventorySlotView;
        if (slot == null || slot.kind != SlotKind.Bag) throw new InvalidOperationException("Rune bag slot must use SlotKind.Bag.");
        var close = popup.transform.Find("Dialog/Header/Close")?.GetComponent<Button>();
        if (close == null || close.onClick.GetPersistentEventCount() != 1 || close.onClick.GetPersistentTarget(0) != popup || close.onClick.GetPersistentMethodName(0) != nameof(RuneSocketPopup.Close))
            throw new InvalidOperationException("Close button is not wired to this popup.");
        if (!Application.isPlaying && popup.gameObject.activeSelf)
            throw new InvalidOperationException("Save RuneSocketPopup inactive; InventoryScreenView opens it with player data.");
    }

    private static void RequireArray(UnityEngine.Object target,string field,int count)
    {
        var array = new SerializedObject(target).FindProperty(field);
        if (array == null || !array.isArray || array.arraySize != count) throw new InvalidOperationException(target.name+"."+field+": wrong array size.");
        for (int i = 0; i < count; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == null)
            throw new InvalidOperationException(target.name+"."+field+"["+i+"]: unassigned reference.");
    }
    private static int ValidateSlots(SerializedProperty array)
    {
        int wired = 0;
        for (int i=0;i<array.arraySize;i++)
        {
            var slot = array.GetArrayElementAtIndex(i);
            var icon = slot.FindPropertyRelative("icon").objectReferenceValue;
            var placeholder = slot.FindPropertyRelative("placeholder").objectReferenceValue;
            var overlay = slot.FindPropertyRelative("cooldownOverlay").objectReferenceValue as Image;
            if (icon == null && overlay == null && placeholder == null) continue;
            if (icon == null || overlay == null || placeholder == null || overlay.type != Image.Type.Filled)
                throw new InvalidOperationException("Incomplete cooldown slot: " + array.propertyPath + "[" + i + "]");
            wired++;
        }
        return wired;
    }
    private static void Require(UnityEngine.Object component,string field)
    {
        if (component == null) throw new InvalidOperationException("Component missing for field: " + field);
        var p = new SerializedObject(component).FindProperty(field);
        if (p == null || p.objectReferenceValue == null) throw new InvalidOperationException(component.GetType().Name + "." + field + " is not assigned.");
    }
}
#endif
