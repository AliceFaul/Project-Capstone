using UnityEngine;
using System;

public interface IPlayerRuntime : ICharacterRuntime
{
    float TotalAttackSpeed { get; }
    float TotalAttackRange { get; }
    float TotalCritChance { get; }
    float TotalCritDamage { get; }
    
    Currency Currency { get; }

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    public event Action OnStatsChanged;
    
    float BonusAttackSpeed { get; }
    float BonusAttackRange { get; }
    float BonusCritChance { get; }
    float BonusCritDamage { get; }
    
    void GainExp(float amount);
    void AppyAttributes(EquipmentData equipment);
}