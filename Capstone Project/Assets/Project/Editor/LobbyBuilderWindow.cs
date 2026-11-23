using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > Lobby > Lobby Builder
/// Builds the lobby from the approved top-down plan (56 x 44 m, spawn at the origin of the plan, +Z is north) into the OPEN scene,
/// on the Terrain you choose: the terrain is flattened under the plan and PAINTED (plaza, curved paths, station yards in the dirt layer,
/// grass everywhere else), then trees, spawn point, level gate, shop spot, gacha chest, three NPCs, two reserved spots,
/// the always-in-scene UI / systems prefabs and a NavMesh limited to the plan.
/// Every prefab slot is optional: an empty slot gets a graybox stand-in (colored primitives).
/// Running it again replaces the "Lobby" object it made before.
/// </summary>
public class LobbyBuilderWindow : EditorWindow
{
    private const string MaterialDir = "Assets/Lobby/Materials";
    private const string RootName = "Lobby";

    private const float PlanCenterZ = 12f;
    private const float PlanHalfWidth = 30f;   // plan 28 m + 2 m
    private const float PlanHalfDepth = 24f;   // plan 22 m + 2 m

    [Header("Terrain")]
    [SerializeField] private Terrain targetTerrain;
    [Tooltip("Put the plan in the middle of the terrain. Off = use 'Spawn Position' below (world X, Z).")]
    [SerializeField] private bool centerOnTerrain = true;
    [SerializeField] private Vector2 spawnPosition = new Vector2(75f, 63f);
    [Tooltip("Level the terrain under the plan (plus a soft edge) so the plaza, paths and stations sit flat.")]
    [SerializeField] private bool flattenTerrain = true;
    [SerializeField, Min(1f)] private float flattenEdge = 10f;
    [Tooltip("Paint the plaza, paths and yards with the dirt layer (the rest of the plan becomes grass).")]
    [SerializeField] private bool paintTerrain = true;
    [SerializeField] private int grassLayerIndex = 0;
    [SerializeField] private int dirtLayerIndex = 1;
    [Tooltip("Paint the whole plan area with grass first, so old painting in it does not show through.")]
    [SerializeField] private bool resetPaintFirst = true;
    [Tooltip("Limit the NavMesh to the plan (plus 2 m), so the player cannot walk out into the scenery.")]
    [SerializeField] private bool limitNavMeshToPlan = true;

    [Header("Scene")]
    [SerializeField] private bool bakeNavMesh = true;
    [SerializeField] private bool addToBuildSettings = true;

    [Header("Your prefabs (all optional)")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject cameraRigPrefab;
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private GameObject shopStallPrefab;
    [SerializeField] private GameObject gachaChestPrefab;
    [SerializeField] private GameObject levelGatePrefab;
    [SerializeField] private GameObject treePrefab;
    [Tooltip("Put in the scene as they are: HUD, inventory, level select, pause menu, settings, shop canvas, minimap system...")]
    [SerializeField] private List<GameObject> systemPrefabs = new List<GameObject>();

    [Header("Data")]
    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private CombatPowerConfig powerConfig;

    private SerializedObject _so;
    private Vector2 _scroll;
    private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
    private int _interactLayer = -1;

    // build state
    private Vector2 _spawnXZ;
    private float _baseY;
    private Vector3 _terrainPos;
    private Vector3 _terrainSize;

    [MenuItem("Tools/Lobby/Lobby Builder")]
    private static void Open()
    {
        var window = GetWindow<LobbyBuilderWindow>("Lobby Builder");
        window.minSize = new Vector2(440, 640);
        window.FillKnownAssets();
    }

    private void OnEnable() => _so = new SerializedObject(this);

    private void OnGUI()
    {
        _so.Update();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.HelpBox("Builds the lobby into the OPEN scene, on your terrain. Empty prefab slots get graybox stand-ins you can replace later.", MessageType.Info);

        EditorGUILayout.PropertyField(_so.FindProperty("targetTerrain"));
        EditorGUILayout.PropertyField(_so.FindProperty("centerOnTerrain"));
        if (!centerOnTerrain) EditorGUILayout.PropertyField(_so.FindProperty("spawnPosition"));
        EditorGUILayout.PropertyField(_so.FindProperty("flattenTerrain"));
        if (flattenTerrain) EditorGUILayout.PropertyField(_so.FindProperty("flattenEdge"));
        EditorGUILayout.PropertyField(_so.FindProperty("paintTerrain"));
        if (paintTerrain)
        {
            DrawLayerPopup("Grass layer", "grassLayerIndex");
            DrawLayerPopup("Dirt layer", "dirtLayerIndex");
            EditorGUILayout.PropertyField(_so.FindProperty("resetPaintFirst"));
        }
        EditorGUILayout.PropertyField(_so.FindProperty("limitNavMeshToPlan"));
        EditorGUILayout.PropertyField(_so.FindProperty("bakeNavMesh"));
        EditorGUILayout.PropertyField(_so.FindProperty("addToBuildSettings"));

        EditorGUILayout.Space();
        foreach (var name in new[] { "playerPrefab", "cameraRigPrefab", "npcPrefab", "shopStallPrefab", "gachaChestPrefab", "levelGatePrefab", "treePrefab" })
            EditorGUILayout.PropertyField(_so.FindProperty(name));
        EditorGUILayout.PropertyField(_so.FindProperty("systemPrefabs"), true);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_so.FindProperty("catalog"));
        EditorGUILayout.PropertyField(_so.FindProperty("powerConfig"));

        EditorGUILayout.EndScrollView();
        _so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Fill known prefabs / data", GUILayout.Height(24))) FillKnownAssets();
        if (GUILayout.Button("Build Lobby Into Open Scene", GUILayout.Height(36))) Build();
    }

