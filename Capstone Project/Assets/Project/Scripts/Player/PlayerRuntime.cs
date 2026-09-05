using UnityEngine;
using System;
using UnityEngine.Localization;

public class PlayerRuntime : CharacterRuntime, IPlayerRuntime
{
    private PlayerDataConfig _config;
    
    [Header("Experience")]
    public override int Level => _config != null ? _config.Level : base.Level;
    public float CurrentExp => _config != null ? _config.CurrentExp : 0f;
    public float ExpToNextLevel => _config != null ? _config.ExpToNextLevel : 100f;
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    
    [Header("Player Bonus Stats")]
    protected float bonusAttackSpeed = 0f;
    protected float bonusAttackRange = 0f;
    protected float bonusCritChance = 0f;
    protected float bonusCritDamage = 0f;
    
    public float BonusAttackSpeed => bonusAttackSpeed;
    public float BonusAttackRange => bonusAttackRange;
    public float BonusCritChance => bonusCritChance;
    public float BonusCritDamage => bonusCritDamage;

    [Header("Player Total Stats")] 
    protected float totalAttackSpeed => CharacterData.baseAttackSpeed + bonusAttackSpeed;
    protected float totalAttackRange => CharacterData.baseAttackRange + bonusAttackRange;
    protected float totalCritChance => CharacterData.baseCritChance + bonusCritChance;
    protected float totalCritDamage => CharacterData.baseCritDamage + bonusCritDamage;

    public float TotalAttackSpeed => totalAttackSpeed;
    public float TotalAttackRange => totalAttackRange;
    public float TotalCritChance => totalCritChance;
    public float TotalCritDamage => totalCritDamage;

    public Currency Currency => _config?.Currency;
    private EquipmentManager _equipmentManager;
    public PlayerArchive playerArchive;
    
    public event Action OnStatsChanged; // On Stats Changed event
    
    private void Awake()
    {
        Init();
    }
    
    private void Start()
    {
        RefreshStats();
        
        Debug.Log($"Damage = {TotalDamage}");
        Debug.Log($"Range = {TotalAttackRange}");
        Debug.Log($"Speed = {TotalSpeed}");
    }

    public override void Init()
    {
        base.Init();
        _equipmentManager = EquipmentManager.Instance;
        _equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        
        var configManager = StartupProcessor.Instance?.GetService<ConfigManager>();
        _config = configManager != null && configManager.GetConfig(out PlayerDataConfig config) ? config : null;

        if (_config != null)
        {
            _config.OnLevelUp += LevelUp;
            _config.OnExpChanged += ExpChanged;
            _config.Currency.OnCurrencyGained += OnGoldObtained;
        }
        else
        {
            Debug.LogError($"[PlayerRuntime] Not found Player data config - please check config step in startup progress.");
        }
        
        if(playerArchive == null)
            playerArchive = GetComponent<PlayerArchive>();
    }

    private void OnDestroy()
    {
        if(_equipmentManager != null)
            _equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;

        if (_config != null)
        {
            _config.OnLevelUp -= LevelUp;
            _config.OnExpChanged -= ExpChanged;
            
            if(_config.Currency != null)
                _config.Currency.OnCurrencyGained -= OnGoldObtained;
        }
    }

    public void GainExp(float amount)
        => _config?.GainExp(amount);

    private void ExpChanged(float exp, float toNext)
        => OnExpChanged?.Invoke(exp, toNext);
    
    private readonly LocalizedString _localizedText = new LocalizedString("UI", "LevelUp");
    protected virtual void LevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel);

        Hp = TotalHealth;
        HpChanged(Hp);

        var floatingText = UIManager.Instance?.GetFloatingTextService();
        floatingText?.Create("LevelUpText", Guid.NewGuid().ToString(), _localizedText, transform.position + Vector3.up * 1.1f);
        Debug.Log($"[PlayerRuntime] {gameObject.name} level up to level {newLevel}!");
    }

    protected override void ApplyBonusStat(BonusStat bonusStat, float amount)
    {
        base.ApplyBonusStat(bonusStat, amount);

        switch (bonusStat)
        {
            case BonusStat.AttackRange:
                bonusAttackRange += amount; break;
            case BonusStat.AttackSpeed:
                bonusAttackSpeed += amount; break;
            case BonusStat.CritChance:
                bonusCritChance += amount; break;
            case BonusStat.CritDamage:
                bonusCritDamage += amount; break;
        }
    }
    
    protected override void ResetBonusStats()
    {
        base.ResetBonusStats();
        bonusAttackSpeed = 0f;
        bonusAttackRange = 0f;
        bonusCritChance = 0f;
        bonusCritDamage = 0f;
    }

    private void RefreshStats()
    {
        if (_equipmentManager == null)
            _equipmentManager = EquipmentManager.Instance;
        
        ResetBonusStats();

        /*
        ApplyAttributes(_equipmentManager.GetCurrentEquipment(EquipmentType.MeleeWeapon));
        ApplyAttributes(_equipmentManager.GetCurrentEquipment(EquipmentType.RangedWeapon));
        */
        
        var armor = _equipmentManager.Armor;
        if (armor != null)
        {
            ApplyBonusStat(BonusStat.Defense, armor.armorModifier);
            ApplyBonusStat(BonusStat.MoveSpeed, armor.speedModifier);
        }

        OnStatsChanged?.Invoke();
    }

    public void ApplyAttributes(EquipmentData equipment)
    {
        if(equipment == null)
            return;
        
        ApplyBonusStat(BonusStat.AttackSpeed, equipment.attackSpeedModifier);
    }

    private void HandleEquipmentChanged(EquipmentChangedEventArgs args)
        => RefreshStats();

    public float GetCurrentAttackSpeed(EquipmentType type)
    {
        var equipment = _equipmentManager.GetCurrentEquipment(type);
        float speed = equipment != null ? equipment.attackSpeedModifier : 0f;

        float baseAttackSpeed = CharacterData != null ? CharacterData.baseAttackSpeed : 1f;
        return baseAttackSpeed + speed + bonusAttackSpeed;
    }

    private void OnGoldObtained(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                playerArchive.goldObtained += amount;
                break;
            case CurrencyType.Gem:
                playerArchive.gemObtained += amount;
                break;
        }
    }
}