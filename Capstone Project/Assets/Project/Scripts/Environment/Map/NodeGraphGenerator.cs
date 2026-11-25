using System.Collections.Generic;
using UnityEngine;

/// Builds a layered node graph from a seed and the areas available in the scene.
public static class NodeGraphGenerator
{
    public static NodeGraph Generate(NodeMapConfig config, Dictionary<NodeType, List<string>> areasByType, int seed)
        => Generate(config, areasByType, seed, null);

    public static NodeGraph Generate(NodeMapConfig config, Dictionary<NodeType, List<string>> areasByType,
        int seed, IReadOnlyDictionary<string, int> exitsByArea)
    {
        var rng = new System.Random(seed);
        int layers = Mathf.Max(3, config.layerCount);
        int maxLinks = Mathf.Max(1, config.maxConnectionsPerNode);
        var graph = new NodeGraph { Seed = seed, LayerCount = layers };
        var counts = new Dictionary<NodeType, int>();
        var used = new HashSet<string>();
        var pools = areasByType;
        if (!config.allowAreaReuse)
        {
            pools = new Dictionary<NodeType, List<string>>();
            foreach (var pair in areasByType) pools.Add(pair.Key, new List<string>(pair.Value));
        }
        List<MapNode> previous = null;
        int[] priorLimits = null;
        int totalExits = 0;

        for (int layer = 0; layer < layers; layer++)
        {
            bool first = layer == 0;
            bool last = layer == layers - 1;
            bool beforeBoss = layer == layers - 2 && config.restBeforeBoss;
            int width = 1;
            if (!first && !last)
            {
                int max = Mathf.Min(config.maxWidth, totalExits);
                if (beforeBoss) max = Mathf.Min(max, 2);
                // Keep one node for validation if the previous layer has no working exit.
                max = Mathf.Max(1, max);
                width = rng.Next(Mathf.Clamp(config.minWidth, 1, max), max + 1);
            }

            var current = new List<MapNode>(width);
            var limits = new int[width];
            totalExits = 0;
            for (int slot = 0; slot < width; slot++)
            {
                NodeType type;
                if (first) type = config.startType == NodeType.Boss ? NodeType.Combat : config.startType;
                else if (last) type = NodeType.Boss;
                else
                {
                    type = beforeBoss && Has(pools, NodeType.Rest)
                        ? NodeType.Rest : PickType(config, layer, counts, pools, rng);
                    counts.TryGetValue(type, out int count);
                    counts[type] = count + 1;
                }

                string areaId = PickAreas(pools, type, used, config.allowAreaReuse, rng);
                if (!config.allowAreaReuse && areaId != null) pools[type].Remove(areaId);
                var node = new MapNode { Id = graph.Nodes.Count, Layer = layer, Slot = slot, Type = type, AreaId = areaId };
                graph.Nodes.Add(node);
                current.Add(node);

                int capacity = maxLinks;
                if (exitsByArea != null)
                    capacity = areaId != null && exitsByArea.TryGetValue(areaId, out int exits)
                        ? Mathf.Clamp(exits, 0, maxLinks) : 0;
                limits[slot] = capacity;
                totalExits += capacity;
            }

            if (previous != null) ConnectLayers(previous, current, priorLimits, rng);
            previous = current;
            priorLimits = limits;
        }
        return graph;
    }

