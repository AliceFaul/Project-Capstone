using UnityEngine;

/// Marker inside an area: where an enemy / chest / prop is spawned when the area is entered.
public class AreaSpawnPoint : MonoBehaviour
{
    public enum Kind { Enemy, Chest, Prop }
    public enum SpawnTiming { OnEnter, OnClear }

    [SerializeField] private Kind kind = Kind.Enemy;
    [Tooltip("One of these is picked at random.")]
    [SerializeField] private GameObject[] prefabs;
    [Range(0f, 1f)] [SerializeField] private float chance = 1f;
    [Tooltip("OnClear: spawned when the area is cleared (e.g. the reward chest of an Elite or Boss node).")]
    [SerializeField] private SpawnTiming timing = SpawnTiming.OnEnter;
    [Tooltip("Node types this point is used for. Empty = all types. (Enemy points: Combat, Elite, Boss. Chests: Reward.)")]
    [SerializeField] private NodeType[] activeFor = { NodeType.Combat, NodeType.Elite, NodeType.Boss };

    public Kind SpawnKind => kind;
    public SpawnTiming Timing => timing;

    public bool IsActiveFor(NodeType type)
    {
        if (activeFor == null || activeFor.Length == 0) return true;
        foreach (var t in activeFor)
        {
            if (t == type) return true;
        }
        return false;
    }

    // null = nothing spawns this time (chance failed / no prefab).
    public GameObject Pick(System.Random rng)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        if (rng.NextDouble() > chance) return null;
        return prefabs[rng.Next(prefabs.Length)];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = kind == Kind.Enemy ? Color.red : kind == Kind.Chest ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, 0.4f);
    }
}