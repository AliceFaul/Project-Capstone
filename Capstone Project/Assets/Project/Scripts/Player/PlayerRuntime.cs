using UnityEngine;
using System;
using UnityEngine.Localization;

public class PlayerRuntime : CharacterRuntime, IPlayerRuntime
{
    [Header("Experience")]
    [SerializeField] protected float currentExp = 0;
    [SerializeField] protected float expToNextLevel = 100;
    public float CurrentExp => currentExp;
    public float ExpToNextLevel => expToNextLevel;
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

    private Currency _currency;
    public Currency Currency => _currency;

    private EquipmentManager _equipmentManager;

    public PlayerArchive playerArchive;
    public event Action OnStatsChanged; // On Stats Changed event
    
    private void Awake()
    {
        Init();
        RefreshStats();
    }
    
    private void Start()
    {
        Debug.Log($"Damage = {TotalDamage}");
        Debug.Log($"Range = {TotalAttackRange}");
        Debug.Log($"Speed = {TotalSpeed}");
    }

    public override void Init()
    {
        base.Init();
        _equipmentManager = EquipmentManager.Instance;
        _equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        
        _currency = new Currency();
        _currency.OnCurrencyGained += OnGoldObtained;
        
        if(playerArchive == null)
            playerArchive = GetComponent<PlayerArchive>();
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

    private readonly LocalizedString _localizedText = new LocalizedString("UI", "LevelUp");
    protected virtual void LevelUp()
    {
        level++;
        OnLevelUp?.Invoke(level);

        expToNextLevel = Mathf.Round(expToNextLevel * 1.25f);

        Hp = TotalHealth;
        HpChanged(Hp);

        var floatingText = UIManager.Instance?.GetFloatingTextService();
        floatingText?.Create("LevelUpText", Guid.NewGuid().ToString(), _localizedText, transform.position + Vector3.up * 1.1f);
        Debug.Log($"[PlayerRuntime] {gameObject.name} level up to level {level}!");
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
        {
            _equipmentManager = EquipmentManager.Instance;
        }
        
        ResetBonusStats();

        AppyAttributes(_equipmentManager.GetCurrentEquipment(EquipmentType.MeleeWeapon));
        AppyAttributes(_equipmentManager.GetCurrentEquipment(EquipmentType.RangedWeapon));
        
        var armor = _equipmentManager.Armor;
        if (armor != null)
        {
            ApplyBonusStat(BonusStat.Defense, armor.armorModifier);
            ApplyBonusStat(BonusStat.MoveSpeed, armor.speedModifier);
        }

        OnStatsChanged?.Invoke();
    }

    public void AppyAttributes(EquipmentData equipment)
    {
        if(equipment == null)
            return;
        
        ApplyBonusStat(BonusStat.AttackSpeed, equipment.attackSpeedModifier);
    }

    private void HandleEquipmentChanged(EquipmentChangedEventArgs args)
    {
        RefreshStats();
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