using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class LevelRunController : MonoBehaviour
{
    public static LevelRunController Instance { get; private set; }

    [SerializeField] private NodeMapConfig config;
    [SerializeField] private bool autoStart = true;
    [Tooltip("0 = a new random seed every run.")]
    [SerializeField] private int seed;
    [Tooltip("Loot difficulty for this run (until the difficulty selection screen exists).")]
    [SerializeField] private int difficulty;
    [SerializeField] private PlayerController player;
    [Tooltip("Empty = every AreaController in the scene (inactive ones too).")]
    [SerializeField] private AreaController[] areas;
    [Tooltip("Called after the player is teleported with the movement delta - hook the camera warp here.")]
    [SerializeField] private UnityEvent<Vector3> onPlayerTeleported;

    public NodeGraph Graph { get; private set; }
    public MapNode Current { get; private set; }
    public int CombatNodesCleared { get; private set; }
    public bool IsTransitioning { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<NodeGraph> RunStarted;
    public event Action<MapNode> NodeEntered;
    public event Action<MapNode> NodeCleared;
    public event Action RunCompleted; // boss node cleared
    public event Action RunFailed;    // call Fail() from the dead / revive system

    private readonly Dictionary<string, AreaController> _areaById = new Dictionary<string, AreaController>();
    private AreaController _currentArea;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        if (!autoStart) yield break;

        while (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            yield return null;
        }
        StartRun(seed);
    }

    public void StartRun(int runSeed)
    {
        if (config == null)
        {
            Debug.LogError("[LevelRunController] No NodeMapConfig assigned.", this);
            return;
        }

        CollectAreas();
        foreach (var area in _areaById.Values) area.gameObject.SetActive(false);

        if (runSeed == 0) runSeed = Environment.TickCount;
        Graph = NodeGraphGenerator.Generate(config, AreasByType(), runSeed);

        foreach (var problem in NodeGraphGenerator.Validate(Graph)) Debug.LogError("[LevelRunController] " + problem, this);
        Debug.Log("[LevelRunController] " + Graph.ToAscii());

        LootService.Difficulty = difficulty;
        CombatNodesCleared = 0;
        Current = null;
        _currentArea = null;
        IsRunning = true;

        RunStarted?.Invoke(Graph);
        StartCoroutine(TransitionTo(Graph.Start, true));
    }

    // Called by AreaExit.
    public void Choose(int nodeId)
    {
        if (!IsRunning || IsTransitioning || Current == null) return;
        if (!Current.Next.Contains(nodeId)) return;
        if (_currentArea == null || !_currentArea.IsCleared) return;

        StartCoroutine(TransitionTo(Graph.Get(nodeId), false));
    }

    public void Fail()
    {
        if (!IsRunning) return;
        IsRunning = false;
        RunFailed?.Invoke();
    }

    private IEnumerator TransitionTo(MapNode node, bool first)
    {
        IsTransitioning = true;
        player.CmdCombatLocked(true);

        float fade = config.fadeDuration;
        var fader = ScreenFader.Instance;
        if (first) fader.SetAlpha(1f);
        else yield return fader.FadeTo(1f, fade);

        if (_currentArea != null)
        {
            _currentArea.Cleared -= OnAreaCleared;
            _currentArea.Leave();
        }

        if (!_areaById.TryGetValue(node.AreaId ?? string.Empty, out var area))
        {
            Debug.LogError($"[LevelRunController] No area '{node.AreaId}' for node {node.Id} ({node.Type}).", this);
            yield return fader.FadeTo(0f, fade);
            player.CmdCombatLocked(false);
            IsTransitioning = false;
            yield break;
        }

        Current = node;
        node.Visited = true;
        _currentArea = area;

        area.gameObject.SetActive(true);
        Vector3 before = player.transform.position;
        Teleport(area.EntryPosition);
        onPlayerTeleported?.Invoke(player.transform.position - before);

        Debug.Log($"[LevelRunController] Entered node {node.Id} ({node.Type}) in area '{area.AreaId}'.");
        NodeEntered?.Invoke(node);
        area.Cleared += OnAreaCleared;
        area.Enter(BuildContext(node));

        yield return null;
        yield return fader.FadeTo(0f, fade);

        player.CmdCombatLocked(false);
        IsTransitioning = false;
    }

    private NodeRunContext BuildContext(MapNode node)
    {
        float health = 1f + config.healthPerCombatNode * CombatNodesCleared;
        float damage = 1f + config.damagePerCombatNode * CombatNodesCleared;

        if (node.Type == NodeType.Elite)
        {
            health *= config.eliteHealthMultiplier;
            damage *= config.eliteDamageMultiplier;
        }
        else if (node.Type == NodeType.Boss)
        {
            health = 1f;
            damage = 1f;
        }

        return new NodeRunContext
        {
            Node = node,
            Graph = Graph,
            CombatNodesCleared = CombatNodesCleared,
            EnemyHealthMultiplier = health,
            EnemyDamageMultiplier = damage,
            Difficulty = difficulty
        };
    }

    private void OnAreaCleared(AreaController area)
    {
        if (Current == null || area != _currentArea) return;

        Current.Cleared = true;
        if (Current.Type == NodeType.Combat || Current.Type == NodeType.Elite) CombatNodesCleared++;

        Debug.Log($"[LevelRunController] Node {Current.Id} ({Current.Type}) cleared.");
        NodeCleared?.Invoke(Current);
        if (Current.Type == NodeType.Boss)
        {
            Debug.Log("[LevelRunController] Run completed.");
            IsRunning = false;
            RunCompleted?.Invoke();
        }
    }

    private void Teleport(Vector3 position)
    {
        player.Movement?.Stop();

        var agent = player.GetComponent<NavMeshAgent>();
        if (agent == null) agent = player.GetComponentInChildren<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(position);
            return;
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            return;
        }

        player.transform.position = position;
    }

    private void CollectAreas()
    {
        _areaById.Clear();
        var found = areas is { Length: > 0 }
            ? areas
            : FindObjectsByType<AreaController>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var area in found)
        {
            if (area == null) continue;
            if (!_areaById.TryAdd(area.AreaId, area)) Debug.LogError($"[LevelRunController] Duplicate area id '{area.AreaId}'.", area);
        }
    }

    private Dictionary<NodeType, List<string>> AreasByType()
    {
        var result = new Dictionary<NodeType, List<string>>();
        foreach (var area in _areaById.Values)
        {
            if (!result.TryGetValue(area.Type, out var list))
            {
                list = new List<string>();
                result[area.Type] = list;
            }
            list.Add(area.AreaId);
        }
        foreach (var list in result.Values) list.Sort(string.CompareOrdinal); // same seed = same map
        return result;
    }

    // Right-click the component > "Log Preview Graph": checks the generator with the areas of the scene (no play mode needed).
    [ContextMenu("Log Preview Graph")]
    private void LogPreviewGraph()
    {
        if (config == null)
        {
            Debug.LogWarning("Assign a NodeMapConfig first.");
            return;
        }

        CollectAreas();
        var graph = NodeGraphGenerator.Generate(config, AreasByType(), seed == 0 ? 12345 : seed);
        Debug.Log(graph.ToAscii());
        foreach (var problem in NodeGraphGenerator.Validate(graph)) Debug.LogWarning(problem);
    }
}