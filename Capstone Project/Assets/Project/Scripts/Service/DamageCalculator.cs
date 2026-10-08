using UnityEngine;
using Random = UnityEngine.Random;

public struct DamageResult
{
    public int Damage;
    public bool IsCritical;

    public DamageResult(int damage, bool isCritical)
    {
        Damage = damage;
        IsCritical = isCritical;
    }
}

public static class DamageCalculator
{
    public static DamageResult Calculate(PlayerRuntime runtime, EquipmentData equipment)
    {
        var damage = runtime.TotalDamage;

        bool isCritical = false;

        var critChance = runtime.TotalCritChance;
        var critDamage = runtime.TotalCritDamage;

        if (equipment != null)
        {
            damage += equipment.attributes.damage + equipment.damageModifier;
            critChance += equipment.attributes.critChance + equipment.critChanceModifier;
            critDamage += equipment.attributes.critDamage + equipment.critDamageModifier;
        }

        if (Random.value <= critChance / 100f)
        {
            damage = Mathf.RoundToInt(damage * (critDamage / 100f));
            isCritical = true;
        }
        
        return new DamageResult(damage, isCritical);
    }
}