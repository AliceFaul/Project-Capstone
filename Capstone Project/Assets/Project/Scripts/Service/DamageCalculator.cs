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
        var damage = runtime.Damage;

        bool isCritical = false;

        var critChance = runtime.CritChance;
        var critDamage = runtime.CritDamage;

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