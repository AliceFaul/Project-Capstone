using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// Replaced the data fields in Player Runtime;
// data is now loaded during the Config step to provide an instance available for use throughout the application.
[CreateAssetMenu(fileName = "PlayerDataConfig", menuName = "Config/Progress")]
public class PlayerDataConfig : ScriptableObject, IConfig
{
    [Header("Progression")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;

    [Header("Currency (Sync while runtime)")]
    [SerializeField] private List<CurrencyAmount> currencyBalances = new List<CurrencyAmount>
    {
        new CurrencyAmount { type = CurrencyType.Gold, amount = 0 },
        new CurrencyAmount { type = CurrencyType.Gem, amount = 0 },
    };
    
    public int Level => level;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;

    // Use the lazy pattern to defer initialization until the object is used.
    private Lazy<Currency> _currency;
    public Currency Currency => _currency.Value;
    
    private void OnEnable()
    {
        _currency = new Lazy<Currency>(() =>
        {
            var instance = new Currency();
            foreach (var currency in currencyBalances)
            {
                instance.Set(currency.type, currency.amount);
            }
            instance.OnCurrencyChanged += SyncCurrency;
            return instance;
        }, LazyThreadSafetyMode.None);
    }

    // unsubscribe to avoid memory leak
    private void OnDisable()
    {
        if(_currency is { IsValueCreated: true })
            _currency.Value.OnCurrencyChanged -= SyncCurrency;
    }

    private void SyncCurrency(CurrencyType type, int amount)
    {
        var entry = currencyBalances.Find(x => x.type == type);

        if (entry != null)
        {
            entry.amount = amount;
        }
        else
        {
            currencyBalances.Add(new CurrencyAmount { type = type, amount = amount });
        }
    }

    public void GainExp(float amount)
    {
        if(amount <= 0)
            return;
        
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        expToNextLevel = Mathf.Round(expToNextLevel * 1.25f);
        OnLevelUp?.Invoke(level);
    }
}