using System.IO;
using UnityEditor;
using UnityEngine;

public static class LootPrefabBuilder
{
    private const string Dir = "Assets/Prefabs/Loot";

    [MenuItem("Tools/Loot/Create Pickup Prefab")]
    public static void CreatePickupPrefab()
    {
        Directory.CreateDirectory(Dir);
        AssetDatabase.Refresh();

        var root = new GameObject("WorldPickup", typeof(WorldPickup), typeof(SphereCollider));
        foreach (var layerName in new[] { "Interact", "Interactable", "Interaction" })
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) continue;
            root.layer = layer;
            break;
        }

        var collider = root.GetComponent<SphereCollider>();
        collider.radius = 0.45f;
        collider.isTrigger = false; // PlayerController's click raycast ignores triggers

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = Vector3.one * 0.35f;
        var icon = visual.AddComponent<SpriteRenderer>();
        icon.sortingOrder = 1;

        var glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(visual.transform, false);
        glowObject.transform.localScale = Vector3.one * 2.4f;
        var glow = glowObject.AddComponent<SpriteRenderer>();
        glow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        glow.sortingOrder = 0;

        var so = new SerializedObject(root.GetComponent<WorldPickup>());
        so.FindProperty("visual").objectReferenceValue = visual.transform;
        so.FindProperty("iconRenderer").objectReferenceValue = icon;
        so.FindProperty("glowRenderer").objectReferenceValue = glow;
        so.FindProperty("interactRange").floatValue = 1.5f; // click-to-collect range (Interactable)
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = Dir + "/WorldPickup.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log($"[Loot] Pickup prefab created at {path}. Put it on the 'Interact' layer if no layer was found automatically.");
    }
}