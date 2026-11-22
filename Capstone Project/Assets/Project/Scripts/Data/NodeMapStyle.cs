using System;
using System.Collections.Generic;
using UnityEngine;

/// Look of each node type on the route map: color, optional icon sprite and a fallback glyph (used when there is no sprite yet).
[CreateAssetMenu(fileName = "NodeMapStyle", menuName = "Map/Node Map Style")]
public class NodeMapStyle : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public NodeType type;
        public Color color = Color.white;
        public Sprite icon;
        public string glyph = "?";
    }

    public List<Entry> entries = new List<Entry>
    {
        new Entry { type = NodeType.Combat, color = new Color32(0xE0, 0x60, 0x3A, 255), glyph = "X" },
        new Entry { type = NodeType.Elite, color = new Color32(0xB2, 0x65, 0xF0, 255), glyph = "E" },
        new Entry { type = NodeType.Reward, color = new Color32(0xFF, 0xD2, 0x7A, 255), glyph = "+" },
        new Entry { type = NodeType.Event, color = new Color32(0x5A, 0xB8, 0xF0, 255), glyph = "?" },
        new Entry { type = NodeType.Rest, color = new Color32(0x6F, 0xD1, 0x8A, 255), glyph = "Z" },
        new Entry { type = NodeType.Shop, color = new Color32(0x3F, 0xD0, 0xC0, 255), glyph = "$" },
        new Entry { type = NodeType.Boss, color = new Color32(0xE0, 0x3A, 0x3A, 255), glyph = "B" },
    };

    public Entry Get(NodeType type)
    {
        foreach (var entry in entries)
        {
            if (entry.type == type) return entry;
        }
        return entries[0];
    }
}