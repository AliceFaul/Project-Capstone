using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Project.Capstone.Inventory;

public interface IPlayerIdentity
{
    string DisplayName { get; }
    event Action<string> OnDisplayNameChanged;
    bool SetDisplayName(string newName, out string errorId);
}

public interface IProgression
{
    int Level { get; }
    float CurrentExp { get; }
    float ExpToNextLevel { get; }
    event Action<int> OnLevelUp;
    event Action<float, float> OnExpChanged;
    void GainExp(float amount);
}

public interface ICurrency
{
    Currency Currency { get; }
}

public interface ICosmetics
{
    IReadOnlyList<string> UnlockedCosmeticIds { get; }
    string EquippedCosmeticId { get; }
    event Action<string> OnCosmeticEquipped;
    bool IsCosmeticUnlocked(CosmeticData cosmetic);
    bool PurchaseCosmetic(CosmeticData cosmetic);
    void EquipCosmetic(CosmeticData cosmetic);
}

public interface IInventory
{
    Inventory Inventory { get; }
    Loadout Loadout { get; }
}

[Serializable]
public class GameData
{
    public const int CurrentSchemaVersion = 2;
    public int SchemaVersion;
    
    public string PlayerDisplayName;
    public int Level;
    public float CurrentExp;
    public float ExpToNextLevel;
    public List<CurrencyAmount> CurrencyBalances;
    public List<string> UnlockedCosmeticIds;
    public string EquippedCosmeticId;
    
    public List<EquipmentInstance> EquipmentInstances;
    public List<RuneInstance> RuneInstances;
    public List<ArtifactInstance> ArtifactInstances;
    public List<BagLayout> BagLayouts;
    public LoadoutData Loadout;
}

// Replaced the data fields in Player Runtime;
// data is now loaded during the Config step to provide an instance available for use throughout the application.
[CreateAssetMenu(fileName = "PlayerDataConfig", menuName = "Config/Progress")]
public class PlayerDataConfig : ScriptableObject, IConfig, IPlayerIdentity, IProgression, ICurrency, ICosmetics, IInventory
{
    [Header("Identity")]
    // 'Unknown' name = Call popup service create set name popup when first time play game
    [SerializeField] private string displayName = "";
    
    [Header("Progression")]
    [SerializeField] private int level = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float expToNextLevel = 100f;
    
    [Header("Starting Equipment")]
    [SerializeField] private string startingMeleeWeaponId;
    [SerializeField] private string startingRangedWeaponId;

    [Header("Currency")]
    [SerializeField] private List<CurrencyAmount> currencyBalances = new List<CurrencyAmount>
    {
        new CurrencyAmount { type = CurrencyType.Gold, amount = 0 },
        new CurrencyAmount { type = CurrencyType.Gem, amount = 0 },
    };
    
    [Header("Cosmetics")]
    [SerializeField] private List<string> unlockedCosmeticIds = new List<string>();
    [SerializeField] private string equippedCosmeticId = "";

    [Header("Inventory")]
    [SerializeField] private InventoryConfig inventoryConfig;
    [SerializeField] private ItemDatabase itemDatabase;

    public string DisplayName => displayName;
    public int Level => level;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
    public string StartingMeleeWeaponId => startingMeleeWeaponId;
    public string StartingRangedWeaponId => startingRangedWeaponId;
    public IReadOnlyList<string> UnlockedCosmeticIds => unlockedCosmeticIds;
    public string EquippedCosmeticId => equippedCosmeticId;
    public ItemDatabase ItemDatabase => itemDatabase;

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    public event Action<string> OnDisplayNameChanged;
    public event Action<string> OnCosmeticEquipped;
    public event Action OnDataApplied;

    // Use the lazy pattern to defer initialization until the object is used.
    private Lazy<Currency> _currency;
    private Lazy<Inventory> _inventory;
    private Lazy<Loadout> _loadout;
    
    public Currency Currency => _currency.Value;
    public Inventory Inventory => _inventory.Value;
    public Loadout Loadout => _loadout.Value;
    
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

        _inventory = new Lazy<Inventory>(() =>
        {
            var capacities = inventoryConfig != null ? inventoryConfig.Categories : InventoryConfig.DefaultCategories();
            return new Inventory(capacities);
        }, LazyThreadSafetyMode.None);
        
