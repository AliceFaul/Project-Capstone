using System;
using System.Collections.Generic;
using UnityEngine;

/// What a chest / enemy can drop. Each "roll" picks ONE entry by weight (or nothing); an entry can give several copies.
[CreateAssetMenu(fileName = "New Loot Table", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemData item;
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int minCount = 1;
        [Min(1)] public int maxCount = 1;
    }
    
    [Header("Rolls")]
    [Min(0)] public int minRolls = 1;
    [Min(0)] public int maxRolls = 1;
    
    [Tooltip("Weight of 'nothing drops' in every roll, compared with the entry weights.")]
    [Min(0f)] public float nothingWeight = 0f;
    
    [Header("Difficulty")]
    [Tooltip("Index = Rarity (Common, Uncommon, Rare, Legendary). Entry weight is multiplied by (1 + difficulty x value).")]
    public float[] rarityBoostPerDifficulty = { -0.10f, 0.15f, 0.40f, 0.80f };
    
    [Header("Currency")]
    public Vector2Int gold = Vector2Int.zero;
    public Vector2Int gem = Vector2Int.zero;
    
    [Header("Entries")]
    public List<Entry> entries = new();

    public Entry PickEntry(int difficulty, System.Random rng)
    {
        float total = nothingWeight;
        foreach(var entry in entries) total += EffectiveWeight(entry, difficulty);
        if(total <= 0f) return null;
        
        double roll = rng.NextDouble() * total;
        if(roll < nothingWeight) return null;
        roll -= nothingWeight;

        foreach (var entry in entries)
        {
            float weight = EffectiveWeight(entry, difficulty);
            if(roll < weight) return entry;
            roll -= weight;
        }
        return null;
    }

    private float EffectiveWeight(Entry entry, int difficulty)
    {
        if(entry == null || entry.item == null || entry.weight <= 0f) return 0f;
        int index = (int)entry.item.rarity;
        
        float boost = rarityBoostPerDifficulty != null && index < rarityBoostPerDifficulty.Length
            ? rarityBoostPerDifficulty[index]
            : 0f;

        return Mathf.Max(0f, entry.weight * (1f + Mathf.Max(0, difficulty) * boost));
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if(maxRolls < minRolls) maxRolls = minRolls;
        if(gold.y < gold.x) gold.y = gold.x;
        if(gem.y < gem.x) gem.y = gem.x;
        foreach (var entry in entries)
        {
            if(entry != null && entry.maxCount < entry.minCount) entry.maxCount = entry.minCount;
        }
    }
#endif
}