using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeMapConfig", menuName = "Map/Node Map Config")]
public class NodeMapConfig : ScriptableObject
{
    [Serializable]
    public class TypeRule
    {
        public NodeType type;
        [Min(0f)] public float weight = 1f;

        [Tooltip("First layer where this type may appear.")]
        public int minLayer = 1;
        public int maxLayer = 99;
        public int maxPerRun = 99;
    }
    
    [Header("Shape")]
    [Tooltip("Total layers including the start and the boss layer.")]
    [Min(3)] public int layerCount = 7;
    [Range(1, 4)] public int minWidth = 2;
    [Range(1, 4)] public int maxWidth = 3;
    [Range(1, 3)] public int maxConnectionsPerNode = 2;

    [Header("Node types for the middle layers")]
    public NodeType startType = NodeType.Combat;

    [Tooltip("The layer right before the boss is made of Rest nodes")]
    public bool restBeforeBoss = true;

    public List<TypeRule> rules = new List<TypeRule>
    {
        new TypeRule { type = NodeType.Combat, weight = 5f, minLayer = 1 },
        new TypeRule { type = NodeType.Reward, weight = 2f, minLayer = 1 },
        new TypeRule { type = NodeType.Event, weight = 1.5f, minLayer = 1 },
        new TypeRule { type = NodeType.Rest, weight = 1.5f, minLayer = 2 },
        new TypeRule { type = NodeType.Elite, weight = 1.5f, minLayer = 2, maxPerRun = 2 },
        new TypeRule { type = NodeType.Shop, weight = 1f, minLayer = 2, maxPerRun = 1 },
    };

    [Header("Areas")]
    [Tooltip("If the scene has fewer areas of a type than the run needs, the same area may be used again.")]
    public bool allowAreaReuse = true;
    
    [Header("Enemy Scaling")]
    public float healthPerCombatNode = 0.2f;
    public float damagePerCombatNode = 0.1f;
    public float eliteHealthMultiplier = 1.6f;
    public float eliteDamageMultiplier = 1.25f;
    
    [Header("Transition")]
    [Min(0f)] public float fadeDuration = 0.35f;
}