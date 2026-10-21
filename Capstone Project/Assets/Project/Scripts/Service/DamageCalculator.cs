using UnityEngine;
using Random = UnityEngine.Random;

public struct DamageResult
{
    public readonly int Damage;
    public bool IsCritical;

    public DamageResult(int damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }
}

public static class DamageCalculator
{
    public static DamageResult Calculate(PlayerRuntime runtime, EquipmentInstance weapon, EquipmentData definition, EquipmentProgressConfig config)
    {
        if (runtime == null)
        {
            Debug.LogError($"[DamageCalculator] PlayerRuntime is null.]");
            return new DamageResult(0, false);
        }

        if (weapon == null)
        {
            Debug.LogWarning($"[DamageCalculator] Weapon isntance is null.");
            return new DamageResult(0, false);
        }

        if (definition == null)
        {
            Debug.LogError($"[DamageCalculator] Definition is null for weapon instance '{weapon.InstanceId}'.");
            return new DamageResult(0, false);
        }

        var weaponDamage = StatResolver.GetStat(weapon, definition, BonusStat.Damage, config);
        var damage = runtime.TotalDamage + weaponDamage;
        
        var critChance = runtime.TotalCritChance + StatResolver.GetStat(weapon, definition, BonusStat.CritChance, config);
        var critDamage = runtime.TotalCritDamage + StatResolver.GetStat(weapon, definition, BonusStat.CritDamage, config);
        
        bool isCritical = Random.value <= critChance / 100f;

        if (isCritical) damage *= critDamage / 100f;
        
        return new DamageResult(Mathf.RoundToInt(damage), isCritical);
    }
}