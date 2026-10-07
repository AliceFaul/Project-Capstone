using UnityEngine;
using System;

public interface IPlayerRuntime : ICharacterRuntime
{
    Currency Currency { get; }

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    public event Action OnStatsChanged;
    
    void GainExp(float amount);
}