    private static void ConnectLayers(List<MapNode> from, List<MapNode> to, int[] limits, System.Random rng)
    {
        // Reserve one way forward for each parent before spending any exits on extra routes.
        for (int i = 0; i < from.Count; i++)
        {
            if (limits[i] == 0) continue;
            int target = Mathf.RoundToInt(SlotX(i, from.Count) * (to.Count - 1));
            Link(from[i], to[target]);
        }

        foreach (var child in to)
        {
            if (child.Previous.Count != 0) continue;
            int best = -1;
            float nearest = float.PositiveInfinity;
            float x = SlotX(child.Slot, to.Count);
            for (int i = 0; i < from.Count; i++)
            {
                if (from[i].Next.Count >= limits[i]) continue;
                float distance = Mathf.Abs(SlotX(i, from.Count) - x);
                if (distance >= nearest) continue;
                nearest = distance;
                best = i;
            }
            if (best >= 0) Link(from[best], child);
        }

        foreach (var parent in from)
        {
            int limit = limits[parent.Slot];
            if (parent.Next.Count >= limit) continue;
            float x = SlotX(parent.Slot, from.Count);
            var ordered = new List<MapNode>(to);
            ordered.Sort((a, b) =>
            {
                float da = Mathf.Abs(SlotX(a.Slot, to.Count) - x);
                float db = Mathf.Abs(SlotX(b.Slot, to.Count) - x);
                int compare = da.CompareTo(db);
                return compare != 0 ? compare : a.Slot.CompareTo(b.Slot);
            });
            foreach (var child in ordered)
            {
                if (parent.Next.Count >= limit) break;
                if (!parent.Next.Contains(child.Id) && rng.NextDouble() < 0.5) Link(parent, child);
            }
        }
    }

    public static List<string> Validate(NodeGraph graph)
    {
        var problems = new List<string>();
        foreach (var node in graph.Nodes)
        {
            if (string.IsNullOrEmpty(node.AreaId))
                problems.Add($"Node {node.Id} ({node.Type}, layer {node.Layer}) has no area. Check the area pool and Allow Area Reuse.");
            if (node != graph.Start && node.Previous.Count == 0)
                problems.Add($"Node {node.Id} ({node.AreaId}) has no incoming path.");
            if (node != graph.Boss && node.Next.Count == 0)
                problems.Add($"Node {node.Id} ({node.AreaId}) has no usable exit to the next layer.");
        }
        return problems;
    }

    private static NodeType PickType(NodeMapConfig config, int layer, Dictionary<NodeType, int> counts, Dictionary<NodeType, List<string>> areas, System.Random rng)
    {
        float total = 0f;
        foreach(var rule in config.rules) total += Weight(rule, layer, counts, areas);
        if(total <= 0f) return NodeType.Combat;
        
        double roll = rng.NextDouble() * total;
        foreach (var rule in config.rules)
        {
            float weight = Weight(rule, layer, counts, areas);
            if(roll < weight) return rule.type;
            roll -= weight;
        }

        return NodeType.Combat;
    }
    
    private static float Weight(NodeMapConfig.TypeRule rule, int layer, Dictionary<NodeType, int> counts, Dictionary<NodeType, List<string>> areas)
    {
        if(rule.type == NodeType.Boss || rule.weight <= 0f) return 0f;
        if(layer < rule.minLayer || layer > rule.maxLayer) return 0f;

        counts.TryGetValue(rule.type, out int count);
        if(count >= rule.maxPerRun) return 0f;
        return Has(areas, rule.type) ? rule.weight : 0f;
    }
    
    private static bool Has(Dictionary<NodeType, List<string>> areas, NodeType type) =>
        areas.TryGetValue(type, out var list) && list.Count > 0;
    
    private static string PickAreas(Dictionary<NodeType, List<string>> areas, NodeType type, HashSet<string> used, bool allowReuse, System.Random rng)
    {
        if(!areas.TryGetValue(type, out var all) || all.Count == 0) return null;

        var free = all.FindAll(id => !used.Contains(id));
        if (free.Count == 0)
        {
            if(!allowReuse) return null;
            free = all;
        }
        
        string pick = free[rng.Next(free.Count)];
        used.Add(pick);
        return pick;
    }
    
    private static float SlotX(int slot, int count) => count <= 1 ? 0.5f : slot / (float)(count - 1);
    
    private static void Link(MapNode from, MapNode to)
    {
        if(from.Next.Contains(to.Id)) return;
        from.Next.Add(to.Id);
        to.Previous.Add(from.Id);
    }
}