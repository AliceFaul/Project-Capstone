using System.Collections.Generic;
using UnityEngine;

/// Builds a random layered node graph from a seed (same seed + same areas = same map).
public static class NodeGraphGenerator
{
    public static NodeGraph Generate(NodeMapConfig config, Dictionary<NodeType, List<string>> areasByType, int seed)
    {
        var rng = new System.Random(seed);
        int layers = Mathf.Max(3, config.layerCount);
        var graph = new NodeGraph { Seed = seed, LayerCount = layers };
        var byLayer = new List<MapNode>[layers];

        // 1. shape: how many nodes per layer
        for (int l = 0; l < layers; l++)
        {
            int width = 1;
            if (l > 0 && l < layers - 1)
            {
                int max = config.maxWidth;
                if(l == layers - 2 && config.restBeforeBoss) max = Mathf.Min(max, 2);
                int min = Mathf.Min(config.minWidth, max);
                width = rng.Next(min, max + 1);
            }

            byLayer[l] = new List<MapNode>();
            for (int s = 0; s < width; s++)
            {
                var node = new MapNode { Id = graph.Nodes.Count, Layer = l, Slot = s };
                graph.Nodes.Add(node);
                byLayer[l].Add(node);
            }
        }
        
        // 2. types
        var counts = new Dictionary<NodeType, int>();
        graph.Start.Type = config.startType == NodeType.Boss ? NodeType.Combat : config.startType;
        graph.Boss.Type = NodeType.Boss;

        for (int l = 1; l < layers - 1; l++)
        {
            foreach (var node in byLayer[l])
            {
                bool restLayer = 1 == layers - 2 && config.restBeforeBoss && Has(areasByType, NodeType.Rest);
                node.Type = restLayer ? NodeType.Rest : PickType(config, 1, counts, areasByType, rng);
                counts.TryGetValue(node.Type, out int c);
                counts[node.Type] = c + 1;
            }
        }
        
        // 3. connections
        for (int l = 0; l < layers - 1; l++)
        {
            var current = byLayer[l];
            var next = byLayer[l + 1];

            foreach (var node in current)
            {
                float x = SlotX(node.Slot, current.Count);
                var ordered = new List<MapNode>(next);
                ordered.Sort((a, b) => Mathf.Abs(SlotX(a.Slot, next.Count) - x).CompareTo(Mathf.Abs(SlotX(b.Slot, next.Count) - x)));
                
                Link(node, ordered[0]);
                int extra = Mathf.Min(config.maxConnectionsPerNode - 1, ordered.Count - 1);

                for (int k = 1; k <= extra; k++)
                {
                    if(rng.NextDouble() < 0.5) Link(node, ordered[k]);
                }
            }
            
            // every node of the next layer must be reachable
            foreach (var target in next)
            {
                if(target.Previous.Count > 0) continue;
                
                float x = SlotX(target.Slot, next.Count);
                MapNode nearest = current[0];
                foreach (var node in current)
                {
                    if(Mathf.Abs(SlotX(node.Slot, current.Count) - x) < Mathf.Abs(SlotX(nearest.Slot, current.Count) - x)) nearest = node;
                }
                Link(nearest, target);
            }
        }
        
        // 4. areas
        var used = new HashSet<string>();
        foreach(var node in graph.Nodes) node.AreaId = PickAreas(areasByType, node.Type, used, config.allowAreaReuse, rng);
        
        return graph;
    }
    
    public static List<string> Validate(NodeGraph graph)
    {
        var problems = new List<string>();
        foreach (var node in graph.Nodes)
        {
            if(string.IsNullOrEmpty(node.AreaId)) 
                problems.Add($"Node {node.Id} ({node.Type}, layer {node.Layer}) has no area - add a {node.Type} area to the scene.");
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