using System;
using UnityEngine;

// Prices by rarity. A buy price is rolled inside the rarity's min...max range; a better quality item (rune roll, equipment tiers)
// leans toward the top of the range. Selling pays a share of what the item is worth.
[CreateAssetMenu(fileName = "ShopPriceConfig", menuName = "Shop/Shop Price Config")]
public class ShopPriceConfig : ScriptableObject
{
    [Serializable]
    public struct PriceRange
    {
        public int min;
        public int max;
        public PriceRange(int min, int max) { this.min = min; this.max = max; }
    }

    [Header("Buy price range (gold) by rarity")]
    public PriceRange common = new PriceRange(40, 70);
    public PriceRange uncommon = new PriceRange(90, 150);
    public PriceRange rare = new PriceRange(220, 350);
    public PriceRange legendary = new PriceRange(600, 900);

    [Header("Roll")]
    [Tooltip("0 = fully random inside the range, 1 = decided only by the item's quality.")]
    [Range(0f, 1f)] public float qualityInfluence = 0.6f;
    [Min(1)] public int roundTo = 5;

    [Header("Selling")]
    [Tooltip("Share of the item's value paid when the player sells it.")]
    [Range(0f, 1f)] public float sellRatio = 0.4f;
    [Tooltip("Extra value per equipment level above 1 (0.1 = +10% per level).")]
    [Min(0f)] public float sellBonusPerEquipmentLevel = 0.1f;

    public PriceRange GetRange(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: return uncommon;
            case Rarity.Rare: return rare;
            case Rarity.Legendary: return legendary;
            default: return common;
        }
    }

    public int RollBuyPrice(Rarity rarity, float quality, System.Random rng)
    {
        var range = GetRange(rarity);
        float t = Mathf.Lerp((float)rng.NextDouble(), Mathf.Clamp01(quality), qualityInfluence);
        return Round(Mathf.Lerp(range.min, range.max, t));
    }

    // Deterministic, so the sell price shown in the UI is always what the player gets.
    public int GetSellPrice(Rarity rarity, float quality, int equipmentLevel)
    {
        var range = GetRange(rarity);
        float value = Mathf.Lerp(range.min, range.max, Mathf.Clamp01(quality));
        value *= sellRatio * (1f + sellBonusPerEquipmentLevel * Mathf.Max(0, equipmentLevel - 1));
        return Mathf.Max(1, Round(value));
    }

    private int Round(float value)
    {
        int step = Mathf.Max(1, roundTo);
        return Mathf.Max(1, Mathf.RoundToInt(value / step) * step);
    }
}