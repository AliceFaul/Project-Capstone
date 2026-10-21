using System;

public interface IPlayerRuntime : ICharacterRuntime
{
    int BonusCritChance { get; }
    int BonusCritDamage { get; }
    
    float TotalCritChance { get; }
    float TotalCritDamage { get; }
    
    Currency Currency { get; }

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    public event Action OnStatsChanged;
    
    void GainExp(float amount);
}