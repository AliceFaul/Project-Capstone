using UnityEngine;
using System.Collections.Generic;
using System;

public enum CurrencyType
{
    Gold,
    Gem,
}

[System.Serializable]
public class CurrencyAmount
{
    public CurrencyType type;
    public int amount;
}

public class Currency
{
    private readonly List<CurrencyAmount> _initialBalances = new List<CurrencyAmount>
    {
        new CurrencyAmount
        {
            type = CurrencyType.Gold,
            amount = 0
        },
        new CurrencyAmount
        {
            type = CurrencyType.Gem,
            amount = 0
        }
    };
    
    private readonly Dictionary<CurrencyType, int> _balances = new();

    public event Action<CurrencyType, int> OnCurrencyChanged;
    public event Action<CurrencyType, int> OnCurrencyGained;

    public Currency()
    {
        foreach (var currency in _initialBalances)
        {
            _balances[currency.type] = currency.amount;
        }
    }
    
    // Get amount value by type reference
    private int GetAmount(CurrencyType type)
    {
        return _balances.GetValueOrDefault(type, 0);
    }
    
    public int Gold() => GetAmount(CurrencyType.Gold);
    public int Gem() => GetAmount(CurrencyType.Gem);
    
    // Set and spend currency logic flow
    public void Set(CurrencyType type, int amount, bool save = true)
    {
        var clamped = Mathf.Max(0, amount);
        _balances[type] = clamped;
        OnCurrencyChanged?.Invoke(type, clamped);
    }

    public bool TrySpend(CurrencyType type, int amount, bool save = true)
    {
        if(amount <= 0)
            return true;
        
        var currentAmount = GetAmount(type);

        if (currentAmount < amount)
            return false;
        
        currentAmount -= amount;
        _balances[type] = currentAmount;
        OnCurrencyChanged?.Invoke(type, currentAmount);
        return true;
    }

    public void Add(CurrencyType type, int amount, bool save = true)
    {
        if(amount <= 0)
            return;
        var currentAmount = GetAmount(type) + amount;

        if (currentAmount < 0)
            currentAmount = 0;
        
        _balances[type] = currentAmount;
        OnCurrencyGained?.Invoke(type, amount);
        OnCurrencyChanged?.Invoke(type, currentAmount);
    }
}