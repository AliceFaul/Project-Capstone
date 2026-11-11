using UnityEngine;
using System;
using Project.Capstone.Inventory;

public static class LootRoller
{
    public static string NewId() => Guid.NewGuid().ToString("N");
 
    public static EquipmentInstance RollEquipment(EquipmentData def, EquipmentProgressConfig config, System.Random rng = null)
    {
        if (def == null) return null;
 
        if (!TryGetCategory(def.equipmentType, out var category))
        {
            Debug.LogError($"[LootRoller] {def.itemName}: equipmentType {def.equipmentType} khong thuoc Melee/Ranged/Armor.");
            return null;
        }
 
        rng ??= new System.Random();
 
        var instance = new EquipmentInstance
        {
            instanceId = NewId(),
            definitionId = def.id,
            category = category,
            level = 1,
            exp = 0f
        };
 
        foreach (var table in def.stats)
        {
            if (!table.Tiered) continue;
            instance.rolls.Add(new StatRoll
            {
                stat = table.Stat,
                tier = PickTier(config != null ? config.tierWeights : null, StatTierTable.TierCount, rng)
            });
        }
 
        instance.EnsureSockets();
        return instance;
    }
 
    public static ArtifactInstance CreateArtifact(string definitionId)
    {
        return new ArtifactInstance { instanceId = NewId(), definitionId = definitionId };
    }
 
    // Chon tier theo trong so (mac dinh deu nhau neu thieu/khong hop le).
    public static int PickTier(float[] weights, int tierCount, System.Random rng)
    {
        float total = 0f;
        for (int i = 0; i < tierCount; i++) total += WeightAt(weights, i);
 
        if (total <= 0f) return rng.Next(tierCount);
 
        double roll = rng.NextDouble() * total;
        float cumulative = 0f;
        for (int i = 0; i < tierCount; i++)
        {
            cumulative += WeightAt(weights, i);
            if (roll < cumulative) return i;
        }
        return tierCount - 1;
    }
 
    private static float WeightAt(float[] weights, int index)
    {
        if (weights == null || index >= weights.Length) return 1f;
        return Mathf.Max(0f, weights[index]);
    }
 
    public static bool TryGetCategory(EquipmentType type, out InventoryCategory category)
    {
        switch (type)
        {
            case EquipmentType.MeleeWeapon: category = InventoryCategory.Melee; return true;
            case EquipmentType.RangedWeapon: category = InventoryCategory.Ranged; return true;
            case EquipmentType.Armor: category = InventoryCategory.Armor; return true;
            default: category = default; return false;
        }
    }
}