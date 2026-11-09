using System.Collections.Generic;
using System.Text;

public enum NodeType { Combat, Reward, Event, Rest, Elite, Boss, Shop }

/// One node of the run map. Pure data.
public class MapNode
{
    public int Id;
    public int Layer;
    public int Slot;
    public NodeType Type;
    public string AreaId;
    public readonly List<int> Next = new();
    public readonly List<int> Previous = new();
    public bool Visited;
    public bool Cleared;
}

public class NodeGraph
{
    public int Seed;
    public int LayerCount;
    public readonly List<MapNode> Nodes = new();
    
    public MapNode Start => Nodes.Count > 0 ? Nodes[0] : null;
    public MapNode Boss => Nodes.Count > 0 ? Nodes[^1] : null;
    public MapNode Get(int id) => Nodes[id];
    public List<MapNode> GetLayer(int layer) => Nodes.FindAll(n => n.Layer == layer);

    public string ToAscii()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"NodeGraph seed {Seed}, {LayerCount} layers, {Nodes.Count} nodes");
        for (int l = 0; l < LayerCount; l++)
        {
            sb.Append($"L{l}: ");
            foreach (var node in GetLayer(l))
            {
                sb.Append($"[{node.Id} {node.Type}");
                if(!string.IsNullOrEmpty(node.AreaId)) sb.Append($" '{node.AreaId}'");
                if(node.Next.Count > 0) sb.Append(" -> " + string.Join(",", node.Next));
                sb.Append("]  ");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}