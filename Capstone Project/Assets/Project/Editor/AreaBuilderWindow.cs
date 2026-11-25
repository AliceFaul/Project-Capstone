#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class AreaBuilderWindow : EditorWindow
{
    private enum Placement { AutoFreeSlot, TerrainCenter, Manual }

    [SerializeField] private string areaId = "Area_Combat_01";
    [SerializeField] private NodeType areaType = NodeType.Combat;
    [SerializeField] private int batchCount = 1;
    [SerializeField] private float areaRadius = 14f;
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Vector2 newTerrainSize = new Vector2(200f, 200f);
    [SerializeField] private Placement placement;
    [SerializeField] private Vector3 manualPosition;
    [SerializeField] private float areaGap = 6f;
    [SerializeField] private bool parentUnderAreasObject = true;

    [SerializeField] private int enemySpawnCount = 4;
    [SerializeField] private int chestSpawnCount;
    [SerializeField] private int propSpawnCount;
    [SerializeField] private int exitCount = 2;
    [SerializeField] private AreaSpawnPoint.SpawnTiming chestTiming;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] chestPrefabs;
    [SerializeField] private GameObject[] propPrefabs;
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private int interactionLayer = -1;

    [SerializeField] private bool randomizePositions = true;
    [SerializeField] private int layoutSeed = 12345;
    [SerializeField] private bool randomEntryAngle;
    [SerializeField] private bool entryAtEdge = true;
    [SerializeField] private float entryAngle;
    [SerializeField] private float exitAngleOffset = 180f;
    [SerializeField] private float exitSpread = 70f;
    [SerializeField] private float minDistanceBetweenPoints = 2.8f;
    [SerializeField] private float heightOffset = 0.05f;
    [SerializeField] private float maxSlope = 25f;

    [SerializeField] private bool requireNavMesh = true;
    [SerializeField] private bool snapToNavMesh = true;
    [SerializeField] private bool checkPaths = true;
    [SerializeField] private NavMeshAgent referenceAgent;
    [SerializeField] private int agentTypeId;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;
    [SerializeField] private float sampleDistance = 1.5f;
    [SerializeField] private float maxHeightDifference = 0.6f;

    [SerializeField] private bool createBoundary = true;
    [SerializeField] private int boundarySegments = 24;
    [SerializeField] private float boundaryThickness = 2f;
    [SerializeField] private bool drawSceneLabels = true;
    [SerializeField] private Vector2 scroll;
    private List<AreaBuilderLayout.Plan> preview;
    private readonly List<Issue> issues = new List<Issue>();
    private string status;

    private sealed class Issue
    {
        public string Text;
        public UnityEngine.Object Target;
        public Vector3 Position;
        public bool Error;
    }

    [MenuItem("Tools/Map/Area Builder")]
    public static void Open()
    {
        var window = GetWindow<AreaBuilderWindow>("Area Builder");
        window.minSize = new Vector2(440f, 600f);
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += DrawScene;
        if (interactionLayer < 0) interactionLayer = FindInteractLayer();
        if (targetTerrain == null && Terrain.activeTerrains.Length == 1) targetTerrain = Terrain.activeTerrain;
    }

    private void OnDisable() => SceneView.duringSceneGui -= DrawScene;

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Area Builder v2 - Shared Terrain", EditorStyles.boldLabel);
            areaId = EditorGUILayout.TextField("Area Id", areaId);
            var newType = (NodeType)EditorGUILayout.EnumPopup("Node Type", areaType);
            if (newType != areaType) { areaType = newType; ApplyPreset(); }
            areaRadius = EditorGUILayout.Slider("Area Radius", areaRadius, 8f, 40f);

            Label("Terrain & Placement");
            targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
            if (targetTerrain == null)
            {
                newTerrainSize = EditorGUILayout.Vector2Field("New Size (X, Z)", newTerrainSize);
                if (GUILayout.Button("Create Empty Terrain")) CreateTerrain();
            }
            placement = (Placement)EditorGUILayout.EnumPopup("Placement", placement);
            if (placement != Placement.AutoFreeSlot) batchCount = 1;
            using (new EditorGUI.DisabledScope(placement != Placement.AutoFreeSlot))
                batchCount = EditorGUILayout.IntSlider("Areas To Create", batchCount, 1, 12);
            if (placement == Placement.Manual)
            {
                manualPosition = EditorGUILayout.Vector3Field("World Position (Y auto)", manualPosition);
                using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
                    if (GUILayout.Button("Use Selected Object Position")) manualPosition = Selection.activeTransform.position;
            }
            areaGap = EditorGUILayout.Slider("Gap Between Boundaries", areaGap, 0f, 30f);
            parentUnderAreasObject = EditorGUILayout.Toggle("Group Under 'Areas'", parentUnderAreasObject);

            Label("Spawn Counts & Prefabs");
            enemySpawnCount = EditorGUILayout.IntSlider("Enemy Spawns", enemySpawnCount, 0, 12);
            chestSpawnCount = EditorGUILayout.IntSlider("Chest Spawns", chestSpawnCount, 0, 4);
            if (chestSpawnCount > 0) chestTiming = (AreaSpawnPoint.SpawnTiming)EditorGUILayout.EnumPopup("Chest Timing", chestTiming);
            propSpawnCount = EditorGUILayout.IntSlider("Prop Spawns", propSpawnCount, 0, 8);
            exitCount = EditorGUILayout.IntSlider("Exits", exitCount, 0, 4);
            DrawPrefabList("Enemy Prefabs", ref enemyPrefabs);
            DrawPrefabList("Chest Prefabs", ref chestPrefabs);
            DrawPrefabList("Prop Prefabs", ref propPrefabs);
            exitPrefab = (GameObject)EditorGUILayout.ObjectField("Exit Prefab", exitPrefab, typeof(GameObject), false);
            interactionLayer = EditorGUILayout.LayerField("Exit Interaction Layer", interactionLayer);

            Label("Layout & Direction");
            randomizePositions = EditorGUILayout.Toggle("Randomize Spawn Positions", randomizePositions);
            layoutSeed = EditorGUILayout.IntField("Layout Seed", layoutSeed);
            using (new EditorGUI.DisabledScope(!randomizePositions))
                randomEntryAngle = EditorGUILayout.Toggle("Randomize Entry Angle", randomEntryAngle);
            entryAtEdge = EditorGUILayout.Toggle("Entry At Edge", entryAtEdge);
            entryAngle = EditorGUILayout.Slider("Entry Angle (world Y)", entryAngle, 0f, 360f);
            exitAngleOffset = EditorGUILayout.Slider("Exit Offset From Entry", exitAngleOffset, 0f, 360f);
            exitSpread = EditorGUILayout.Slider("Exit Fan Spread", exitSpread, 0f, 180f);
            minDistanceBetweenPoints = EditorGUILayout.Slider("Min Point Distance", minDistanceBetweenPoints, 1.5f, 8f);
            heightOffset = EditorGUILayout.Slider("Height Offset", heightOffset, 0f, 0.5f);
            maxSlope = EditorGUILayout.Slider("Max Slope (degrees)", maxSlope, 5f, 60f);
            EditorGUILayout.HelpBox("0 = +Z, 90 = +X. Entry/Exit angle giup can theo duong ground03; tool khong doc hay paint TerrainLayer.", MessageType.Info);

            Label("Navigation");
            requireNavMesh = EditorGUILayout.Toggle("Require Baked NavMesh", requireNavMesh);
            referenceAgent = (NavMeshAgent)EditorGUILayout.ObjectField("Player / Reference Agent", referenceAgent, typeof(NavMeshAgent), true);
            if (referenceAgent != null)
            {
                agentTypeId = referenceAgent.agentTypeID;
                navMeshAreaMask = referenceAgent.areaMask;
            }
            using (new EditorGUI.DisabledScope(referenceAgent != null))
            {
                DrawAgentTypes();
                navMeshAreaMask = EditorGUILayout.IntField("NavMesh Area Mask (-1=all)", navMeshAreaMask);
            }
            using (new EditorGUI.DisabledScope(!requireNavMesh))
            {
                snapToNavMesh = EditorGUILayout.Toggle("Snap During Creation", snapToNavMesh);
                checkPaths = EditorGUILayout.Toggle("Require Complete Paths", checkPaths);
                sampleDistance = EditorGUILayout.Slider("Max Snap Distance", sampleDistance, 0.1f, 2f);
                maxHeightDifference = EditorGUILayout.Slider("Max Nav/Terrain Y Delta", maxHeightDifference, 0.05f, 2f);
            }
            if (!requireNavMesh)
                EditorGUILayout.HelpBox("Layout only: chua kiem tra NavMesh/duong di. Bake Terrain roi bat lai va Validate truoc Play.", MessageType.Warning);

            Label("Boundary");
            createBoundary = EditorGUILayout.Toggle("Create Carving Ring", createBoundary);
            if (createBoundary)
            {
                boundarySegments = EditorGUILayout.IntSlider("Wall Segments", boundarySegments, 6, 48);
                boundaryThickness = EditorGUILayout.Slider("Thickness", boundaryThickness, 0.5f, 6f);
            }
            EditorGUILayout.HelpBox("Boundary dung NavMeshObstacle Carve, bat/tat cung Area. Day la ranh gioi navigation; di chuyen Transform truc tiep khong bi chan boi no.", MessageType.Info);

            if (EditorGUI.EndChangeCheck()) { preview = null; SceneView.RepaintAll(); }
            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Layout", GUILayout.Height(32f))) Preview();
                if (GUILayout.Button("Create " + batchCount + " Area(s)", GUILayout.Height(32f))) CreateAreas();
            }
            Label("Validate Existing Areas (read only)");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Selected")) ValidateAreas(true);
                if (GUILayout.Button("Validate Terrain Areas")) ValidateAreas(false);
            }
            Label("Update Existing Boundary");
            using (new EditorGUI.DisabledScope(!createBoundary))
                if (GUILayout.Button("Rebuild Selected Carving Rings (Undo)")) RebuildSelectedBoundaries();
            drawSceneLabels = EditorGUILayout.Toggle("Show Scene Labels", drawSceneLabels);
            if (GUILayout.Button("Clear Preview / Results")) { preview = null; issues.Clear(); status = null; SceneView.RepaintAll(); }
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        for (int i = 0; i < issues.Count; i++)
        {
            var issue = issues[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox((i + 1) + ". " + issue.Text, issue.Error ? MessageType.Error : MessageType.Warning);
                if (GUILayout.Button("Find", GUILayout.Width(42f), GUILayout.Height(30f)))
                {
                    Selection.activeObject = issue.Target;
                    EditorGUIUtility.PingObject(issue.Target);
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.Frame(new Bounds(issue.Position, Vector3.one * 8f), false);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static void Label(string text) { EditorGUILayout.Space(8f); GUILayout.Label(text, EditorStyles.boldLabel); }

    private static void DrawPrefabList(string label, ref GameObject[] prefabs)
    {
        int size = Mathf.Clamp(EditorGUILayout.IntField(label + " Count", prefabs == null ? 0 : prefabs.Length), 0, 64);
        if (prefabs == null || prefabs.Length != size) Array.Resize(ref prefabs, size);
        for (int i = 0; i < size; i++)
            prefabs[i] = (GameObject)EditorGUILayout.ObjectField("  [" + i + "]", prefabs[i], typeof(GameObject), false);
    }

    private void DrawAgentTypes()
    {
        int count = NavMesh.GetSettingsCount();
        if (count == 0) { EditorGUILayout.HelpBox("Khong co Agent Type. Kiem tra Navigation settings.", MessageType.Warning); return; }
        var names = new string[count];
        var ids = new int[count];
        int selected = 0;
        for (int i = 0; i < count; i++)
        {
            ids[i] = NavMesh.GetSettingsByIndex(i).agentTypeID;
            names[i] = NavMesh.GetSettingsNameFromID(ids[i]);
            if (ids[i] == agentTypeId) selected = i;
        }
        agentTypeId = ids[EditorGUILayout.Popup("Agent Type", selected, names)];
    }

    private void ApplyPreset()
    {
        enemySpawnCount = chestSpawnCount = propSpawnCount = 0;
        exitCount = areaType == NodeType.Boss ? 0 : 2;
        chestTiming = AreaSpawnPoint.SpawnTiming.OnEnter;
        switch (areaType)
        {
            case NodeType.Combat: enemySpawnCount = 4; break;
            case NodeType.Reward: chestSpawnCount = 2; break;
            case NodeType.Event:
            case NodeType.Rest:
            case NodeType.Shop: propSpawnCount = 1; break;
            case NodeType.Elite: enemySpawnCount = 5; chestSpawnCount = 1; chestTiming = AreaSpawnPoint.SpawnTiming.OnClear; break;
            case NodeType.Boss: enemySpawnCount = 1; chestSpawnCount = 1; chestTiming = AreaSpawnPoint.SpawnTiming.OnClear; break;
        }
        areaId = Regex.Replace(areaId ?? string.Empty, @"^Area_[A-Za-z]+_", "Area_" + areaType + "_");
    }

    private AreaBuilderLayout.Options Options(float radius)
    {
        return new AreaBuilderLayout.Options
        {
            Terrain = targetTerrain, Radius = radius, MinDistance = minDistanceBetweenPoints,
            MaxSlope = maxSlope, HeightOffset = heightOffset, RequireNavMesh = requireNavMesh,
            SnapToNavMesh = snapToNavMesh, CheckPaths = checkPaths,
            AgentTypeId = referenceAgent != null ? referenceAgent.agentTypeID : agentTypeId,
            AreaMask = referenceAgent != null ? referenceAgent.areaMask : navMeshAreaMask,
            SampleDistance = sampleDistance, MaxHeightDifference = maxHeightDifference,
            Randomize = randomizePositions, RandomEntryAngle = randomEntryAngle,
            EntryAtEdge = entryAtEdge, EntryAngle = entryAngle, ExitAngleOffset = exitAngleOffset,
            ExitSpread = exitSpread, EnemyCount = enemySpawnCount, ChestCount = chestSpawnCount,
            PropCount = propSpawnCount, ExitCount = exitCount, Boundary = createBoundary,
            BoundarySegments = boundarySegments, BoundaryThickness = boundaryThickness, Gap = areaGap
        };
    }

    private bool CheckTerrain()
    {
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return Fail("Chon Target Terrain truoc.");
        if (!targetTerrain.gameObject.scene.IsValid() || EditorUtility.IsPersistent(targetTerrain))
            return Fail("Target Terrain phai nam trong scene dang mo.");
        if (Quaternion.Angle(targetTerrain.transform.rotation, Quaternion.identity) > 0.01f
            || (targetTerrain.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            return Fail("Terrain can Rotation (0,0,0), Scale (1,1,1). Vi tri Terrain co the khac goc toa do.");
        return true;
    }

    private bool CheckCreationSettings()
    {
        if (!CheckTerrain()) return false;
        if (string.IsNullOrWhiteSpace(areaId)) return Fail("Area Id khong duoc rong.");
        if (exitCount > 0 && (interactionLayer < 0 || interactionLayer > 31))
            return Fail("Chon Exit Interaction Layer khop voi PlayerController.interactLayer.");
        if (requireNavMesh && NavMesh.CalculateTriangulation().vertices.Length == 0)
            return Fail("Chua co NavMesh. Bake truoc, hoac tat Require Baked NavMesh de chi tao layout.");
        if (requireNavMesh && Options(areaRadius).AreaMask == 0) return Fail("NavMesh Area Mask khong duoc bang 0.");
        if (parentUnderAreasObject)
        {
            var parent = FindContainer();
            if (parent != null && (!parent.gameObject.activeInHierarchy
                || (parent.lossyScale - Vector3.one).sqrMagnitude > 0.0001f
                || Quaternion.Angle(parent.rotation, Quaternion.identity) > 0.01f))
                return Fail("Root 'Areas' can active, Rotation 0, Scale 1 de area hoat dong dung.");
        }
        if (exitPrefab != null)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(exitPrefab)) return Fail("Exit Prefab phai la prefab asset.");
            var components = exitPrefab.GetComponentsInChildren<AreaExit>(true);
            if (components.Length > 1 || (components.Length == 1 && components[0].gameObject != exitPrefab))
                return Fail("Exit Prefab can toi da mot AreaExit va component nay phai o root. Tool se khong them AreaExit trung lap.");
        }
        return true;
    }

    private bool Fail(string message)
    {
        status = message;
        Debug.LogWarning("[AreaBuilder] " + message, targetTerrain);
        Repaint();
        return false;
    }

    private bool BuildPlans(out List<AreaBuilderLayout.Plan> plans)
    {
        plans = null;
        if (!CheckCreationSettings()) return false;
        var options = Options(areaRadius);
        var occupied = new List<AreaBuilderLayout.OccupiedArea>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var area in SceneAreas())
        {
            ids.Add(area.AreaId);
            occupied.Add(new AreaBuilderLayout.OccupiedArea(area.transform.position, Footprint(area), area.AreaId));
        }
        var random = new System.Random(layoutSeed);
        var result = new List<AreaBuilderLayout.Plan>();
        string nextId = areaId.Trim();
        int count = placement == Placement.AutoFreeSlot ? batchCount : 1;
        try
        {
            for (int n = 0; n < count; n++)
            {
                AreaBuilderLayout.Plan plan = null;
                string reason = "Khong con cho trong tren Terrain.";
                if (placement == Placement.AutoFreeSlot)
                {
                    Vector3 origin = targetTerrain.transform.position;
                    Vector3 size = targetTerrain.terrainData.size;
                    float margin = options.Footprint + 0.1f;
                    float step = Mathf.Max(2f, areaRadius * 0.5f);
                    int checkedCenters = 0;
                    for (float z = origin.z + margin; z <= origin.z + size.z - margin && plan == null; z += step)
                    for (float x = origin.x + margin; x <= origin.x + size.x - margin && plan == null; x += step)
                    {
                        var center = new Vector3(x, origin.y, z);
                        if (++checkedCenters % 16 == 0 && EditorUtility.DisplayCancelableProgressBar("Area Builder",
                            "Planning area " + (n + 1) + "/" + count + " - center " + checkedCenters, n / (float)count))
                            return Fail("Da huy. Khong tao object nao.");
                        if (!AreaBuilderLayout.CheckCenter(options, center, occupied, out reason)) continue;
                        AreaBuilderLayout.TryPlan(options, center, random, out plan, out reason);
                    }
                }
                else
                {
                    Vector3 center = placement == Placement.Manual ? manualPosition
                        : targetTerrain.transform.position + targetTerrain.terrainData.size * 0.5f;
                    if (AreaBuilderLayout.CheckCenter(options, center, occupied, out reason))
                        AreaBuilderLayout.TryPlan(options, center, random, out plan, out reason);
                }
                if (plan == null) return Fail("Khong tao batch: area " + (n + 1) + ". " + reason);
                while (ids.Contains(nextId)) nextId = NextId(nextId);
                plan.Id = nextId;
                ids.Add(nextId);
                nextId = NextId(nextId);
                result.Add(plan);
                occupied.Add(new AreaBuilderLayout.OccupiedArea(plan.Center, options.Footprint, plan.Id));
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        plans = result;
        return true;
    }

    private void Preview()
    {
        preview = null;
        if (BuildPlans(out var plans))
        {
            preview = plans;
            status = "Preview " + plans.Count + " area(s). Green=Entry, cyan=Exit, red=Enemy, yellow=Chest. Chua tao object.";
        }
        SceneView.RepaintAll();
    }

    private void CreateAreas()
    {
        preview = null;
        if (!BuildPlans(out var plans)) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Terrain Areas");
        var created = new List<UnityEngine.Object>();
        try
        {
            Transform parent = parentUnderAreasObject ? GetOrCreateContainer() : null;
            foreach (var plan in plans)
            {
                var root = CreateArea(plan, parent);
                Undo.RegisterCreatedObjectUndo(root, "Create Terrain Area");
                created.Add(root);
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(targetTerrain.gameObject.scene);
            areaId = NextId(plans[plans.Count - 1].Id);
            Selection.objects = created.ToArray();
            ValidateAreas(true);
            status = "Da tao " + created.Count + " area(s). Undo mot lan de huy batch. " + status;
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Fail("Da huy batch vi loi: " + exception.Message);
            Debug.LogException(exception);
        }
        SceneView.RepaintAll();
    }

    private GameObject CreateArea(AreaBuilderLayout.Plan plan, Transform parent)
    {
        var root = new GameObject(plan.Id);
        SceneManager.MoveGameObjectToScene(root, targetTerrain.gameObject.scene);
        try
        {
            root.transform.position = plan.Center;
            if (parent != null) root.transform.SetParent(parent, true);
            var area = root.AddComponent<AreaController>();
            var entry = NewChild("EntryPoint", root.transform);
            entry.transform.position = plan.Entry;
            FaceTowards(entry.transform, plan.Center);
            var spawnParent = NewChild("Spawns", root.transform).transform;
            var exitParent = NewChild("Exits", root.transform).transform;
            int enemy = 0, chest = 0, prop = 0;
            foreach (var spawn in plan.Spawns)
            {
                int index = spawn.Kind == AreaSpawnPoint.Kind.Enemy ? ++enemy
                    : spawn.Kind == AreaSpawnPoint.Kind.Chest ? ++chest : ++prop;
                var go = NewChild("Spawn_" + spawn.Kind + "_" + index, spawnParent);
                go.transform.position = spawn.Position;
                if (spawn.Kind == AreaSpawnPoint.Kind.Enemy) FaceTowards(go.transform, plan.Entry);
                var point = go.AddComponent<AreaSpawnPoint>();
                var data = new SerializedObject(point);
                Property(data, "kind").enumValueIndex = (int)spawn.Kind;
                Property(data, "chance").floatValue = 1f;
                Property(data, "timing").enumValueIndex = (int)(spawn.Kind == AreaSpawnPoint.Kind.Chest ? chestTiming : AreaSpawnPoint.SpawnTiming.OnEnter);
                var activeFor = Property(data, "activeFor");
                activeFor.arraySize = 1;
                activeFor.GetArrayElementAtIndex(0).enumValueIndex = (int)areaType;
                var prefabs = spawn.Kind == AreaSpawnPoint.Kind.Enemy ? enemyPrefabs
                    : spawn.Kind == AreaSpawnPoint.Kind.Chest ? chestPrefabs : propPrefabs;
                WriteReferences(Property(data, "prefabs"), prefabs ?? Array.Empty<GameObject>());
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            for (int i = 0; i < plan.Exits.Count; i++)
            {
                var go = exitPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(exitPrefab, exitParent)
                    : NewChild("Exit_" + i, exitParent);
                go.name = "Exit_" + i;
                go.SetActive(true);
                go.transform.position = plan.Exits[i];
                FaceTowards(go.transform, plan.Center);
                var exit = go.GetComponent<AreaExit>();
                if (exit == null) exit = go.AddComponent<AreaExit>();
                var data = new SerializedObject(exit);
                Property(data, "exitIndex").intValue = i;
                bool hasClickableCollider = false;
                foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                {
                    collider.gameObject.layer = interactionLayer;
                    hasClickableCollider |= collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy;
                }
                go.layer = interactionLayer;
                if (!hasClickableCollider)
                {
                    var collider = go.AddComponent<SphereCollider>();
                    collider.radius = 1.2f;
                    collider.center = Vector3.up * 0.8f;
                }
                if (exitPrefab == null)
                {
                    var labelGo = NewChild("Label", go.transform);
                    labelGo.transform.localPosition = Vector3.up * 2.2f;
                    var label = labelGo.AddComponent<TextMeshPro>();
                    label.text = "?";
                    label.fontSize = 4f;
                    label.alignment = TextAlignmentOptions.Center;
                    Property(data, "label").objectReferenceValue = label;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                if (exitPrefab != null)
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
                    foreach (var transform in go.GetComponentsInChildren<Transform>(true))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                    }
                }
            }
            if (createBoundary) CreateBoundary(root.transform, areaRadius);
            var areaData = new SerializedObject(area);
            Property(areaData, "areaId").stringValue = plan.Id;
            Property(areaData, "type").enumValueIndex = (int)areaType;
            Property(areaData, "radius").floatValue = areaRadius;
            Property(areaData, "entryPoint").objectReferenceValue = entry.transform;
            WriteReferences(Property(areaData, "exits"), root.GetComponentsInChildren<AreaExit>(true));
            WriteReferences(Property(areaData, "spawnPoints"), root.GetComponentsInChildren<AreaSpawnPoint>(true));
            areaData.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }
        catch { DestroyImmediate(root); throw; }
    }

    private GameObject CreateBoundary(Transform root, float radius)
    {
        var folder = NewChild("Boundary", root).transform;
        float length = AreaBuilderLayout.BoundaryLength(radius, boundarySegments, boundaryThickness);
        float ringRadius = AreaBuilderLayout.BoundaryRadius(radius, boundaryThickness);
        // Cover the Terrain's full height range, including steep ridges between segment endpoints.
        float height = targetTerrain.terrainData.size.y + 8f;
        float centerY = targetTerrain.transform.position.y + targetTerrain.terrainData.size.y * 0.5f + 2f;
        try
        {
            for (int i = 0; i < boundarySegments; i++)
            {
                float angle = 360f * i / boundarySegments;
                var wall = NewChild("Wall_" + i, folder);
                wall.transform.position = AreaBuilderLayout.Ground(targetTerrain,
                    root.position + AreaBuilderLayout.Polar(angle, ringRadius));
                wall.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                var obstacle = wall.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(length, height, boundaryThickness);
                obstacle.center = Vector3.up * (centerY - wall.transform.position.y);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;
                obstacle.carvingTimeToStationary = 0f;
            }
            return folder.gameObject;
        }
        catch { DestroyImmediate(folder.gameObject); throw; }
    }

    private void RebuildSelectedBoundaries()
    {
        if (!CheckTerrain()) return;
        var selected = SelectedAreas();
        if (selected.Count == 0) { Fail("Chon AreaController hoac root Areas truoc."); return; }
        var all = SceneAreas();
        foreach (var area in selected)
        {
            if (area.Radius <= 0f || float.IsNaN(area.Radius) || float.IsInfinity(area.Radius)
                || (area.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            { Fail("Area can Radius hop le va Scale 1: " + area.name); return; }
            float footprint = AreaBuilderLayout.BoundaryFootprint(area.Radius, boundarySegments, boundaryThickness);
            if (!AreaBuilderLayout.OnTerrain(targetTerrain, area.transform.position, footprint))
            { Fail("Boundary moi vuot Terrain: " + area.name); return; }
            foreach (var other in all)
            {
                if (other == area) continue;
                float otherFootprint = selected.Contains(other)
                    ? AreaBuilderLayout.BoundaryFootprint(other.Radius, boundarySegments, boundaryThickness) : Footprint(other);
                if (AreaBuilderLayout.DistanceXZ(area.transform.position, other.transform.position) < footprint + otherFootprint)
                { Fail("Boundary moi chong area: " + area.name + " / " + other.name); return; }
            }
            var old = area.transform.Find("Boundary");
            if (old == null) continue;
            if (old.GetComponents<Component>().Length != 1)
            { Fail("Boundary root co component tuy chinh: " + area.name); return; }
            for (int i = 0; i < old.childCount; i++)
            {
                var wall = old.GetChild(i);
                if (!Regex.IsMatch(wall.name, @"^Wall_\d+$") || wall.childCount != 0
                    || wall.GetComponent<NavMeshObstacle>() == null || wall.GetComponents<Component>().Length != 2)
                { Fail("Boundary co object tuy chinh; chi rebuild vong Wall_* do tool tao: " + area.name); return; }
            }
            foreach (var component in old.GetComponentsInChildren<Component>(true))
                if (!(component is Transform) && !(component is NavMeshObstacle))
                { Fail("Boundary cua '" + area.name + "' chua component tuy chinh. Hay giu/rieng phan do truoc khi rebuild."); return; }
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild Area Carving Rings");
        try
        {
            foreach (var area in selected)
            {
                var old = area.transform.Find("Boundary");
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                var ring = CreateBoundary(area.transform, area.Radius);
                Undo.RegisterCreatedObjectUndo(ring, "Rebuild Carving Ring");
                EditorSceneManager.MarkSceneDirty(area.gameObject.scene);
            }
            Undo.CollapseUndoOperations(group);
            ValidateAreas(true);
            status = "Da rebuild " + selected.Count + " carving ring(s). " + status;
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Fail("Da hoan tac rebuild: " + exception.Message);
        }
    }

    private static HashSet<AreaController> SelectedAreas()
    {
        var selected = new HashSet<AreaController>();
        foreach (var go in Selection.gameObjects)
        {
            var parent = go.GetComponentInParent<AreaController>(true);
            if (parent != null) selected.Add(parent);
            foreach (var child in go.GetComponentsInChildren<AreaController>(true)) selected.Add(child);
        }
        return selected;
    }

    private void ValidateAreas(bool selectedOnly)
    {
        issues.Clear();
        if (!CheckTerrain()) return;
        var selected = selectedOnly ? SelectedAreas() : new HashSet<AreaController>();
        var all = SceneAreas();
        var idOwners = new Dictionary<string, AreaController>(StringComparer.Ordinal);
        int validated = 0;
        foreach (var area in all)
        {
            if (selectedOnly ? !selected.Contains(area) : !AreaBuilderLayout.OnTerrain(targetTerrain, area.transform.position)) continue;
            validated++;
            ValidateArea(area, all);
        }
        foreach (var area in all)
        {
            if (idOwners.TryGetValue(area.AreaId, out var owner))
            {
                if (!selectedOnly || selected.Contains(area) || selected.Contains(owner))
                    AddIssue("Trung Area Id: " + area.AreaId, area, area.transform.position, true);
            }
            else idOwners.Add(area.AreaId, area);
        }
        int errors = 0;
        foreach (var issue in issues) if (issue.Error) errors++;
        status = "Da kiem tra " + validated + " area(s): " + errors + " loi, " + (issues.Count - errors) + " canh bao."
            + (!requireNavMesh ? " Chua kiem tra NavMesh." : !checkPaths ? " Chua kiem tra duong di." : " Can test carving trong Play Mode.");
        SceneView.RepaintAll();
    }

    private void ValidateArea(AreaController area, AreaController[] all)
    {
        Vector3 center = area.transform.position;
        if (area.Radius <= 0f || float.IsNaN(area.Radius) || float.IsInfinity(area.Radius)) { AddIssue("Radius khong hop le.", area, center, true); return; }
        var options = Options(area.Radius);
        if (!AreaBuilderLayout.OnTerrain(targetTerrain, center, Footprint(area)))
            AddIssue("Area/boundary vuot mep Terrain.", area, center, true);
        if ((area.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
            AddIssue("Area dang co Scale khac 1; Radius va boundary co the khong khop.", area, center, true);
        foreach (var other in all)
        {
            if (other == area) continue;
            if (AreaBuilderLayout.DistanceXZ(center, other.transform.position) < Footprint(area) + Footprint(other))
                AddIssue("Boundary/area chong voi '" + other.AreaId + "'.", area, center, true);
        }
        var data = new SerializedObject(area);
        var entryProperty = data.FindProperty("entryPoint");
        var entry = entryProperty != null ? entryProperty.objectReferenceValue as Transform : null;
        var points = area.GetComponentsInChildren<AreaSpawnPoint>(true);
        var exits = area.GetComponentsInChildren<AreaExit>(true);
        ValidateReferences(data.FindProperty("spawnPoints"), points, "spawnPoints", area);
        ValidateReferences(data.FindProperty("exits"), exits, "exits", area);
        var occupied = new List<Vector3>();
        var path = new NavMeshPath();
        Vector3 entryPosition = area.EntryPosition;
        bool validEntry = false;
        if (entry == null) AddIssue("Chua gan EntryPoint.", area, entryPosition, true);
        else
        {
            if (!entry.IsChildOf(area.transform)) AddIssue("EntryPoint khong nam trong area.", area, entry.position, true);
            validEntry = ValidatePoint(options, center, entry, entryPosition, false, path, out entryPosition);
            occupied.Add(entry.position);
        }
        foreach (var exit in exits)
        {
            ValidatePoint(options, center, exit.transform, entryPosition, validEntry, path, out _);
            ValidateSpacing(options, exit, occupied);
            ValidateExit(exit);
        }
        foreach (var point in points)
        {
            ValidatePoint(options, center, point.transform, entryPosition, validEntry, path, out _);
            ValidateSpacing(options, point, occupied);
            var pointData = new SerializedObject(point);
            var prefabs = pointData.FindProperty("prefabs");
            bool missing = prefabs == null || prefabs.arraySize == 0;
            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.arraySize; i++)
                {
                    var prefab = prefabs.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (prefab == null) { missing = true; continue; }
                    foreach (var agent in prefab.GetComponentsInChildren<NavMeshAgent>(true))
                        if (requireNavMesh && agent.agentTypeID != options.AgentTypeId)
                            AddIssue("Prefab '" + prefab.name + "' dung Agent Type khac player; can kiem tra NavMesh rieng.", point, point.transform.position, false);
                }
            }
            if (missing) AddIssue("Spawn chua gan prefab hoac co phan tu null; co the khong spawn gi.", point, point.transform.position, false);
            if (!point.IsActiveFor(area.Type)) AddIssue("Spawn khong active cho Node Type cua area.", point, point.transform.position, false);
        }
        var indices = new HashSet<int>();
        foreach (var exit in exits)
            if (exit.ExitIndex < 0 || !indices.Add(exit.ExitIndex))
                AddIssue("ExitIndex am hoac trung lap.", exit, exit.transform.position, true);
        if (exits.Length == 0 && area.Type != NodeType.Boss)
            AddIssue("Area khong co Exit; kiem tra so nhanh cua node.", area, center, false);
        var boundary = area.transform.Find("Boundary");
        if (boundary == null)
            AddIssue("Khong tim thay Boundary; can ranh gioi navigation tu dia hinh/props.", area, center, false);
        else
        {
            var obstacles = boundary.GetComponentsInChildren<NavMeshObstacle>(true);
            if (obstacles.Length < 6) AddIssue("Boundary co it hon 6 obstacle; kiem tra vong chan.", area, center, false);
            foreach (var obstacle in obstacles)
                if (!obstacle.enabled || !obstacle.carving || !ActiveBelow(obstacle.transform, area.transform))
                    AddIssue("Boundary co obstacle dang tat hoac khong Carve.", obstacle, obstacle.transform.position, true);
        }
    }

    private bool ValidatePoint(AreaBuilderLayout.Options options, Vector3 center, Transform point,
        Vector3 entry, bool validatePath, NavMeshPath path, out Vector3 projected)
    {
        if (!AreaBuilderLayout.TryPoint(options, center, point.position, false, out projected, out var reason))
        {
            AddIssue(point.name + ": " + reason, point, point.position, true);
            return false;
        }
        if (Mathf.Abs(point.position.y - projected.y) > 0.25f)
            AddIssue(point.name + ": cao do khac mat dat/NavMesh hien tai.", point, point.position, false);
        if (validatePath && !AreaBuilderLayout.CheckPath(options, center, entry, projected, path, out reason))
        {
            AddIssue(point.name + ": " + reason, point, point.position, true);
            return false;
        }
        return true;
    }

    private void ValidateSpacing(AreaBuilderLayout.Options options, Component point, List<Vector3> occupied)
    {
        if (!AreaBuilderLayout.WellSpaced(point.transform.position, occupied, options.MinDistance))
            AddIssue(point.name + ": qua gan diem khac (theo Min Point Distance hien tai).", point, point.transform.position, false);
        occupied.Add(point.transform.position);
    }

    private void ValidateExit(AreaExit exit)
    {
        bool clickable = false;
        int expectedMask = interactionLayer < 0 ? 0 : 1 << interactionLayer;
        if (referenceAgent != null)
        {
            var player = referenceAgent.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                var mask = new SerializedObject(player).FindProperty("interactLayer");
                if (mask != null) expectedMask = mask.intValue;
            }
        }
        foreach (var collider in exit.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled || collider.isTrigger) continue;
            if (ActiveBelow(collider.transform, exit.transform)
                && (expectedMask & (1 << collider.gameObject.layer)) != 0) clickable = true;
        }
        if (!clickable) AddIssue("Exit thieu collider non-trigger dang bat tren Interact Layer cua player.", exit, exit.transform.position, true);
    }

    private void ValidateReferences(SerializedProperty property, UnityEngine.Object[] children, string name, AreaController area)
    {
        if (property == null) { AddIssue("Khong tim thay serialized field '" + name + "'.", area, area.transform.position, true); return; }
        var actual = new HashSet<UnityEngine.Object>();
        bool invalid = false;
        for (int i = 0; i < property.arraySize; i++)
        {
            var value = property.GetArrayElementAtIndex(i).objectReferenceValue;
            if (value == null || !actual.Add(value)) invalid = true;
        }
        if (invalid || !actual.SetEquals(children))
            AddIssue("References '" + name + "' khong khop cac marker con. Gan lai array trong Inspector.", area, area.transform.position, true);
    }

    private void AddIssue(string text, UnityEngine.Object target, Vector3 position, bool error)
        => issues.Add(new Issue { Text = text, Target = target, Position = position, Error = error });

    private static bool ActiveBelow(Transform child, Transform root)
    {
        for (var transform = child; transform != root; transform = transform.parent)
            if (transform == null || !transform.gameObject.activeSelf) return false;
        return true;
    }

    private void DrawScene(SceneView sceneView)
    {
        Color oldColor = Handles.color;
        if (preview != null)
        {
            foreach (var plan in preview)
            {
                Handles.color = new Color(0.4f, 0.8f, 1f, 0.8f);
                Handles.DrawWireDisc(plan.Center + Vector3.up * 0.1f, Vector3.up, areaRadius);
                if (drawSceneLabels) Handles.Label(plan.Center + Vector3.up, plan.Id + " (preview)");
                DrawPoint(plan.Entry, Color.green, "Entry");
                for (int i = 0; i < plan.Exits.Count; i++) DrawPoint(plan.Exits[i], Color.cyan, "Exit " + i);
                foreach (var point in plan.Spawns)
                    DrawPoint(point.Position, point.Kind == AreaSpawnPoint.Kind.Enemy ? Color.red
                        : point.Kind == AreaSpawnPoint.Kind.Chest ? Color.yellow : Color.white, point.Kind.ToString());
            }
        }
        for (int i = 0; i < issues.Count; i++)
            DrawPoint(issues[i].Position, issues[i].Error ? Color.red : Color.yellow, "Issue " + (i + 1));
        Handles.color = oldColor;
    }

    private void DrawPoint(Vector3 point, Color color, string label)
    {
        Handles.color = color;
        Handles.DrawWireDisc(point + Vector3.up * 0.15f, Vector3.up, 0.45f);
        if (drawSceneLabels) Handles.Label(point + Vector3.up * 0.8f, label);
    }

    private static AreaController[] SceneAreas()
        => FindObjectsByType<AreaController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    private static float Footprint(AreaController area)
    {
        float radius = Mathf.Max(0f, area.Radius);
        var boundary = area.transform.Find("Boundary");
        if (boundary == null) return radius;
        foreach (var obstacle in boundary.GetComponentsInChildren<NavMeshObstacle>(true))
        {
            if (obstacle.shape != NavMeshObstacleShape.Box) continue;
            Vector3 half = obstacle.size * 0.5f;
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = obstacle.transform.TransformPoint(obstacle.center + new Vector3(x * half.x, 0f, z * half.z));
                radius = Mathf.Max(radius, AreaBuilderLayout.DistanceXZ(area.transform.position, corner));
            }
        }
        return radius;
    }

    private Transform FindContainer()
    {
        foreach (var root in targetTerrain.gameObject.scene.GetRootGameObjects())
            if (root.name == "Areas") return root.transform;
        return null;
    }

    private Transform GetOrCreateContainer()
    {
        var existing = FindContainer();
        if (existing != null) return existing;
        var go = new GameObject("Areas");
        SceneManager.MoveGameObjectToScene(go, targetTerrain.gameObject.scene);
        Undo.RegisterCreatedObjectUndo(go, "Create Areas Root");
        return go.transform;
    }

    private static GameObject NewChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void FaceTowards(Transform transform, Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    private static SerializedProperty Property(SerializedObject data, string name)
    {
        var property = data.FindProperty(name);
        if (property == null) throw new InvalidOperationException(data.targetObject.GetType().Name + " missing serialized field: " + name);
        return property;
    }

    private static void WriteReferences(SerializedProperty property, UnityEngine.Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static string NextId(string id)
    {
        var match = Regex.Match(id, @"^(.*?)(\d+)$");
        if (!match.Success || !long.TryParse(match.Groups[2].Value, out long number) || number == long.MaxValue)
            return id + "_02";
        return match.Groups[1].Value + (number + 1).ToString().PadLeft(match.Groups[2].Length, '0');
    }

    private static int FindInteractLayer()
    {
        foreach (string name in new[] { "Interact", "Interactable", "Interaction" })
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0) return layer;
        }
        return -1;
    }

    private void CreateTerrain()
    {
        if (newTerrainSize.x <= 0f || newTerrainSize.y <= 0f
            || float.IsNaN(newTerrainSize.x) || float.IsNaN(newTerrainSize.y)
            || float.IsInfinity(newTerrainSize.x) || float.IsInfinity(newTerrainSize.y))
        {
            Fail("Terrain Size phai la so duong huu han.");
            return;
        }
        const string folder = "Assets/Map";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var data = new TerrainData { heightmapResolution = 257, size = new Vector3(newTerrainSize.x, 30f, newTerrainSize.y) };
        AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(folder + "/Terrain_Auto.asset"));
        var go = Terrain.CreateTerrainGameObject(data);
        go.name = "Terrain_Auto";
        Undo.RegisterCreatedObjectUndo(go, "Create Terrain");
        targetTerrain = go.GetComponent<Terrain>();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        status = "Da tao Terrain. Gan ground01/ground03 va bake NavMesh truoc khi tao area o che do Require NavMesh.";
    }
}
#endif