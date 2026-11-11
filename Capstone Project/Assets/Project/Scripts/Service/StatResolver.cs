using System.Collections.Generic;

// Chi so hieu dung cua 1 mon trang bi:
// (gia tri tier x he so level + rune Flat) x (1 + rune Percent / 100)
public static class StatResolver
{
    public static float GetStat(EquipmentInstance instance, EquipmentData def, BonusStat stat, EquipmentProgressConfig config)
    {
        if (instance == null || def == null) return 0f;
 
        float baseValue = 0f;
        var table = def.GetStatTable(stat);

        if (table != null)
        {
            baseValue = table.GetValue(instance.GetTier(stat));
            if (config != null) baseValue *= config.GetLevelMultiplier(stat, instance.level);
        }
        
        if(config == null) return baseValue;
 
        // Rune: ke ca khi vu khi khong co dong goc cho chi so nay van duoc cong.
        float flat = RuneResolver.GetStatBonus(instance, stat, config.GetRune, StatMode.Flat);
        float percent = RuneResolver.GetStatBonus(instance, stat, config.GetRune, StatMode.Percent);

        return (baseValue + flat) * (1f + percent / 100f);
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