using System;
using System.Collections.Generic;
using UnityEngine;

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