#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Editor-only placement logic. Positions in a plan are WORLD positions.
// A plan is completed before any scene objects are created.
internal static class AreaBuilderLayout
{
    internal sealed class Options
    {
        public Terrain Terrain;
        public float Radius = 14f;
        public float MinDistance = 2.8f;
        public float MaxSlope = 25f;
        public float HeightOffset = 0.05f;
        public bool RequireNavMesh = true;
        public bool SnapToNavMesh = true;
        public bool CheckPaths = true;
        public int AgentTypeId;
        public int AreaMask = NavMesh.AllAreas;
        public float SampleDistance = 1.5f;
        public float MaxHeightDifference = 0.6f;
        public bool Randomize = true;
        public bool RandomEntryAngle;
        public bool EntryAtEdge = true;
        public float EntryAngle;
        public float ExitAngleOffset = 180f;
        public float ExitSpread = 70f;
        public int EnemyCount = 4;
        public int ChestCount;
        public int PropCount;
        public int ExitCount = 2;
        public bool Boundary = true;
        public int BoundarySegments = 24;
        public float BoundaryThickness = 2f;
        public float Gap = 6f;
        public int Attempts = 60;

        public NavMeshQueryFilter Filter => new NavMeshQueryFilter
        {
            agentTypeID = AgentTypeId,
            areaMask = AreaMask
        };

        public float Footprint => Boundary
            ? BoundaryFootprint(Radius, BoundarySegments, BoundaryThickness)
            : Radius;
    }

    internal struct OccupiedArea
    {
        public Vector3 Center;
        public float Radius;
        public string Name;

        public OccupiedArea(Vector3 center, float radius, string name)
        {
            Center = center;
            Radius = radius;
            Name = name;
        }
    }

    internal sealed class Spawn
    {
        public AreaSpawnPoint.Kind Kind;
        public Vector3 Position;
    }

    internal sealed class Plan
    {
        public string Id;
        public Vector3 Center;
        public Vector3 Entry;
        public readonly List<Vector3> Exits = new List<Vector3>();
        public readonly List<Spawn> Spawns = new List<Spawn>();
    }

    // Kept inside the nominal area, ahead of the external carving ring.
    internal static float PlayableRadius(float radius) => Mathf.Max(0.1f, radius - 0.35f);

