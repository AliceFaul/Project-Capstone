#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class AreaBuilderWindow : EditorWindow
{
    private enum Placement { AutoFreeSlot, TerrainCenter, Manual }

    // ===== Settings =====
    private string areaId = "Area_Combat_01";
    private NodeType areaType = NodeType.Combat;
    private int batchCount = 1;
    private float areaRadius = 14f;

    private int enemySpawnCount = 4;
    private int chestSpawnCount;
    private int propSpawnCount;
    private int exitCount = 2;
    private AreaSpawnPoint.SpawnTiming chestTiming = AreaSpawnPoint.SpawnTiming.OnEnter;

    private GameObject[] enemyPrefabs;
    private GameObject[] chestPrefabs;
    private GameObject[] propPrefabs;
    private GameObject exitPrefab;

    private Terrain targetTerrain;
    private bool createTerrainIfMissing;
    private Vector2 newTerrainSize = new Vector2(200f, 200f);
    private Placement placement = Placement.AutoFreeSlot;
    private Vector3 manualPosition;
    private float areaGap = 6f;
    private bool parentUnderAreasObject = true;

    private bool randomizePositions = true;
    private bool entryAtEdge = true;
    private float minDistanceBetweenPoints = 2.8f;
    private float heightOffset = 0.05f;
    private float maxSlope = 25f;
    private bool snapToNavMesh = true;

    private int boundarySegments = 24;
    private float boundaryThickness = 2f;

    private Vector2 scroll;
    private bool _navMeshBaked;

    [MenuItem("Tools/Map/Area Builder")]
    public static void Open()
    {
        var window = GetWindow<AreaBuilderWindow>("Area Builder");
        window.minSize = new Vector2(420, 640);
        window.Show();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Area Builder (Terrain)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        areaId = EditorGUILayout.TextField("Area Id", areaId);

        EditorGUI.BeginChangeCheck();
        areaType = (NodeType)EditorGUILayout.EnumPopup("Node Type", areaType);
        if (EditorGUI.EndChangeCheck()) ApplyPreset(areaType);

        batchCount = EditorGUILayout.IntSlider("Areas To Create", batchCount, 1, 12);
        areaRadius = EditorGUILayout.Slider("Area Radius", areaRadius, 8f, 40f);

        EditorGUILayout.Space(8);
        GUILayout.Label("Terrain & Placement", EditorStyles.boldLabel);
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        createTerrainIfMissing = EditorGUILayout.Toggle("Create Terrain If Missing", createTerrainIfMissing);
        if (createTerrainIfMissing) newTerrainSize = EditorGUILayout.Vector2Field("  New Terrain Size (X, Z)", newTerrainSize);
        placement = (Placement)EditorGUILayout.EnumPopup("Placement", placement);
        if (placement == Placement.Manual) manualPosition = EditorGUILayout.Vector3Field("  Position", manualPosition);
        if (placement == Placement.AutoFreeSlot) areaGap = EditorGUILayout.Slider("  Gap Between Areas", areaGap, 0f, 30f);
        parentUnderAreasObject = EditorGUILayout.Toggle("Group Under 'Areas'", parentUnderAreasObject);
        heightOffset = EditorGUILayout.Slider("Height Offset", heightOffset, 0f, 0.5f);

        EditorGUILayout.Space(8);
        GUILayout.Label("Spawn Counts", EditorStyles.boldLabel);
        enemySpawnCount = EditorGUILayout.IntSlider("Enemy Spawns", enemySpawnCount, 0, 12);
        chestSpawnCount = EditorGUILayout.IntSlider("Chest Spawns", chestSpawnCount, 0, 4);
        if (chestSpawnCount > 0) chestTiming = (AreaSpawnPoint.SpawnTiming)EditorGUILayout.EnumPopup("  Chest Timing", chestTiming);
        propSpawnCount = EditorGUILayout.IntSlider("Prop Spawns", propSpawnCount, 0, 8);
        exitCount = EditorGUILayout.IntSlider("Exits", exitCount, 0, 4);

        EditorGUILayout.Space(8);
        GUILayout.Label("Prefabs (optional)", EditorStyles.boldLabel);
        DrawPrefabList("Enemy Prefabs", ref enemyPrefabs);
        DrawPrefabList("Chest Prefabs", ref chestPrefabs);
        DrawPrefabList("Prop Prefabs", ref propPrefabs);
        exitPrefab = (GameObject)EditorGUILayout.ObjectField("Exit Prefab", exitPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(8);
        GUILayout.Label("Layout", EditorStyles.boldLabel);
        randomizePositions = EditorGUILayout.Toggle("Randomize Positions", randomizePositions);
        entryAtEdge = EditorGUILayout.Toggle("Entry At Edge, Exits Opposite", entryAtEdge);
        minDistanceBetweenPoints = EditorGUILayout.Slider("Min Distance", minDistanceBetweenPoints, 1.5f, 8f);
        maxSlope = EditorGUILayout.Slider("Max Slope (deg)", maxSlope, 5f, 60f);
        snapToNavMesh = EditorGUILayout.Toggle("Snap To NavMesh (if baked)", snapToNavMesh);

        EditorGUILayout.Space(8);
        GUILayout.Label("Boundary (keeps the player inside the area)", EditorStyles.boldLabel);
        boundarySegments = EditorGUILayout.IntSlider("Wall Segments (0 = none)", boundarySegments, 0, 48);
        if (boundarySegments > 0) boundaryThickness = EditorGUILayout.Slider("  Thickness", boundaryThickness, 0.5f, 6f);

        EditorGUILayout.Space(12);
        if (GUILayout.Button(batchCount > 1 ? $"Create {batchCount} Areas On Terrain" : "Create Area On Terrain", GUILayout.Height(36)))
            CreateAreas();

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Tool đặt Entry / Spawn / Exit theo chiều cao Terrain, tránh dốc và (nếu đã bake) chỗ ngoài NavMesh.\n" +
            "Mỗi Area ở một ô trống riêng trên Terrain (Auto Free Slot). Boundary là các NavMeshObstacle (Carve) bật/tắt cùng Area, " +
            "cần bake NavMesh cho Terrain trước.\n" +
            "Số Exit nên bằng số nhánh đi tiếp của node (mặc định tối đa 2). Node Boss không cần Exit.",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabList(string label, ref GameObject[] prefabs)
    {
        int size = prefabs?.Length ?? 0;
        size = EditorGUILayout.IntField(label + " Count", Mathf.Max(0, size));
        if (prefabs == null || prefabs.Length != size) System.Array.Resize(ref prefabs, size);

        for (int i = 0; i < size; i++)
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField($"  [{i}]", prefabs[i], typeof(GameObject), false);
    }

    // Sensible counts per node type (you can still change them afterward).
    private void ApplyPreset(NodeType type)
    {
        chestTiming = AreaSpawnPoint.SpawnTiming.OnEnter;
        switch (type)
        {
            case NodeType.Combat: enemySpawnCount = 4; chestSpawnCount = 0; propSpawnCount = 0; exitCount = 2; break;
            case NodeType.Reward: enemySpawnCount = 0; chestSpawnCount = 2; propSpawnCount = 0; exitCount = 2; break;
            case NodeType.Event: enemySpawnCount = 0; chestSpawnCount = 0; propSpawnCount = 1; exitCount = 2; break;
            case NodeType.Rest: enemySpawnCount = 0; chestSpawnCount = 0; propSpawnCount = 1; exitCount = 2; break;
            case NodeType.Shop: enemySpawnCount = 0; chestSpawnCount = 0; propSpawnCount = 1; exitCount = 2; break;
            case NodeType.Elite:
                enemySpawnCount = 5; chestSpawnCount = 1; propSpawnCount = 0; exitCount = 2;
                chestTiming = AreaSpawnPoint.SpawnTiming.OnClear;
                break;
            case NodeType.Boss:
                enemySpawnCount = 1; chestSpawnCount = 1; propSpawnCount = 0; exitCount = 0;
                chestTiming = AreaSpawnPoint.SpawnTiming.OnClear;
                break;
        }

        // Area_Combat_01 -> Area_Elite_01
        if (Regex.IsMatch(areaId, @"^Area_[A-Za-z]+_")) areaId = Regex.Replace(areaId, @"^Area_[A-Za-z]+_", $"Area_{type}_");
    }

    // ================================================================== create

    private void CreateAreas()
    {
        Terrain terrain = ResolveTerrain();
        if (terrain == null) return;

        _navMeshBaked = NavMesh.CalculateTriangulation().vertices.Length > 0;
        Transform container = parentUnderAreasObject ? GetOrCreateAreasContainer() : null;

        int created = 0;
        for (int n = 0; n < batchCount; n++)
        {
            string id = UniqueId(areaId);
            if (!TryFindCenter(terrain, out Vector3 center))
            {
                EditorUtility.DisplayDialog("Area Builder",
                    $"Không còn chỗ trống trên Terrain cho Area bán kính {areaRadius}.\nGiảm radius/gap hoặc dùng Terrain lớn hơn.", "OK");
                break;
            }

            CreateArea(terrain, id, center, container);
            areaId = NextId(id);
            created++;
        }

        if (created > 0) Debug.Log($"[AreaBuilder] Created {created} area(s) on Terrain '{terrain.name}'.");
    }

    private Terrain ResolveTerrain()
    {
        Terrain terrain = targetTerrain != null ? targetTerrain : Terrain.activeTerrain;

        if (terrain == null && createTerrainIfMissing)
        {
            terrain = CreateSimpleTerrain();
            targetTerrain = terrain;
        }

        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Không tìm thấy Terrain.\nHãy kéo Terrain vào ô Target Terrain hoặc bật Create Terrain If Missing.", "OK");
        }
        return terrain;
    }

    private void CreateArea(Terrain terrain, string id, Vector3 center, Transform container)
    {
        var root = new GameObject(id);
        Undo.RegisterCreatedObjectUndo(root, "Create Area");

        center.y = terrain.SampleHeight(center) + terrain.transform.position.y;
        root.transform.position = center;
        if (container != null) root.transform.SetParent(container, true);

        var area = root.AddComponent<AreaController>();
        var used = new List<Vector2>(); // XZ in the area's local space
        int misplaced = 0;

        // ===== EntryPoint =====
        float entryAngle = Random.Range(0f, 360f);
        var entry = NewChild("EntryPoint", root.transform);
        Vector3 entryLocal = entryAtEdge
            ? Polar(entryAngle, areaRadius * 0.78f)
            : (randomizePositions ? RandomInDisc(areaRadius * 0.3f) : Vector3.zero);
        used.Add(Place(entry.transform, root.transform, entryLocal, terrain, true));

        var spawnsFolder = NewChild("Spawns", root.transform);
        var exitsFolder = NewChild("Exits", root.transform);

        // ===== Spawn points =====
        for (int i = 0; i < enemySpawnCount; i++)
            misplaced += CreateSpawnPoint(spawnsFolder.transform, $"Spawn_Enemy_{i + 1}", AreaSpawnPoint.Kind.Enemy, enemyPrefabs,
                AreaSpawnPoint.SpawnTiming.OnEnter, used, terrain, root.transform, entry.transform);

        for (int i = 0; i < chestSpawnCount; i++)
            misplaced += CreateSpawnPoint(spawnsFolder.transform, $"Spawn_Chest_{i + 1}", AreaSpawnPoint.Kind.Chest, chestPrefabs,
                chestTiming, used, terrain, root.transform, entry.transform);

        for (int i = 0; i < propSpawnCount; i++)
            misplaced += CreateSpawnPoint(spawnsFolder.transform, $"Spawn_Prop_{i + 1}", AreaSpawnPoint.Kind.Prop, propPrefabs,
                AreaSpawnPoint.SpawnTiming.OnEnter, used, terrain, root.transform, entry.transform);

        // ===== Exits =====
        int interactLayer = FindInteractLayer();
        for (int i = 0; i < exitCount; i++)
        {
            Vector3 localPos;
            if (entryAtEdge)
            {
                float spread = Mathf.Min(70f, 140f / Mathf.Max(1, exitCount));
                float angle = entryAngle + 180f + spread * (i - (exitCount - 1) * 0.5f);
                localPos = Polar(angle, areaRadius * Random.Range(0.8f, 0.9f));
            }
            else if (randomizePositions)
            {
                float angle = (360f / Mathf.Max(1, exitCount)) * i + Random.Range(-20f, 20f);
                localPos = Polar(angle, areaRadius * Random.Range(0.78f, 0.95f));
            }
            else
            {
                localPos = new Vector3((i - (exitCount - 1) * 0.5f) * 5f, 0f, areaRadius * 0.85f);
            }

            GameObject exitGo = exitPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(exitPrefab, exitsFolder.transform)
                : new GameObject();
            exitGo.name = $"Exit_{i}";
            exitGo.transform.SetParent(exitsFolder.transform, false);
            Place(exitGo.transform, root.transform, localPos, terrain, true);
            FaceTowards(exitGo.transform, root.transform.position);

            var exit = exitGo.GetComponent<AreaExit>();
            if (exit == null) exit = exitGo.AddComponent<AreaExit>();
            exit.ExitIndex = i;
            if (exitPrefab == null) DecorateDefaultExit(exit, interactLayer);
        }

        // ===== Boundary =====
        if (boundarySegments >= 6) CreateBoundary(root.transform, terrain);

        // ===== AreaController data =====
        var so = new SerializedObject(area);
        so.FindProperty("areaId").stringValue = id;
        so.FindProperty("type").enumValueIndex = (int)areaType;
        so.FindProperty("entryPoint").objectReferenceValue = entry.transform;
        so.FindProperty("radius").floatValue = areaRadius;

        var exits = root.GetComponentsInChildren<AreaExit>(true);
        var spawnPoints = root.GetComponentsInChildren<AreaSpawnPoint>(true);

        var exitsProp = so.FindProperty("exits");
        exitsProp.arraySize = exits.Length;
        for (int i = 0; i < exits.Length; i++) exitsProp.GetArrayElementAtIndex(i).objectReferenceValue = exits[i];

        var spawnsProp = so.FindProperty("spawnPoints");
        spawnsProp.arraySize = spawnPoints.Length;
        for (int i = 0; i < spawnPoints.Length; i++) spawnsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        if (misplaced > 0)
            Debug.LogWarning($"[AreaBuilder] '{id}': {misplaced} spawn point(s) could not find a flat, walkable, well-spaced spot - check them in the scene.", root);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
    }

    // Returns 1 when no good spot was found (the last candidate is used anyway).
    private int CreateSpawnPoint(Transform parent, string name, AreaSpawnPoint.Kind kind, GameObject[] prefabs,
        AreaSpawnPoint.SpawnTiming timing, List<Vector2> used, Terrain terrain, Transform areaRoot, Transform entry)
    {
        var go = NewChild(name, parent);

        bool ok = true;
        Vector3 local = randomizePositions
            ? FindSpot(used, areaRadius * 0.72f, minDistanceBetweenPoints, terrain, areaRoot, out ok)
            : new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

        used.Add(Place(go.transform, areaRoot, local, terrain, true));
        if (kind == AreaSpawnPoint.Kind.Enemy) FaceTowards(go.transform, entry.position);

        var point = go.AddComponent<AreaSpawnPoint>();
        var so = new SerializedObject(point);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("chance").floatValue = 1f;
        so.FindProperty("timing").enumValueIndex = (int)timing;

        if (prefabs is { Length: > 0 })
        {
            var prefabProp = so.FindProperty("prefabs");
            prefabProp.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++) prefabProp.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        // The area has exactly one node type, so every point is active for it.
        var activeProp = so.FindProperty("activeFor");
        activeProp.arraySize = 1;
        activeProp.GetArrayElementAtIndex(0).enumValueIndex = (int)areaType;
        so.ApplyModifiedPropertiesWithoutUndo();

        return ok ? 0 : 1;
    }

    private void DecorateDefaultExit(AreaExit exit, int interactLayer)
    {
        var go = exit.gameObject;
        if (interactLayer >= 0) go.layer = interactLayer;

        if (go.GetComponent<Collider>() == null)
        {
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 1.2f;
            collider.center = Vector3.up * 0.8f; // solid, not a trigger: click raycasts ignore triggers
        }

        var labelObject = NewChild("Label", go.transform);
        labelObject.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        var label = labelObject.AddComponent<TextMeshPro>();
        label.text = "?";
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;

        var so = new SerializedObject(exit);
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Ring of carving NavMeshObstacles just outside the area: active only while the area is active.
    private void CreateBoundary(Transform areaRoot, Terrain terrain)
    {
        var ring = NewChild("Boundary", areaRoot);
        float radius = areaRadius + 1.5f;
        float length = 2f * Mathf.PI * radius / boundarySegments * 1.08f;

        for (int i = 0; i < boundarySegments; i++)
        {
            float angle = 360f * i / boundarySegments;
            var wall = NewChild($"Wall_{i}", ring.transform);

            Vector3 world = areaRoot.TransformPoint(Polar(angle, radius));
            world.y = terrain.SampleHeight(world) + terrain.transform.position.y;
            wall.transform.position = world;
            wall.transform.rotation = Quaternion.Euler(0f, angle, 0f); // local X = tangent, local Z = outward

            var obstacle = wall.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(length, 6f, boundaryThickness);
            obstacle.center = new Vector3(0f, 2f, 0f);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }
    }

    // ================================================================== placement helpers

    private bool TryFindCenter(Terrain terrain, out Vector3 center)
    {
        var data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;

        switch (placement)
        {
            case Placement.TerrainCenter:
                center = new Vector3(origin.x + data.size.x * 0.5f, origin.y, origin.z + data.size.z * 0.5f);
                return true;
            case Placement.Manual:
                center = manualPosition;
                return true;
        }

        var existing = FindObjectsByType<AreaController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float margin = areaRadius + boundaryThickness + 3f;
        float step = Mathf.Max(2f, areaRadius * 0.5f);

        for (float z = origin.z + margin; z <= origin.z + data.size.z - margin; z += step)
        {
            for (float x = origin.x + margin; x <= origin.x + data.size.x - margin; x += step)
            {
                var candidate = new Vector3(x, origin.y, z);
                bool free = true;
                foreach (var other in existing)
                {
                    Vector3 delta = other.transform.position - candidate;
                    delta.y = 0f;
                    if (delta.magnitude < areaRadius + other.Radius + areaGap) { free = false; break; }
                }
                if (!free) continue;

                center = candidate;
                return true;
            }
        }

        center = default;
        return false;
    }

    // Puts 't' at the given XZ (local to the area root) on the terrain. Returns the final XZ in area-local space.
    private Vector2 Place(Transform t, Transform areaRoot, Vector3 local, Terrain terrain, bool allowNavMeshSnap)
    {
        Vector3 world = areaRoot.TransformPoint(new Vector3(local.x, 0f, local.z));
        world.y = terrain.SampleHeight(world) + terrain.transform.position.y;

        if (allowNavMeshSnap && snapToNavMesh && _navMeshBaked && NavMesh.SamplePosition(world, out var hit, 2f, NavMesh.AllAreas))
            world = hit.position;

        world.y += heightOffset;
        t.position = world;

        Vector3 result = areaRoot.InverseTransformPoint(t.position);
        return new Vector2(result.x, result.z);
    }

    private Vector3 FindSpot(List<Vector2> used, float radius, float minDistance, Terrain terrain, Transform areaRoot, out bool ok)
    {
        Vector3 last = RandomInDisc(radius);
        for (int attempt = 0; attempt < 60; attempt++)
        {
            Vector3 candidate = RandomInDisc(radius);
            last = candidate;

            bool tooClose = false;
            foreach (var p in used)
            {
                if (Vector2.Distance(new Vector2(candidate.x, candidate.z), p) < minDistance) { tooClose = true; break; }
            }
            if (tooClose || !IsWalkable(areaRoot.TransformPoint(candidate), terrain)) continue;

            ok = true;
            return candidate;
        }

        ok = false;
        return last;
    }

    private bool IsWalkable(Vector3 world, Terrain terrain)
    {
        var data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        float nx = (world.x - origin.x) / data.size.x;
        float nz = (world.z - origin.z) / data.size.z;
        if (nx < 0f || nx > 1f || nz < 0f || nz > 1f) return false;
        if (data.GetSteepness(nx, nz) > maxSlope) return false;

        if (!_navMeshBaked) return true;
        world.y = terrain.SampleHeight(world) + origin.y;
        return NavMesh.SamplePosition(world, out _, 1.5f, NavMesh.AllAreas);
    }

    private static void FaceTowards(Transform t, Vector3 worldTarget)
    {
        Vector3 direction = worldTarget - t.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private static Vector3 Polar(float angleDegrees, float distance)
        => Quaternion.Euler(0f, angleDegrees, 0f) * Vector3.forward * distance;

    private static Vector3 RandomInDisc(float radius)
    {
        Vector2 c = Random.insideUnitCircle * radius;
        return new Vector3(c.x, 0f, c.y);
    }

    private static GameObject NewChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false); // false: local zero, not "keep the world position"
        return go;
    }

    // ================================================================== misc

    private static Transform GetOrCreateAreasContainer()
    {
        var found = GameObject.Find("Areas");
        if (found != null) return found.transform;

        var go = new GameObject("Areas");
        Undo.RegisterCreatedObjectUndo(go, "Create Areas container");
        return go.transform;
    }

    private static string UniqueId(string id)
    {
        var existing = new HashSet<string>();
        foreach (var area in FindObjectsByType<AreaController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            existing.Add(area.AreaId);

        string result = id;
        while (existing.Contains(result)) result = NextId(result);
        return result;
    }

    // Area_Combat_01 -> Area_Combat_02 (no number: appends _02)
    private static string NextId(string id)
    {
        var match = Regex.Match(id, @"^(.*?)(\d+)$");
        if (!match.Success) return id + "_02";

        int next = int.Parse(match.Groups[2].Value) + 1;
        return match.Groups[1].Value + next.ToString().PadLeft(match.Groups[2].Length, '0');
    }

    private static int FindInteractLayer()
    {
        foreach (var layerName in new[] { "Interact", "Interactable", "Interaction" })
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) return layer;
        }
        Debug.LogWarning("[AreaBuilder] No 'Interact' layer found - set the exits' layer (the one used as interactLayer) by hand.");
        return -1;
    }

    private Terrain CreateSimpleTerrain()
    {
        const string dir = "Assets/Map";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        var terrainData = new TerrainData
        {
            heightmapResolution = 257,
            size = new Vector3(newTerrainSize.x, 30f, newTerrainSize.y)
        };

        // TerrainData must be an asset, otherwise it is lost when the scene is saved and reopened.
        AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath(dir + "/Terrain_Auto.asset"));

        var go = Terrain.CreateTerrainGameObject(terrainData);
        go.name = "Terrain_Auto";
        Undo.RegisterCreatedObjectUndo(go, "Create Terrain");
        return go.GetComponent<Terrain>();
    }
}
#endif