    private string[] LayerNames()
    {
        if (targetTerrain == null || targetTerrain.terrainData == null) return Array.Empty<string>();

        var layers = targetTerrain.terrainData.terrainLayers;
        var names = new string[layers.Length];
        for (int i = 0; i < layers.Length; i++) names[i] = $"{i}: {(layers[i] != null ? layers[i].name : "(empty)")}";
        return names;
    }

    private void DrawLayerPopup(string label, string propertyName)
    {
        var property = _so.FindProperty(propertyName);
        string[] names = LayerNames();
        if (names.Length == 0)
        {
            EditorGUILayout.PropertyField(property);
            return;
        }
        property.intValue = EditorGUILayout.Popup(label, Mathf.Clamp(property.intValue, 0, names.Length - 1), names);
    }

    // picks the grass and dirt layer by name (grass, lawn / dirt, soil, mud, path, ground); otherwise layer 0 = grass, layer 1 = dirt
    private void GuessLayers()
    {
        var names = LayerNames();
        if (names.Length < 2) return;

        int grass = -1, dirt = -1;
        for (int i = 0; i < names.Length; i++)
        {
            string lower = names[i].ToLowerInvariant();
            if (grass < 0 && (lower.Contains("grass") || lower.Contains("lawn") || lower.Contains("meadow"))) grass = i;
            if (dirt < 0 && (lower.Contains("dirt") || lower.Contains("soil") || lower.Contains("mud") || lower.Contains("path") || lower.Contains("ground"))) dirt = i;
        }
        if (grass < 0 || dirt < 0 || grass == dirt) { grass = 0; dirt = 1; }
        grassLayerIndex = grass;
        dirtLayerIndex = dirt;
    }