        _loadout = new Lazy<Loadout>(() => new Loadout(_inventory.Value), LazyThreadSafetyMode.None);
    }

    // unsubscribe to avoid memory leak
    private void OnDisable()
    {
        if(_currency is { IsValueCreated: true }) _currency.Value.OnCurrencyChanged -= SyncCurrency;
    }

    private void SyncCurrency(CurrencyType type, int amount)
    {
        var entry = currencyBalances.Find(x => x.type == type);
        if (entry != null) entry.amount = amount;
        else currencyBalances.Add(new CurrencyAmount { type = type, amount = amount });
    }

    public T GetDefinition<T>(string id) where T : ItemData
    {
        return itemDatabase != null ? itemDatabase.Get(id) as T : null;
    }

    public void GainExp(float amount)
    {
        if(amount <= 0) return;
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

    public bool SetDisplayName(string newName, out string errorId)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            errorId = "NAME_NULL_OR_WHITESPACE";
            return false;
        }
        
        string playerName = newName.Trim();
        
        if (playerName.Length > 20)
        {
            errorId = "NAME_TOO_LONG";
            return false;
        }
        
        displayName = playerName;
        errorId = null;
        OnDisplayNameChanged?.Invoke(displayName);
        return true;
    }

    public bool IsCosmeticUnlocked(CosmeticData cosmetic)
    {
        if(cosmetic == null) return false;
        return cosmetic.unlockedByDefault || unlockedCosmeticIds.Contains(cosmetic.cosmeticId);
    }

    public bool PurchaseCosmetic(CosmeticData cosmetic)
    {
        if (cosmetic == null) return false;
        if (IsCosmeticUnlocked(cosmetic)) return true;
        if (cosmetic.priceGold > 0 && !Currency.TrySpend(CurrencyType.Gold, cosmetic.priceGold)) return false;
        
        if (cosmetic.priceGem > 0 && !Currency.TrySpend(CurrencyType.Gem, cosmetic.priceGem))
        {
            if(cosmetic.priceGold > 0) Currency.Add(CurrencyType.Gold, cosmetic.priceGold);
            return false;
        }
        
        unlockedCosmeticIds.Add(cosmetic.cosmeticId);
        return true;
    }

    public void EquipCosmetic(CosmeticData cosmetic)
    {
        if(cosmetic == null) return;
        equippedCosmeticId = cosmetic.cosmeticId;
        OnCosmeticEquipped?.Invoke(cosmetic.cosmeticId);
    }

    public GameData ToGameData()
    {
        var owned = this.Inventory.AllItems().Concat(this.Loadout.EquippedItems()).ToList();
        
        return new GameData
        {
            SchemaVersion = GameData.CurrentSchemaVersion,
            
            PlayerDisplayName = this.displayName,
            Level = this.level,
            CurrentExp = this.currentExp,
            ExpToNextLevel = this.expToNextLevel,
            CurrencyBalances = this.currencyBalances,
            UnlockedCosmeticIds = this.unlockedCosmeticIds,
            EquippedCosmeticId = this.equippedCosmeticId,
            
            EquipmentInstances = owned.OfType<EquipmentInstance>().ToList(),
            RuneInstances = owned.OfType<RuneInstance>().ToList(),
            ArtifactInstances = owned.OfType<ArtifactInstance>().ToList(),
            
            BagLayouts = this.Inventory.ToLayouts(),
            Loadout = this.Loadout.ToData()
        };
    }

    public void ApplyGameData(GameData gameData)
    {
        if(gameData == null) return;
        
        this.displayName = gameData.PlayerDisplayName ?? this.displayName;
        this.level = gameData.Level;
        this.currentExp = gameData.CurrentExp;
        this.expToNextLevel = gameData.ExpToNextLevel;
        this.currencyBalances = gameData.CurrencyBalances ?? this.currencyBalances;
        this.unlockedCosmeticIds = gameData.UnlockedCosmeticIds ?? this.unlockedCosmeticIds;
        this.equippedCosmeticId = gameData.EquippedCosmeticId ?? this.equippedCosmeticId;

        // Sync currency data
        if (_currency is { IsValueCreated: true })
        {
            foreach (var balance in this.currencyBalances)
            {
                _currency.Value.Set(balance.type, balance.amount);
            }
        }

        ApplyInventory(gameData);
        OnDataApplied?.Invoke();
    }

    private void ApplyInventory(GameData gameData)
    {
        bool hasInventoryData = gameData.BagLayouts != null ||
                                gameData.Loadout != null ||
                                gameData.EquipmentInstances != null ||
                                gameData.RuneInstances != null ||
                                gameData.ArtifactInstances != null;
        
        if(!hasInventoryData) return;

        var lookup = new Dictionary<string, IInventoryItem>();
        IndexItems(lookup, gameData.EquipmentInstances);
        IndexItems(lookup, gameData.RuneInstances);
        IndexItems(lookup, gameData.ArtifactInstances);

        Func<string, IInventoryItem> resolve = id => lookup.TryGetValue(id, out var item) ? item : null;
        this.Inventory.ApplyLayouts(gameData.BagLayouts, resolve);
        this.Loadout.Apply(gameData.Loadout, resolve);
    }

    private static void IndexItems<T>(Dictionary<string, IInventoryItem> lookup, List<T> source)
        where T : class, IInventoryItem
    {
        if(source == null) return;

        foreach (var item in source)
        {
            if(item == null || string.IsNullOrEmpty(item.InstanceId)) continue;
            if(item is EquipmentInstance equipment) equipment.EnsureSockets();
            lookup[item.InstanceId] = item;
        }
    }
}