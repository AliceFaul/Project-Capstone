using System.Collections.Generic;

public static class StatResolver
{
    public static float GetStat(EquipmentInstance instance, EquipmentData def, BonusStat stat, EquipmentProgressConfig config)
    {
        if (instance == null || def == null) return 0f;
 
        var table = def.GetStatTable(stat);
        if (table == null) return 0f;
 
        float value = table.GetValue(instance.GetTier(stat));
        if (config != null) value *= config.GetLevelMultiplier(stat, instance.level);
 
        // TODO Phase B: cong them cac dong chi so cua rune da gan trong socket.
        return value;
    }
 
    public static IEnumerable<KeyValuePair<BonusStat, float>> GetAllStats(EquipmentInstance instance, EquipmentData def, EquipmentProgressConfig config)
    {
        if (instance == null || def == null) yield break;
 
        foreach (var table in def.stats)
        {
            yield return new KeyValuePair<BonusStat, float>(table.Stat, GetStat(instance, def, table.Stat, config));
        }
    }
}