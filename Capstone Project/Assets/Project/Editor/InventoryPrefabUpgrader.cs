#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryPrefabUpgrader
{
    // path suffix (relative to the prefab root) -> localization key
    private static readonly (string path, string key)[] Labels =
    {
        ("TopBar/Tab", "INV_TITLE"), ("LevelLabel", "INV_LEVEL"), ("ExpLabel", "INV_EXPERIENCE"),
        ("PlayerStatsButton/Label", "INV_PLAYER_STATS"), ("StatsPanel/Header/Text", "INV_PLAYER_STATS_HEADER"),
        ("Cap_Weapons", "INV_WEAPONS"), ("Label_Melee", "INV_MELEE"), ("Label_Ranged", "INV_RANGED"),
        ("Cap_Armor", "INV_ARMOR"), ("Label_Armor", "INV_ARMOR"), ("Cap_Artifacts", "INV_ARTIFACTS"),
        ("Tab_MELEE/Label", "INV_MELEE"), ("Tab_RANGED/Label", "INV_RANGED"), ("Tab_ARMOR/Label", "INV_ARMOR"),
        ("Tab_RUNE/Label", "INV_RUNE"), ("Tab_ARTIFACT/Label", "INV_ARTIFACT"),
        ("BagHint", "INV_BAG_HINT"), ("BackLabel", "INV_BACK"), ("SelectLabel", "INV_EQUIP_UNEQUIP"),
        ("StatsLabel", "DETAIL_STATS"), ("SocketsLabel", "DETAIL_SOCKETS"),
        ("Row_Health/Label", "STAT_HEALTH"), ("Row_Damage/Label", "STAT_DAMAGE"), ("Row_Defense/Label", "STAT_DEFENSE"),
        ("Row_Move Speed/Label", "STAT_MOVE_SPEED"), ("Row_Crit Chance/Label", "STAT_CRIT_CHANCE"),
        ("Row_Crit Damage/Label", "STAT_CRIT_DAMAGE"), ("Row_Attack Speed/Label", "STAT_ATTACK_SPEED"),
    };

    [MenuItem("Tools/UI/Upgrade Selected Inventory Prefab")]
    public static void Upgrade()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("[Inventory] Select the inventory prefab asset (or its instance in the scene) first.");
            return;
        }

        string assetPath = EditorUtility.IsPersistent(selected)
            ? AssetDatabase.GetAssetPath(selected)
            : AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(selected));
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("[Inventory] The selection is not a prefab (or an instance of one).");
            return;
        }

        // Load the source only for reading; save to a new path with new generated dependencies.
        var root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            var oldFont = root.GetComponentInChildren<TMP_Text>(true)?.font;
            var options = new CapstoneUIBuildOptions
            {
                IconFolder = "Assets/CapstoneUI/Icons",
                Font = oldFont != null ? oldFont : TMP_Settings.defaultFontAsset,
                CharacterPreview = root.GetComponentInChildren<RawImage>(true)?.texture as RenderTexture
            };
            CapstoneUIEditor.ValidateInputs(options, "Assets/CapstoneUI/Generated", false, true);
            CapstoneUIEditor.ResolveIcons(options);
            options.Output = AssetDatabase.GenerateUniqueAssetPath("Assets/CapstoneUI/Generated/Upgrade_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(options.Output);
            AssetDatabase.Refresh();
            CapstoneUIEditor.CreateShapes(options);
            Apply(root);
            InventoryUIBuilder.BuildDependenciesForCopy(root, options);
            var copy = CapstoneUIEditor.Save(root, options.Output + "/InventoryCanvas.prefab");
            Selection.activeObject = copy;
            InventoryLocalizationTool.AddKeys();
            AssetDatabase.SaveAssets();
            Debug.Log("[Inventory] Visual upgrade saved as a new prefab: " + AssetDatabase.GetAssetPath(copy));
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Apply(GameObject root)
    {
        var view = root.GetComponentInChildren<InventoryScreenView>(true);
        if (view == null)
        {
            throw new InvalidOperationException("[Inventory] No InventoryScreenView found in this prefab.");
        }

        var bg = view.GetComponent<Image>();
        if (bg != null) bg.raycastTarget = true;


        var detail = root.GetComponentInChildren<InventoryDetailView>(true);
        if (detail != null)
        {
            var group = detail.GetComponent<CanvasGroup>();
            if (group == null) group = detail.gameObject.AddComponent<CanvasGroup>();
            SetRef(detail, "canvasGroup", group);
        }

        var matched = new HashSet<string>();
        int added = 0;
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.GetComponent<LocalizationText>() != null) continue;

            string path = RelativePath(text.transform, root.transform);
            foreach (var (suffix, key) in Labels)
            {
                if (path != suffix && !path.EndsWith("/" + suffix)) continue;
                Attach(text, key);
                matched.Add(suffix);
                added++;
                break;
            }
        }

        var missing = new List<string>();
        foreach (var (suffix, _) in Labels)
        {
            if (!matched.Contains(suffix) && !HasLocalized(root, suffix)) missing.Add(suffix);
        }
        Debug.Log($"[Inventory] {added} labels now use LocalizationText." +
                  (missing.Count > 0 ? " Not found (renamed or removed, add by hand if needed): " + string.Join(", ", missing) : ""));
    }

    private static bool HasLocalized(GameObject root, string suffix)
    {
        foreach (var loc in root.GetComponentsInChildren<LocalizationText>(true))
        {
            string path = RelativePath(loc.transform, root.transform);
            if (path == suffix || path.EndsWith("/" + suffix)) return true;
        }
        return false;
    }

    private static void Attach(TMP_Text text, string key)
    {
        var loc = text.gameObject.AddComponent<LocalizationText>();
        var so = new SerializedObject(loc);
        so.FindProperty("text").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
        typeof(LocalizationText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(loc, InventoryText.Ref(key));
        EditorUtility.SetDirty(loc);

        if (InventoryText.Defaults.TryGetValue(key, out var fallback)) text.text = fallback;
    }

    private static string RelativePath(Transform t, Transform root)
    {
        var parts = new List<string>();
        while (t != null && t != root)
        {
            parts.Insert(0, t.name);
            t = t.parent;
        }
        return string.Join("/", parts);
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            throw new InvalidOperationException($"[Inventory] Field '{field}' not found on {target.GetType().Name}.");
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
