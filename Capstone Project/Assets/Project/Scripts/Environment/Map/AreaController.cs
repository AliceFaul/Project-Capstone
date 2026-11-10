using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// What an area needs to know about the node it is used for.
public struct NodeRunContext
{
    public MapNode Node;
    public NodeGraph Graph;
    public int CombatNodesCleared;
    public float EnemyHealthMultiplier;
    public float EnemyDamageMultiplier;
    public int Difficulty;
}

public class AreaController : MonoBehaviour
{
    public enum ClearMode { Auto, KillAll, Immediate, OpenAllChests }

    [Header("Area")]
    [Tooltip("Unique id. Empty = the GameObject name.")]
    [SerializeField] private string areaId;
    [SerializeField] private NodeType type = NodeType.Combat;
    [SerializeField] private Transform entryPoint;

    [Header("Clearing")]
    [Tooltip("Auto: Combat / Elite / Boss = kill all enemies, every other type is cleared at once.")]
    [SerializeField] private ClearMode clearMode = ClearMode.Auto;

    [Header("Content (filled automatically when the component is added; press Reset in the component menu to refresh)")]
    [SerializeField] private AreaExit[] exits;
    [SerializeField] private AreaSpawnPoint[] spawnPoints;

    [SerializeField] private UnityEvent onEntered;
    [SerializeField] private UnityEvent onCleared;

    public string AreaId => string.IsNullOrEmpty(areaId) ? name : areaId;
    public NodeType Type => type;
    public bool IsCleared { get; private set; }
    public Vector3 EntryPosition => entryPoint != null ? entryPoint.position : transform.position;

    public event Action<AreaController> Cleared;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly List<Mhieu.Enemy.EnemyHealth> _enemies = new();
    private int _enemiesAlive;
    private int _chestsClosed;
    private bool _entered;
    private NodeRunContext _context;
    private readonly HashSet<string> _keys = new HashSet<string>();

    private void Reset()
    {
        exits = GetComponentsInChildren<AreaExit>(true);
        spawnPoints = GetComponentsInChildren<AreaSpawnPoint>(true);
        if (entryPoint == null)
        {
            var found = transform.Find("EntryPoint");
            if (found != null) entryPoint = found;
        }
    }

    public void Enter(NodeRunContext context)
    {
        gameObject.SetActive(true);
        IsCleared = false;
        
        _entered = false;
        _enemiesAlive = 0;
        _chestsClosed = 0;
        _context = context;
        _keys.Clear();

        foreach (var resettable in GetComponentsInChildren<IAreaResettable>(true)) resettable.ResetForEnter();

        Spawn(context, AreaSpawnPoint.SpawnTiming.OnEnter);
        BindExits(context);

        _entered = true;
        onEntered?.Invoke();
        EvaluateClear();
    }

    // Keys found in this area (AreaKey) open its LockedDoors. Cleared on every entry.
    public void AddKey(string keyId) => _keys.Add(keyId);
    public bool HasKey(string keyId) => _keys.Contains(keyId);

    public void Leave()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy != null) enemy.Died -= OnEnemyDied;
        }
        _enemies.Clear();

        foreach (var go in _spawned)
        {
            if (go != null) Destroy(go);
        }
        _spawned.Clear();

        _entered = false;
        gameObject.SetActive(false);
    }

    private void Spawn(NodeRunContext context, AreaSpawnPoint.SpawnTiming timing)
    {
        bool counts = timing == AreaSpawnPoint.SpawnTiming.OnEnter;
        var rng = new System.Random();
        float healthMultiplier = context.EnemyHealthMultiplier;
        float damageMultiplier = context.EnemyDamageMultiplier;

        if (spawnPoints == null) return;

        foreach (var point in spawnPoints)
        {
            if (point == null || point.Timing != timing || !point.IsActiveFor(type)) continue;

            var prefab = point.Pick(rng);
            if (prefab == null) continue;

            Vector3 position = point.transform.position;
            if (NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas)) position = hit.position;

            var instance = Instantiate(prefab, position, point.transform.rotation, transform);
            _spawned.Add(instance);

            switch (point.SpawnKind)
            {
                case AreaSpawnPoint.Kind.Enemy:
                    var health = instance.GetComponentInChildren<Mhieu.Enemy.EnemyHealth>();
                    if (health == null)
                    {
                        Debug.LogWarning($"[AreaController] '{prefab.name}' has no EnemyHealth - it is not counted for clearing.", instance);
                        break;
                    }
                    health.ApplyScaling(healthMultiplier, damageMultiplier);
                    if (!counts) break;
                    health.Died += OnEnemyDied;
                    _enemies.Add(health);
                    _enemiesAlive++;
                    break;

                case AreaSpawnPoint.Kind.Chest:
                    var chest = instance.GetComponentInChildren<Chest>();
                    if (chest == null || !counts) break;
                    chest.Opened += OnChestOpened;
                    _chestsClosed++;
                    break;
            }
        }
    }

    private void BindExits(NodeRunContext context)
    {
        if (exits == null) return;

        var next = context.Node.Next;
        if (next.Count > exits.Length)
            Debug.LogError($"[AreaController] '{AreaId}' has {exits.Length} exits but its node leads to {next.Count} nodes.", this);

        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i] == null) continue;

            if (i < next.Count)
            {
                var target = context.Graph.Get(next[i]);
                exits[i].Bind(target.Type, target.Id, true);
            }
            else
            {
                exits[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnEnemyDied(Mhieu.Enemy.EnemyHealth health)
    {
        health.Died -= OnEnemyDied;
        _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
        EvaluateClear();
    }

    private void OnChestOpened(Chest chest)
    {
        chest.Opened -= OnChestOpened;
        _chestsClosed = Mathf.Max(0, _chestsClosed - 1);
        EvaluateClear();
    }

    private void EvaluateClear()
    {
        if (!_entered || IsCleared) return;

        var mode = clearMode;
        if (mode == ClearMode.Auto)
            mode = type == NodeType.Combat || type == NodeType.Elite || type == NodeType.Boss ? ClearMode.KillAll : ClearMode.Immediate;

        bool cleared = mode == ClearMode.Immediate
                       || (mode == ClearMode.KillAll && _enemiesAlive <= 0)
                       || (mode == ClearMode.OpenAllChests && _chestsClosed <= 0);
        if (!cleared) return;

        IsCleared = true;
        Spawn(_context, AreaSpawnPoint.SpawnTiming.OnClear); // reward chests of Elite / Boss nodes
        if (exits != null)
        {
            foreach (var exit in exits)
            {
                if (exit != null && exit.gameObject.activeSelf) exit.SetLocked(false);
            }
        }

        Cleared?.Invoke(this);
        onCleared?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(EntryPosition + Vector3.up * 0.3f, 0.6f);
    }
}