    // The terrain of the open scene, the prefabs and assets the earlier tools created, plus the minimap system.
    private void FillKnownAssets()
    {
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : FindFirstObjectByType<Terrain>();
        GuessLayers();
        if (catalog == null) catalog = FindAsset<LevelCatalog>();
        if (powerConfig == null) powerConfig = FindAsset<CombatPowerConfig>();

        string[] known =
        {
            "Assets/UI/Inventory/Prefabs/InventoryScreen.prefab",
            "Assets/UI/Level/Prefabs/LevelSelectCanvas.prefab",
            "Assets/UI/Shop/Prefabs/ShopCanvas.prefab",
            "Assets/UI/Pause/Prefabs/PauseMenuCanvas.prefab",
            "Assets/UI/Settings/Prefabs/SettingsCanvas.prefab",
        };
        foreach (string path in known)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !systemPrefabs.Contains(prefab)) systemPrefabs.Add(prefab);
        }

        foreach (string guid in AssetDatabase.FindAssets("MinimapSystem t:Prefab"))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null && !systemPrefabs.Contains(prefab)) systemPrefabs.Add(prefab);
            break;
        }

        _so = new SerializedObject(this);
        Repaint();
    }

    private static T FindAsset<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }

    // ------------------------------------------------------------------ plan -> world

    // Plan coordinates (metres from the spawn) -> world position on the terrain, 'up' metres above the ground.
    private Vector3 Pt(float x, float z, float up = 0f)
    {
        float wx = _spawnXZ.x + x;
        float wz = _spawnXZ.y + z;
        float y = flattenTerrain ? _baseY : TerrainHeight(wx, wz);
        return new Vector3(wx, y + up, wz);
    }

    private float TerrainHeight(float worldX, float worldZ) =>
        targetTerrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + _terrainPos.y;

    // ------------------------------------------------------------------ build

    private void Build()
    {
        if (targetTerrain == null)
        {
            EditorUtility.DisplayDialog("Lobby Builder", "Assign the Terrain of the lobby first (the button 'Fill known prefabs / data' finds the one in the open scene).", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!EditorUtility.DisplayDialog("Lobby Builder", $"Build the lobby into the open scene '{scene.name}', on terrain '{targetTerrain.name}'?" +
            (GameObject.Find(RootName) != null ? "\nThe earlier 'Lobby' object will be replaced." : ""), "Build", "Cancel")) return;

        Directory.CreateDirectory(MaterialDir);
        _materials.Clear();
        _interactLayer = FindInteractLayer();

        _terrainPos = targetTerrain.transform.position;
        _terrainSize = targetTerrain.terrainData.size;
        _spawnXZ = centerOnTerrain
            ? new Vector2(_terrainPos.x + _terrainSize.x * 0.5f, _terrainPos.z + _terrainSize.z * 0.5f - PlanCenterZ)
            : spawnPosition;
        _baseY = TerrainHeight(_spawnXZ.x, _spawnXZ.y);

        if (_terrainSize.x < PlanHalfWidth * 2f || _terrainSize.z < PlanHalfDepth * 2f)
            Debug.LogWarning("[Lobby] The terrain is smaller than the 56 x 44 m plan.");

        var existing = GameObject.Find(RootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        if (flattenTerrain) FlattenTerrain();
        if (paintTerrain) PaintTerrain();

        var root = new GameObject(RootName).transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build lobby");
        var environment = Group("Environment", root);
        var stations = Group("Stations", root);
        var npcs = Group("NPCs", root);
        var trees = Group("Trees", root);
        var systems = Group("Systems", root);

        BuildGround(environment);
        BuildTrees(trees);
        BuildSpawnAndCamera(root);
        BuildGate(stations);
        BuildShop(stations);
        BuildGacha(stations);
        BuildNpcs(npcs);
        BuildReserved(stations);
        BuildSystems(systems);

        SaveAndBake(environment.gameObject);

        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[Lobby] Built on terrain '{targetTerrain.name}', spawn at ({_spawnXZ.x:0.#}, {_baseY:0.##}, {_spawnXZ.y:0.#}).");
    }

    private static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // ------------------------------------------------------------------ terrain

    // Sets the heights under the plan to the height at the spawn, with a soft edge around it.
    private void FlattenTerrain()
    {
        var data = targetTerrain.terrainData;
        int res = data.heightmapResolution;
        Undo.RegisterCompleteObjectUndo(data, "Flatten terrain for the lobby");

        float centerX = _spawnXZ.x;
        float centerZ = _spawnXZ.y + PlanCenterZ;
        float reachX = PlanHalfWidth + flattenEdge;
        float reachZ = PlanHalfDepth + flattenEdge;

        int x0 = Mathf.Clamp(Mathf.FloorToInt((centerX - reachX - _terrainPos.x) / _terrainSize.x * (res - 1)), 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((centerX + reachX - _terrainPos.x) / _terrainSize.x * (res - 1)), 0, res - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt((centerZ - reachZ - _terrainPos.z) / _terrainSize.z * (res - 1)), 0, res - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt((centerZ + reachZ - _terrainPos.z) / _terrainSize.z * (res - 1)), 0, res - 1);

        int width = x1 - x0 + 1;
        int depth = z1 - z0 + 1;
        float[,] heights = data.GetHeights(x0, z0, width, depth); // [z, x]
        float target = Mathf.Clamp01((_baseY - _terrainPos.y) / _terrainSize.y);

        for (int iz = 0; iz < depth; iz++)
        {
            for (int ix = 0; ix < width; ix++)
            {
                float wx = _terrainPos.x + (x0 + ix) / (float)(res - 1) * _terrainSize.x;
                float wz = _terrainPos.z + (z0 + iz) / (float)(res - 1) * _terrainSize.z;

                // distance outside the flat rectangle
                float dx = Mathf.Max(0f, Mathf.Abs(wx - centerX) - PlanHalfWidth);
                float dz = Mathf.Max(0f, Mathf.Abs(wz - centerZ) - PlanHalfDepth);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / flattenEdge));
                heights[iz, ix] = Mathf.Lerp(heights[iz, ix], target, t);
            }
        }

        data.SetHeights(x0, z0, heights);
        EditorUtility.SetDirty(data);
    }

    private struct PathDef
    {
        public readonly Vector2 a;
        public readonly Vector2 c;
        public readonly Vector2 b;
        public readonly float width;
        public PathDef(float ax, float az, float cx, float cz, float bx, float bz, float width)
        {
            a = new Vector2(ax, az); c = new Vector2(cx, cz); b = new Vector2(bx, bz); this.width = width;
        }
    }

    private static readonly PathDef[] Paths =
    {
        new PathDef(0, 0, 0, 6, 0, 12, 4),           // spawn -> plaza
        new PathDef(0, 12, 0, 20, 0, 28, 4),         // plaza -> gate (straight)
        new PathDef(-9, 12, -13, 15, -16, 10, 3),    // shop
        new PathDef(9, 12, 13, 15, 16, 10, 3),       // gacha chest
        new PathDef(-9, 21, -8, 17, -3, 15.5f, 3),   // NPC 1
        new PathDef(10, 22, 8, 17.5f, 3, 15.5f, 3),  // NPC 2
        new PathDef(-18, -3, -10, -4, -3, 2.5f, 3),  // NPC 3
    };

    private static readonly Vector3[] Discs =        // x, z, radius
    {
        new Vector3(0, 12, 9),        // plaza
        new Vector3(-16, 10, 3.5f),   // shop yard
        new Vector3(16, 10, 4.5f),    // gacha yard
        new Vector3(-9, 21, 1.8f), new Vector3(10, 22, 1.8f), new Vector3(-18, -3, 1.8f), // NPC patches
    };

    private const float PaintSoft = 1.5f;     // metres of soft edge
    private const float PaintJitter = 1.6f;   // metres the edge wanders (noise)

    private void PaintTerrain()
    {
        var data = targetTerrain.terrainData;
        int layerCount = data.alphamapLayers;
        if (grassLayerIndex < 0 || dirtLayerIndex < 0 || grassLayerIndex >= layerCount || dirtLayerIndex >= layerCount || grassLayerIndex == dirtLayerIndex)
        {
            Debug.LogWarning($"[Lobby] Painting skipped: pick two different terrain layers (the terrain has {layerCount}).");
            return;
        }

        Undo.RegisterCompleteObjectUndo(data, "Paint the lobby");

        // curves -> polylines
        var lines = new List<Vector2[]>();
        foreach (var path in Paths)
        {
            var points = new Vector2[25];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (float)(points.Length - 1);
                points[i] = (1 - t) * (1 - t) * path.a + 2 * (1 - t) * t * path.c + t * t * path.b;
            }
            lines.Add(points);
        }

        int res = data.alphamapResolution;
        float centerX = _spawnXZ.x;
        float centerZ = _spawnXZ.y + PlanCenterZ;
        float reachX = PlanHalfWidth + 6f;
        float reachZ = PlanHalfDepth + 6f;
        int x0 = Mathf.Clamp(Mathf.FloorToInt((centerX - reachX - _terrainPos.x) / _terrainSize.x * res), 0, res - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt((centerX + reachX - _terrainPos.x) / _terrainSize.x * res), 1, res);
        int z0 = Mathf.Clamp(Mathf.FloorToInt((centerZ - reachZ - _terrainPos.z) / _terrainSize.z * res), 0, res - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt((centerZ + reachZ - _terrainPos.z) / _terrainSize.z * res), 1, res);
        int width = x1 - x0;
        int depth = z1 - z0;

        float[,,] maps = data.GetAlphamaps(x0, z0, width, depth); // [z, x, layer]
        for (int iz = 0; iz < depth; iz++)
        {
            for (int ix = 0; ix < width; ix++)
            {
                float wx = _terrainPos.x + (x0 + ix + 0.5f) / res * _terrainSize.x;
                float wz = _terrainPos.z + (z0 + iz + 0.5f) / res * _terrainSize.z;
                float u = wx - _spawnXZ.x;   // plan coordinates
                float v = wz - _spawnXZ.y;

                if (resetPaintFirst)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(u) - PlanHalfWidth);
                    float dz = Mathf.Max(0f, Mathf.Abs(v - PlanCenterZ) - PlanHalfDepth);
                    float grass = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz) / 3f));
                    Blend(maps, iz, ix, layerCount, grassLayerIndex, grass);
                }

                float dirt = DirtCoverage(u, v, lines, Mathf.PerlinNoise(wx * 0.4f, wz * 0.4f));
                if (dirt > 0f) Blend(maps, iz, ix, layerCount, dirtLayerIndex, dirt);
            }
        }

        data.SetAlphamaps(x0, z0, maps);
        EditorUtility.SetDirty(data);
    }

    // 0..1: how much dirt there is at a plan position (union of the plaza, the yards and the paths, with a wandering soft edge)
    private static float DirtCoverage(float u, float v, List<Vector2[]> lines, float noise)
    {
        float jitter = (noise - 0.5f) * PaintJitter;
        float best = float.MinValue; // largest "metres inside a shape"

        foreach (var disc in Discs)
            best = Mathf.Max(best, disc.z - Mathf.Sqrt((u - disc.x) * (u - disc.x) + (v - disc.y) * (v - disc.y)));

        // yard in front of the gate: 10 x 6 m box centred at (0, 27)
        float qx = Mathf.Abs(u) - 5f;
        float qz = Mathf.Abs(v - 27f) - 3f;
        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qz, 0f) * Mathf.Max(qz, 0f));
        best = Mathf.Max(best, -(outside + Mathf.Min(Mathf.Max(qx, qz), 0f)));

        for (int p = 0; p < lines.Count; p++)
        {
            var points = lines[p];
            float minDistance = float.MaxValue;
            for (int i = 0; i + 1 < points.Length; i++)
                minDistance = Mathf.Min(minDistance, DistanceToSegment(u, v, points[i], points[i + 1]));
            best = Mathf.Max(best, Paths[p].width * 0.5f - minDistance);
        }

        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((best + jitter) / PaintSoft + 0.5f));
    }

    private static float DistanceToSegment(float px, float pz, Vector2 a, Vector2 b)
    {
        float abx = b.x - a.x, abz = b.y - a.y;
        float length2 = abx * abx + abz * abz;
        float t = length2 > 0f ? Mathf.Clamp01(((px - a.x) * abx + (pz - a.y) * abz) / length2) : 0f;
        float dx = px - (a.x + abx * t), dz = pz - (a.y + abz * t);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // moves the weights of one pixel toward "all on this layer" by 'amount' (weights keep summing to 1)
    private static void Blend(float[,,] maps, int iz, int ix, int layerCount, int layer, float amount)
    {
        for (int l = 0; l < layerCount; l++)
            maps[iz, ix, l] = Mathf.Lerp(maps[iz, ix, l], l == layer ? 1f : 0f, amount);
    }

    // ------------------------------------------------------------------ environment

    // plaza, paths and yards are painted on the terrain (PaintTerrain); only the fountain is an object
    private void BuildGround(Transform parent)
    {
        var fountain = Primitive("Fountain", PrimitiveType.Cylinder, parent, Pt(0f, 12f, 0.35f), new Vector3(3f, 0.35f, 3f), "Stone", new Color(0.55f, 0.58f, 0.6f));
        GameObjectUtility.SetNavMeshArea(fountain, 1);
    }

    // trees close the edge (a ring) and thicken the left and right sides; the road to the gate and every station stay clear
    private void BuildTrees(Transform parent)
    {
        var rng = new System.Random(11);
        var points = new List<Vector2>();
        for (float x = -27f; x <= 27f; x += 2.6f)
        {
            points.Add(new Vector2(x + Jitter(rng, 0.6f), -9f + Jitter(rng, 0.6f)));
            points.Add(new Vector2(x + Jitter(rng, 0.6f), 33f + Jitter(rng, 0.6f)));
        }
        for (float z = -7f; z <= 31f; z += 2.6f)
        {
            points.Add(new Vector2(-27f + Jitter(rng, 0.6f), z + Jitter(rng, 0.6f)));
            points.Add(new Vector2(27f + Jitter(rng, 0.6f), z + Jitter(rng, 0.6f)));
        }
        for (int i = 0; i < 34; i++)
        {
            float x = rng.Next(2) == 0 ? Range(rng, -26f, -22f) : Range(rng, 22f, 26f);
            points.Add(new Vector2(x, Range(rng, -8f, 32f)));
        }

        var keepClear = new[]
        {
            new Vector2(0, 0), new Vector2(0, 12), new Vector2(0, 30), new Vector2(-16, 10), new Vector2(16, 10),
            new Vector2(-9, 21), new Vector2(10, 22), new Vector2(-18, -3), new Vector2(-21, 22), new Vector2(21, 21),
        };

        foreach (var point in points)
        {
            bool blocked = false;
            foreach (var spot in keepClear)
            {
                if (Vector2.Distance(point, spot) < 6.5f) { blocked = true; break; }
            }
            if (blocked || (Mathf.Abs(point.x) < 3.5f && point.y is > -9f and < 33f && Mathf.Abs(point.y - 30f) > 2f)) continue;

            MakeTree(parent, point, 0.9f + (float)rng.NextDouble() * 0.6f);
        }
    }

    private void MakeTree(Transform parent, Vector2 plan, float size)
    {
        Vector3 ground = Pt(plan.x, plan.y);
        if (treePrefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parent);
            instance.transform.position = ground;
            instance.transform.localScale *= size;
            GameObjectUtility.SetNavMeshArea(instance, 1);
            return;
        }

        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = ground;
        var trunk = Primitive("Trunk", PrimitiveType.Cylinder, tree.transform, ground + Vector3.up * size, new Vector3(0.4f, 1.0f, 0.4f) * size, "Trunk", new Color(0.35f, 0.24f, 0.14f));
        var crown = Primitive("Crown", PrimitiveType.Sphere, tree.transform, ground + Vector3.up * 3f * size, Vector3.one * 2.8f * size, "Leaves", new Color(0.12f, 0.32f, 0.19f));
        GameObjectUtility.SetNavMeshArea(trunk, 1);
        GameObjectUtility.SetNavMeshArea(crown, 1);
    }

    private static float Jitter(System.Random rng, float amount) => ((float)rng.NextDouble() * 2f - 1f) * amount;
    private static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

    // ------------------------------------------------------------------ player, camera

    private void BuildSpawnAndCamera(Transform root)
    {
        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(root, false);
        spawn.transform.SetPositionAndRotation(Pt(0f, 0f), Quaternion.identity); // faces the gate (+Z)
        Mark(spawn, "sv_label_3");

        // a player already in the scene is moved to the spawn; otherwise the prefab is placed
        var existingPlayer = FindFirstObjectByType<PlayerController>();
        if (existingPlayer != null)
        {
            existingPlayer.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }
        else if (playerPrefab != null)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }
        else
        {
            Debug.LogWarning("[Lobby] No player in the scene and no player prefab: add yours at PlayerSpawn.");
        }

        if (cameraRigPrefab != null)
        {
            if (!AlreadyInScene(cameraRigPrefab))
            {
                PrefabUtility.InstantiatePrefab(cameraRigPrefab);
                var defaultCamera = Camera.main;
                if (defaultCamera != null && cameraRigPrefab.GetComponentInChildren<Camera>(true) != null) Object.DestroyImmediate(defaultCamera.gameObject);
            }
        }
        else if (Camera.main != null && FindFirstObjectByType<PlayerController>() == null)
        {
            // stand-in isometric view like the levels: orthographic 10, offset (0, 15, -15) from the spawn
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.transform.SetPositionAndRotation(Pt(0f, -15f, 15f), Quaternion.Euler(45f, 0f, 0f));
        }
    }

    // ------------------------------------------------------------------ stations

    private void BuildGate(Transform parent)
    {
        GameObject gate;
        if (levelGatePrefab != null)
        {
            gate = (GameObject)PrefabUtility.InstantiatePrefab(levelGatePrefab, parent);
            gate.transform.SetPositionAndRotation(Pt(0f, 30f), Quaternion.Euler(0f, 180f, 0f)); // arch faces south
        }
        else
        {
            gate = new GameObject("LevelGate_Arch");
            gate.transform.SetParent(parent, false);
            gate.transform.position = Pt(0f, 30f);

            var gold = new Color(0.91f, 0.76f, 0.29f);
            var left = Primitive("PillarL", PrimitiveType.Cube, gate.transform, Pt(-4.5f, 30f, 2.5f), new Vector3(1f, 5f, 1f), "Gold", gold);
            var right = Primitive("PillarR", PrimitiveType.Cube, gate.transform, Pt(4.5f, 30f, 2.5f), new Vector3(1f, 5f, 1f), "Gold", gold);
            var top = Primitive("Lintel", PrimitiveType.Cube, gate.transform, Pt(0f, 30f, 5.5f), new Vector3(10f, 1f, 1f), "Gold", gold);
            foreach (var part in new[] { left, right, top }) GameObjectUtility.SetNavMeshArea(part, 1);

            var interact = Primitive("GateInteract", PrimitiveType.Cube, gate.transform, Pt(0f, 30f, 1.5f), new Vector3(4f, 3f, 0.6f), "GateGlow", new Color(0.3f, 0.7f, 0.9f), keepCollider: true);
            if (_interactLayer >= 0) interact.layer = _interactLayer;
            AddLabel(interact.transform, "LEVEL GATE", 3.2f);
            interact.AddComponent<LevelGate>();
        }

        var levelGate = gate.GetComponentInChildren<LevelGate>(true);
        if (levelGate == null)
        {
            Debug.LogWarning("[Lobby] The gate prefab has no LevelGate component.");
            return;
        }
        var so = new SerializedObject(levelGate);
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.FindProperty("powerConfig").objectReferenceValue = powerConfig;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (catalog == null) Debug.LogWarning("[Lobby] No LevelCatalog assigned: the gate will not open until you assign one.");
    }

    // The shop needs the camera rig and ShopStall wiring (Tools > Shop > Create Shop Rig): this places the spot, you add the stall.
    private void BuildShop(Transform parent)
    {
        Vector3 position = Pt(-16f, 10f);
        var rotation = Quaternion.Euler(0f, 90f, 0f); // faces the plaza

        if (shopStallPrefab != null)
        {
            var stall = (GameObject)PrefabUtility.InstantiatePrefab(shopStallPrefab, parent);
            stall.transform.SetPositionAndRotation(position, rotation);
            return;
        }

        var spot = new GameObject("ShopSpot_PLACEHOLDER");
        spot.transform.SetParent(parent, false);
        spot.transform.SetPositionAndRotation(position, rotation);
        var counter = Primitive("Counter", PrimitiveType.Cube, spot.transform, position + Vector3.up * 1.1f, new Vector3(3f, 2.2f, 5f), "Teal", new Color(0.25f, 0.82f, 0.75f), keepCollider: true);
        counter.transform.rotation = rotation;
        GameObjectUtility.SetNavMeshArea(counter, 1);
        AddLabel(counter.transform, "SHOP: put the merchant + ShopStall here", 3f);
    }

    private void BuildGacha(Transform parent)
    {
        Vector3 position = Pt(16f, 10f);
        if (gachaChestPrefab != null)
        {
            var chest = (GameObject)PrefabUtility.InstantiatePrefab(gachaChestPrefab, parent);
            chest.transform.position = position;
            return;
        }

        var spot = new GameObject("GachaChest_PLACEHOLDER");
        spot.transform.SetParent(parent, false);
        spot.transform.position = position;
        Primitive("Platform", PrimitiveType.Cylinder, spot.transform, position + Vector3.up * 0.1f, new Vector3(6f, 0.1f, 6f), "Violet", new Color(0.6f, 0.44f, 0.88f));
        var box = Primitive("Chest", PrimitiveType.Cube, spot.transform, position + Vector3.up * 0.9f, new Vector3(2f, 1.4f, 1.4f), "Gold", new Color(0.91f, 0.76f, 0.29f), keepCollider: true);
        GameObjectUtility.SetNavMeshArea(box, 1);
        if (_interactLayer >= 0) box.layer = _interactLayer;
        AddLabel(box.transform, "GACHA: add your gacha chest script here", 1.6f);
    }

    private void BuildNpcs(Transform parent)
    {
        var spots = new[] { new Vector2(-9f, 21f), new Vector2(10f, 22f), new Vector2(-18f, -3f) };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 position = Pt(spots[i].x, spots[i].y);
            var look = Quaternion.LookRotation(Pt(0f, 12f) - position, Vector3.up); // toward the plaza
            look = Quaternion.Euler(0f, look.eulerAngles.y, 0f);

            if (npcPrefab != null)
            {
                var npc = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab, parent);
                npc.name = "NPC_" + (i + 1);
                npc.transform.SetPositionAndRotation(position, look);
                continue;
            }

            var body = Primitive("NPC_" + (i + 1), PrimitiveType.Capsule, parent, position + Vector3.up, Vector3.one, "NpcBlue", new Color(0.35f, 0.72f, 0.94f), keepCollider: true);
            body.transform.rotation = look;
            if (_interactLayer >= 0) body.layer = _interactLayer;
            body.AddComponent<NPC>();
            AddLabel(body.transform, "NPC " + (i + 1), 1.6f);
        }
    }

    private void BuildReserved(Transform parent)
    {
        var quest = new GameObject("Reserved_QuestBoard");
        quest.transform.SetParent(parent, false);
        quest.transform.position = Pt(-21f, 22f);
        Mark(quest, "sv_label_5");

        var smith = new GameObject("Reserved_Blacksmith");
        smith.transform.SetParent(parent, false);
        smith.transform.position = Pt(21f, 21f);
        Mark(smith, "sv_label_5");
    }

    // ------------------------------------------------------------------ systems, navmesh, build settings

    private void BuildSystems(Transform parent)
    {
        foreach (var prefab in systemPrefabs)
        {
            if (prefab == null || AlreadyInScene(prefab)) continue;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = prefab.name;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            eventSystem.transform.SetParent(parent, false);
        }

        if (Object.FindFirstObjectByType<AudioListener>() == null && Camera.main != null) Camera.main.gameObject.AddComponent<AudioListener>();
    }

    // the same prefab placed by an earlier run (or by you) is not placed twice
    private static bool AlreadyInScene(GameObject prefab)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab) return true;
        }
        return false;
    }

    // The NavMesh needs a saved scene (its data is stored next to it).
    private void SaveAndBake(GameObject environment)
    {
        var scene = SceneManager.GetActiveScene();
        string path = scene.path;
        if (string.IsNullOrEmpty(path))
        {
            Directory.CreateDirectory("Assets/Scenes");
            path = EditorUtility.SaveFilePanelInProject("Save the lobby scene", "Lobby", "unity", "The NavMesh data is stored next to the scene.", "Assets/Scenes");
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[Lobby] The scene was not saved, so the NavMesh was not baked. Save it, then add a NavMesh Surface and bake.");
                return;
            }
        }
        EditorSceneManager.SaveScene(scene, path);

        if (addToBuildSettings) AddToBuildSettings(path);
        if (bakeNavMesh) BakeNavMesh(environment);
    }

    private static void AddToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var existing in scenes)
        {
            if (existing.path == path) return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[Lobby] Added {path} to the Build Settings. Its name must match PauseMenuController's lobby scene name.");
    }

    private void BakeNavMesh(GameObject host)
    {
        var surfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType == null)
        {
            Debug.LogWarning("[Lobby] AI Navigation package not found: add a NavMesh Surface and press Bake.");
            return;
        }

        Object surface = Object.FindFirstObjectByType(surfaceType);
        if (surface == null) surface = host.AddComponent(surfaceType);
        if (limitNavMeshToPlan) LimitSurfaceToPlan(surface);

        var managerType = System.Type.GetType("UnityEditor.AI.NavMeshAssetManager, Unity.AI.Navigation.Editor");
        object manager = managerType?.GetProperty("instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        var bake = managerType?.GetMethod("StartBakingSurfaces", new[] { typeof(Object[]) });

        if (manager == null || bake == null)
        {
            Debug.LogWarning("[Lobby] NavMesh Surface ready - press Bake on it, then save the scene.");
            return;
        }
        bake.Invoke(manager, new object[] { new Object[] { surface } });
        Debug.Log("[Lobby] NavMesh baking started - press Ctrl+S when it finishes.");
    }

    // NavMesh Surface -> Collect Objects: Volume, a box around the plan (+2 m), so only the lobby is walkable.
    private void LimitSurfaceToPlan(Object surface)
    {
        var so = new SerializedObject(surface);
        var collect = so.FindProperty("m_CollectObjects");
        var size = so.FindProperty("m_Size");
        var center = so.FindProperty("m_Center");
        if (collect == null || size == null || center == null)
        {
            Debug.LogWarning("[Lobby] Could not limit the NavMesh Surface to the plan: set Collect Objects = Volume on it yourself.");
            return;
        }

        var component = (Component)surface;
        Vector3 worldCenter = new Vector3(_spawnXZ.x, _baseY + 10f, _spawnXZ.y + PlanCenterZ);
        collect.enumValueIndex = 1; // Volume
        size.vector3Value = new Vector3(PlanHalfWidth * 2f, 40f, PlanHalfDepth * 2f);
        center.vector3Value = component.transform.InverseTransformPoint(worldCenter);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ------------------------------------------------------------------ helpers

    private GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, string materialName, Color color, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(materialName, color);
        if (!keepCollider && go.TryGetComponent(out Collider collider)) Object.DestroyImmediate(collider);
        return go;
    }

    private Material Mat(string name, Color color)
    {
        if (_materials.TryGetValue(name, out var cached)) return cached;

        string path = $"{MaterialDir}/Lobby_{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
        }
        _materials[name] = material;
        return material;
    }

    private static void AddLabel(Transform parent, string text, float height)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.position = parent.position + Vector3.up * height;
        var label = go.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void Mark(GameObject go, string iconName)
    {
        var icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;
        if (icon != null) EditorGUIUtility.SetIconForObject(go, icon);
    }

    private static int FindInteractLayer()
    {
        foreach (var name in new[] { "Interact", "Interactable", "Interaction" })
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0) return layer;
        }
        Debug.LogWarning("[Lobby] No 'Interact' layer found - set the layer of the gate, chest and NPCs by hand.");
        return -1;
    }
}