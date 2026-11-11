using System;
using System.Collections.Generic;
using UnityEngine;

// What a merchant can offer. Every visit picks minItems...maxItems DIFFERENT entries by weight.
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Shop/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemData item;
        [Min(0f)] public float weight = 1f;
    }

    [Range(1, 8)] public int minItems = 3;
    [Range(1, 8)] public int maxItems = 5;

    [Tooltip("Index = Rarity (Common, Uncommon, Rare, Legendary). Weight is multiplied by (1 + difficulty x value).")]
    public float[] rarityBoostPerDifficulty = { -0.10f, 0.15f, 0.40f, 0.80f };

    public List<Entry> entries = new List<Entry>();

    public List<ItemData> PickItems(int difficulty, System.Random rng)
    {
        int count = rng.Next(minItems, Mathf.Max(minItems, maxItems) + 1);
        var pool = entries.FindAll(e => e != null && e.item != null && e.weight > 0f);
        var result = new List<ItemData>();

        while (result.Count < count && pool.Count > 0)
        {
            float total = 0f;
            foreach (var entry in pool) total += EffectiveWeight(entry, difficulty);
            if (total <= 0f) break;

            double roll = rng.NextDouble() * total;
            Entry picked = pool[^1];
            foreach (var entry in pool)
            {
                float weight = EffectiveWeight(entry, difficulty);
                if (roll < weight) { picked = entry; break; }
                roll -= weight;
            }

            result.Add(picked.item);
            pool.Remove(picked);
        }
        return result;
    }

    private float EffectiveWeight(Entry entry, int difficulty)
    {
        int index = (int)entry.item.rarity;
        float boost = rarityBoostPerDifficulty != null && index < rarityBoostPerDifficulty.Length ? rarityBoostPerDifficulty[index] : 0f;
        return Mathf.Max(0f, entry.weight * (1f + Mathf.Max(0, difficulty) * boost));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxItems < minItems) maxItems = minItems;
    }
#endif
}