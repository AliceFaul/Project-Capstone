#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LevelDataBuilder
{
    [MenuItem("Tools/Level/Create Sample Level Data")]
    public static void Create()
    {
        const string dir = "Assets/Project/Scriptable Object/Level";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        var power = Load<CombatPowerConfig>(dir + "/CombatPowerConfig.asset");

        var level = Load<LevelData>(dir + "/Level_01.asset");
        level.id = "level_01";
        level.sceneName = "Mhieu";
        level.preloadWithLoadingScene = false;
        level.fallbackName = "Forest";
        level.fallbackDescription = "The first level.";
        EditorUtility.SetDirty(level);

        var catalog = Load<LevelCatalog>(dir + "/LevelCatalog.asset");
        var so = new SerializedObject(catalog);
        var levels = so.FindProperty("levels");
        if (levels.arraySize == 0)
        {
            levels.arraySize = 1;
            levels.GetArrayElementAtIndex(0).objectReferenceValue = level;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = catalog;
        Debug.Log("[Level] Sample level data created in " + dir + ". Assign the catalog and the power config to the LevelGate in the lobby.");
    }

    private static T Load<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
#endif