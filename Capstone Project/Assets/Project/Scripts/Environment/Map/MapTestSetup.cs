using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class MapTestSetup
{
    private static readonly NodeType[] Types =
    {
        NodeType.Combat, NodeType.Combat, NodeType.Combat, NodeType.Combat,
        NodeType.Reward, NodeType.Reward, NodeType.Event, NodeType.Rest,
        NodeType.Elite, NodeType.Elite, NodeType.Shop, NodeType.Boss
    };

    [MenuItem("Tools/Map/Create Graybox Test Areas")]
    public static void Create()
    {
        const string dir = "Assets/Map";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        string configPath = dir + "/NodeMapConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<NodeMapConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<NodeMapConfig>();
            AssetDatabase.CreateAsset(config, configPath);
        }

        var root = new GameObject("LevelRun");
        Undo.RegisterCreatedObjectUndo(root, "Create graybox map");
        var run = root.AddComponent<LevelRunController>();
        var runSo = new SerializedObject(run);
        runSo.FindProperty("config").objectReferenceValue = config;
        runSo.ApplyModifiedPropertiesWithoutUndo();

        int interactLayer = FindInteractLayer();
        var counters = new Dictionary<NodeType, int>();
        for (int i = 0; i < Types.Length; i++)
        {
            counters.TryGetValue(Types[i], out int n);
            counters[Types[i]] = n + 1;
            CreateArea(root.transform, Types[i], n + 1, new Vector3(i * 60f, 0f, 0f), interactLayer);
        }

        Selection.activeGameObject = root;
        Debug.Log("[Map] Graybox areas created. Bake the NavMesh, then press Play (the player must be in the scene).");
    }

    private static void CreateArea(Transform parent, NodeType type, int index, Vector3 origin, int interactLayer)
    {
        var go = new GameObject($"Area_{type}_{index}");
        go.transform.SetParent(parent, false);
        go.transform.position = origin;
        var area = go.AddComponent<AreaController>();

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(go.transform, false);
        floor.transform.localScale = new Vector3(4f, 1f, 2f);
        GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

        var entry = new GameObject("EntryPoint");
        entry.transform.SetParent(go.transform, false);
        entry.transform.localPosition = new Vector3(-15f, 0f, 0f);

        var exits = new AreaExit[3];
        for (int i = 0; i < exits.Length; i++)
        {
            var exitObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            exitObject.name = "Exit" + (i + 1);
            exitObject.transform.SetParent(go.transform, false);
            exitObject.transform.localPosition = new Vector3(16f, 0.75f, (i - 1) * 7f);
            exitObject.transform.localScale = Vector3.one * 1.5f;
            if (interactLayer >= 0) exitObject.layer = interactLayer;

            var exit = exitObject.AddComponent<AreaExit>();
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(exitObject.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            var label = labelObject.AddComponent<TextMeshPro>();
            label.text = "?";
            label.fontSize = 4f;
            label.alignment = TextAlignmentOptions.Center;

            var exitSo = new SerializedObject(exit);
            exitSo.FindProperty("label").objectReferenceValue = label;
            exitSo.ApplyModifiedPropertiesWithoutUndo();
            exits[i] = exit;
        }

        var areaSo = new SerializedObject(area);
        areaSo.FindProperty("type").enumValueIndex = (int)type;
        areaSo.FindProperty("entryPoint").objectReferenceValue = entry.transform;
        var exitsProperty = areaSo.FindProperty("exits");
        exitsProperty.arraySize = exits.Length;
        for (int i = 0; i < exits.Length; i++) exitsProperty.GetArrayElementAtIndex(i).objectReferenceValue = exits[i];
        areaSo.FindProperty("spawnPoints").arraySize = 0;
        areaSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int FindInteractLayer()
    {
        foreach (var name in new[] { "Interact", "Interactable", "Interaction" })
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0) return layer;
        }
        Debug.LogWarning("[Map] No 'Interact' layer found - set the layer of the exits (the one used as interactLayer) by hand.");
        return -1;
    }
}