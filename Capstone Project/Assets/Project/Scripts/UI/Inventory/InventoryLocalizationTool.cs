using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

/// <summary>
/// Tools > UI > Add Inventory Localization Keys
/// Adds every InventoryText key to the "UI" string table (all locales get the English default; existing entries are never overwritten).
/// Translate the new entries afterward in the Localization Tables window.
/// </summary>
public static class InventoryLocalizationTool
{
    [MenuItem("Tools/UI/Add Inventory Localization Keys")]
    public static void AddKeys()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection(InventoryText.Table);
        if (collection == null)
        {
            Debug.LogWarning($"[Inventory] String table collection '{InventoryText.Table}' not found - create it first, then run this tool again.");
            return;
        }

        int added = 0;
        foreach (var pair in InventoryText.Defaults)
        {
            if (collection.SharedData.GetEntry(pair.Key) == null) collection.SharedData.AddKey(pair.Key);

            foreach (var table in collection.StringTables)
            {
                if (table.GetEntry(pair.Key) != null) continue;
                table.AddEntry(pair.Key, pair.Value);
                EditorUtility.SetDirty(table);
                added++;
            }
        }

        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Inventory] Localization keys checked, {added} new entries added to '{InventoryText.Table}'.");
    }
}