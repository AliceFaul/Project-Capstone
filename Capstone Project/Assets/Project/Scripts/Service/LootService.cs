using System.Collections.Generic;
using Project.Capstone.Inventory;
using UnityEngine;
using UnityEngine.AI;

public struct LootDrop
{
    public ItemData Definition;
    public IInventoryItem Item;
}

/// Rolls loot tables into real item instances, grants currency and spawns the pickups.
public static class LootService
{
    // Set by the level / difficulty system. 0 = base drop rates.
    public static int Difficulty { get; set; }

    public static List<LootDrop> Roll(LootTable table, int difficulty, System.Random rng = null)
    {
        var drops = new List<LootDrop>();
        if(table == null) return drops;

        rng ??= new System.Random();
        var config = EquipmentManager.Instance != null ? EquipmentManager.Instance.ProgressConfig : null;

        int rolls = rng.Next(table.minRolls, table.maxRolls + 1);
        for (int r = 0; r < rolls; r++)
        {
            var entry = table.PickEntry(difficulty, rng);
            if(entry == null) continue;

            int count = rng.Next(entry.minCount, entry.maxCount + 1);
            for (int c = 0; c < count; c++)
            {
                var item = CreateItem(entry.item, config, rng);
                if(item != null) drops.Add(new LootDrop { Definition = entry.item, Item = item });
            }
        }
        
        return drops;
    }

    // ItemData (definition) -> a freshly rolled instance.
    public static IInventoryItem CreateItem(ItemData definition, EquipmentProgressConfig config, System.Random rng)
    {
        switch (definition)
        {
            case EquipmentData equipment: return LootRoller.RollEquipment(equipment, config, rng);
            case RuneData rune: return RuneRoller.Roll(rune, rng);
            case ArtifactData artifact: return LootRoller.CreateArtifact(artifact.id);
            case null: return null;
            default:
                Debug.LogWarning($"[LootService] '{definition.itemName}' ({definition.GetType().Name}) can't be dropped yet.");
                return null;
        }
    }

    public static void GrantCurrency(LootTable table, PlayerController player, System.Random rng = null)
    {
        if(table == null || player == null || player.PlayerRuntime == null) return;
        
        var currency = player.PlayerRuntime.Currency;
        if(currency == null) return;
        
        rng ??= new System.Random();
        int gold = rng.Next(table.gold.x, table.gold.y + 1);
        int gem = rng.Next(table.gem.x, table.gem.y + 1);
        
        if(gold > 0) currency.Add(CurrencyType.Gold, gold);
        if(gem > 0) currency.Add(CurrencyType.Gem, gem);
    }

    public static void Spawn(List<LootDrop> drops, Vector3 origin, WorldPickup prefab, float radius = 1.6f)
    {
        if(drops == null || drops.Count == 0) return;
        if(prefab == null)
        {
            Debug.LogError($"[LootService] No pickup prefab assigned! - nothing can drop.");
            return;
        }
        
        float startAngle = Random.value * 360f;
        for(int i = 0; i < drops.Count; i++)
        {
            float angle = startAngle + 360f * i / drops.Count + Random.Range(radius * 0.6f, radius);
            float distance = drops.Count == 1 ? Random.Range(0.5f, radius * 0.7f) : Random.Range(radius * 0.6f, radius);
            Vector3 landing = GroundPoint(origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance, origin.y);
            
            var pickup = Object.Instantiate(prefab, origin + Vector3.up * 0.6f, Quaternion.identity);
            pickup.Setup(drops[i].Item, drops[i].Definition, landing);
        }
    }

    private static Vector3 GroundPoint(Vector3 point, float fallbackY)
    {
        if(NavMesh.SamplePosition(point + Vector3.up * 0.5f, out var hit, 2.5f, NavMesh.AllAreas)) return hit.position;
        point.y = fallbackY;
        return point;
    }
}