    internal static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float z = a.z - b.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    internal static Vector3 Polar(float angle, float distance)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians) * distance, 0f, Mathf.Cos(radians) * distance);
    }

    internal static float BoundaryRadius(float radius, float thickness)
        => radius + 0.5f + thickness * 0.5f;

    internal static float BoundaryLength(float radius, int segments, float thickness)
        => 2f * BoundaryRadius(radius, thickness) * Mathf.Tan(Mathf.PI / Mathf.Max(6, segments)) + 0.25f;

    internal static float BoundaryFootprint(float radius, int segments, float thickness)
    {
        float outer = BoundaryRadius(radius, thickness) + thickness * 0.5f;
        float halfLength = BoundaryLength(radius, segments, thickness) * 0.5f;
        return Mathf.Sqrt(outer * outer + halfLength * halfLength);
    }

    internal static bool OnTerrain(Terrain terrain, Vector3 point, float margin = 0f)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return point.x >= origin.x + margin && point.z >= origin.z + margin
            && point.x <= origin.x + size.x - margin
            && point.z <= origin.z + size.z - margin;
    }

    internal static Vector3 Ground(Terrain terrain, Vector3 point)
    {
        point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
        return point;
    }

    internal static bool CheckCenter(Options options, Vector3 center,
        IList<OccupiedArea> occupied, out string reason)
    {
        if (!OnTerrain(options.Terrain, center, options.Footprint + 0.1f))
        {
            reason = "Area/boundary vuot khoi mep Terrain.";
            return false;
        }
        foreach (var other in occupied)
        {
            if (DistanceXZ(center, other.Center) < options.Footprint + other.Radius + options.Gap)
            {
                reason = "Area/boundary qua gan '" + other.Name + "'.";
                return false;
            }
        }
        reason = null;
        return true;
    }

    // A successful sample alone does not imply reachability. CheckPath handles that separately.
    internal static bool TryPoint(Options options, Vector3 center, Vector3 candidate,
        bool canSnap, out Vector3 result, out string reason)
    {
        result = candidate;
        if (!CheckGround(options, center, candidate, out reason)) return false;
        Vector3 ground = Ground(options.Terrain, candidate);
        result = ground;

        if (options.RequireNavMesh)
        {
            if (!NavMesh.SamplePosition(ground, out var hit, options.SampleDistance, options.Filter))
            {
                reason = "Khong co NavMesh gan diem nay (kiem tra Agent Type / bake).";
                return false;
            }
            if (Mathf.Abs(hit.position.y - ground.y) > options.MaxHeightDifference)
            {
                reason = "NavMesh khac cao do Terrain; co the sample nham tang.";
                return false;
            }
            if ((!canSnap || !options.SnapToNavMesh) && DistanceXZ(ground, hit.position) > 0.15f)
            {
                reason = "Diem nam lech NavMesh; hay di chuyen diem hoac bat Snap khi tao.";
                return false;
            }
            result = canSnap && options.SnapToNavMesh ? hit.position
                : new Vector3(ground.x, hit.position.y, ground.z);
            if (!CheckGround(options, center, result, out reason)) return false;
        }
        result.y += options.HeightOffset;
        reason = null;
        return true;
    }

    private static bool CheckGround(Options options, Vector3 center, Vector3 point, out string reason)
    {
        if (!OnTerrain(options.Terrain, point))
        {
            reason = "Diem nam ngoai Terrain.";
            return false;
        }
        if (DistanceXZ(center, point) > PlayableRadius(options.Radius))
        {
            reason = "Diem nam ngoai vung choi cua area.";
            return false;
        }
        Vector3 origin = options.Terrain.transform.position;
        Vector3 size = options.Terrain.terrainData.size;
        float slope = options.Terrain.terrainData.GetSteepness(
            (point.x - origin.x) / size.x, (point.z - origin.z) / size.z);
        if (slope > options.MaxSlope)
        {
            reason = "Do doc " + slope.ToString("F1") + " vuot Max Slope.";
            return false;
        }
        reason = null;
        return true;
    }

    internal static bool CheckPath(Options options, Vector3 center, Vector3 entry,
        Vector3 destination, NavMeshPath path, out string reason)
    {
        reason = null;
        if (!options.RequireNavMesh || !options.CheckPaths) return true;
        Vector3 start = entry - Vector3.up * options.HeightOffset;
        Vector3 end = destination - Vector3.up * options.HeightOffset;
        if (!NavMesh.CalculatePath(start, end, options.Filter, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            reason = "Khong co duong di hoan chinh tu Entry.";
            return false;
        }
        // A complete route that leaves the arena would be cut by the runtime boundary.
        foreach (var corner in path.corners)
        {
            if (DistanceXZ(center, corner) <= PlayableRadius(options.Radius)) continue;
            reason = "Duong di phai ra ngoai area; boundary se chan duong nay.";
            return false;
        }
        return true;
    }

    internal static bool WellSpaced(Vector3 point, IList<Vector3> occupied, float minDistance)
    {
        foreach (var other in occupied)
            if (DistanceXZ(point, other) < minDistance) return false;
        return true;
    }

    internal static bool TryPlan(Options options, Vector3 center, System.Random random,
        out Plan plan, out string reason)
    {
        plan = null;
        reason = "Khong tim duoc bo diem hop le.";
        center = Ground(options.Terrain, center);
        var draft = new Plan { Center = center };
        var used = new List<Vector3>();
        var path = new NavMeshPath();
        float entryAngle = options.EntryAngle;
        if (options.Randomize && options.RandomEntryAngle)
            entryAngle = (float)random.NextDouble() * 360f;

        Vector3 entryCandidate = options.EntryAtEdge
            ? center + Polar(entryAngle, options.Radius * 0.78f) : center;
        if (!TryPoint(options, center, entryCandidate, true, out draft.Entry, out reason))
        {
            reason = "Entry: " + reason;
            return false;
        }
        used.Add(draft.Entry);

        // Reserve exits BEFORE spawning enemies/props, so the exit approach stays clear.
        float spread = options.ExitCount > 1 ? options.ExitSpread / (options.ExitCount - 1) : 0f;
        for (int i = 0; i < options.ExitCount; i++)
        {
            float angle = entryAngle + options.ExitAngleOffset + spread * (i - (options.ExitCount - 1) * 0.5f);
            Vector3 candidate = center + Polar(angle, options.Radius * 0.85f);
            if (!TryPoint(options, center, candidate, true, out var point, out reason)
                || !CheckPath(options, center, draft.Entry, point, path, out reason))
            {
                reason = "Exit " + i + ": " + reason;
                return false;
            }
            if (!WellSpaced(point, used, options.MinDistance))
            {
                reason = "Exit " + i + " qua gan Entry/Exit khac sau khi snap.";
                return false;
            }
            used.Add(point);
            draft.Exits.Add(point);
        }

        int total = options.EnemyCount + options.ChestCount + options.PropCount;
        // The deterministic sequence scans a grid, instead of calling Random with the toggle off.
        List<Vector3> fixedCandidates = options.Randomize ? null : FixedCandidates(options.Radius * 0.72f,
            Mathf.Max(0.5f, options.MinDistance + 0.2f), entryAngle);
        int fixedIndex = 0;
        for (int i = 0; i < total; i++)
        {
            bool found = false;
            Vector3 final = default;
            int attempts = options.Randomize ? options.Attempts : fixedCandidates.Count;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector3 offset;
                if (options.Randomize)
                {
                    float angle = (float)random.NextDouble() * 360f;
                    float distance = Mathf.Sqrt((float)random.NextDouble()) * options.Radius * 0.72f;
                    offset = Polar(angle, distance);
                }
                else
                {
                    if (fixedIndex >= fixedCandidates.Count) break;
                    offset = fixedCandidates[fixedIndex++];
                }
                if (!TryPoint(options, center, center + offset, true, out final, out reason)) continue;
                if (!WellSpaced(final, used, options.MinDistance))
                {
                    reason = "Khong du khoang cach giua cac diem sau snap.";
                    continue;
                }
                if (!CheckPath(options, center, draft.Entry, final, path, out reason)) continue;
                found = true;
                break;
            }
            if (!found)
            {
                reason = "Spawn " + (i + 1) + ": " + reason + " Giam so diem/khoang cach hoac tang radius.";
                return false;
            }
            var kind = i < options.EnemyCount ? AreaSpawnPoint.Kind.Enemy
                : i < options.EnemyCount + options.ChestCount ? AreaSpawnPoint.Kind.Chest : AreaSpawnPoint.Kind.Prop;
            used.Add(final);
            draft.Spawns.Add(new Spawn { Kind = kind, Position = final });
        }
        plan = draft;
        reason = null;
        return true;
    }

    private static List<Vector3> FixedCandidates(float radius, float step, float angle)
    {
        var result = new List<Vector3>();
        int count = Mathf.FloorToInt(radius / step);
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        for (int z = -count; z <= count; z++)
        for (int x = -count; x <= count; x++)
        {
            var candidate = new Vector3(x * step, 0f, z * step);
            if (candidate.sqrMagnitude <= radius * radius) result.Add(rotation * candidate);
        }
        return result;
    }
}
#endif
