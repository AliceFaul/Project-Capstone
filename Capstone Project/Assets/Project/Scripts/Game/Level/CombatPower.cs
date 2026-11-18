using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Capstone.Inventory;

[CreateAssetMenu(fileName = "CombatPowerConfig", menuName = "Level/Combat Power Config")]
public class CombatPowerConfig : ScriptableObject
{
    [Serializable]
    public struct StatWeight
    {
        public BonusStat stat;
        public float weight;
    }

    [Tooltip("Combat power = sum over equipped weapons and armor of (effective stat x weight) + player level x perPlayerLevel. Tune the weights to your stat scales.")]
    public List<StatWeight> weights = new List<StatWeight>
    {
        new StatWeight { stat = BonusStat.Health, weight = 0.5f },
        new StatWeight { stat = BonusStat.Defense, weight = 3f },
        new StatWeight { stat = BonusStat.Damage, weight = 4f },
        new StatWeight { stat = BonusStat.AttackSpeed, weight = 25f },
        new StatWeight { stat = BonusStat.MoveSpeed, weight = 5f },
        new StatWeight { stat = BonusStat.CritChance, weight = 2f },
        new StatWeight { stat = BonusStat.CritDamage, weight = 0.5f },
    };

    [Min(0f)] public float perPlayerLevel = 10f;

    public float GetWeight(BonusStat stat)
    {
        foreach (var entry in weights)
        {
            if (entry.stat == stat) return entry.weight;
        }
        return 0f;
    }
}

public static class CombatPower
{
    public static int Calculate(PlayerDataConfig data, CombatPowerConfig config)
    {
        var equipment = EquipmentManager.Instance;
        if (data == null || config == null || equipment == null) return 0;

        float power = data.Level * config.perPlayerLevel;
        var progress = equipment.ProgressConfig;

        foreach (var slot in new[] { EquipSlot.Melee, EquipSlot.Ranged, EquipSlot.Armor })
        {
            var instance = equipment.GetEquipped(slot);
            var definition = equipment.GetDefinition(instance);
            if (instance == null || definition == null) continue;

            // GetStat includes level and rune bonuses, even for stats the item has no base value for.
            foreach (BonusStat stat in Enum.GetValues(typeof(BonusStat)))
            {
                float weight = config.GetWeight(stat);
                if (weight > 0f) power += StatResolver.GetStat(instance, definition, stat, progress) * weight;
            }
        }
        return Mathf.RoundToInt(power);
    }
}