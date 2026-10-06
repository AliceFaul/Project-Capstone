using UnityEngine;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class TierTable
{
    [Tooltip("Index 0 = Tier 1, Index 1 = Tier 2, Index 2 = Tier 3, Index 3 = Tier 4")]
    public float[] values = new float[5];
    public int TierCount => values?.Length ?? 0;

    public int Roll()
    {
        if (TierCount == 0) return 0;
        return Random.Range(1, TierCount + 1);
    }

    public float GetValue(int tier)
    {
        if(tier <= 0 || tier > TierCount) return 0f;
        return values[tier - 1];
    }
}

[CreateAssetMenu(fileName = "New Weapon Definition", menuName = "Inventory/Weapon Definition")]
public class WeaponDefinition : EquipmentData
{
    [Header("Weapon Stat Roll Configuration")]
    [SerializeField] private TierTable damageTiers = new();
    [SerializeField] private TierTable attackSpeedTiers = new();
    [SerializeField] private TierTable attackRangeTiers = new();
    [SerializeField] private TierTable critChanceTiers = new();
    [SerializeField] private TierTable critDamageTiers = new();
    
    public TierTable DamageTiers => damageTiers;
    public TierTable AttackSpeedTiers => attackSpeedTiers;
    public TierTable AttackRangeTiers => attackRangeTiers;
    public TierTable CritChanceTiers => critChanceTiers;
    public TierTable CritDamageTiers => critDamageTiers;
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (equipmentType != EquipmentType.MeleeWeapon && equipmentType != EquipmentType.RangedWeapon)
        {
            Debug.LogWarning($"[WeaponDefinition] {name} must use Melee weapon or Ranged weapon as Equipment Type.", this);
        }            
    }
#endif
    
}

public interface IWeapon
{
    string Name { get; }
    int Level { get; }

    int DamageTier { get; }
    int AttackSpeedTier { get; }
    int AttackRangeTier  { get; }
    int CritChanceTier { get; }
    int CritDamageTier { get; }

    int Damage { get; }
    float AttackSpeed { get; }
    float AttackRange { get; }
    float CritChance { get; }
    float CritDamage { get; }
    
    int UnlockedSocketCount { get; }
    
    WeaponDefinition WeaponDefinition { get; }
}

public class Weapon : Item, IWeapon
{
    private const int MaxLevel = 10;
    private readonly WeaponDefinition _definition;
    
    private int _level;

    private int _damageTier;
    private int _attackSpeedTier;
    private int _attackRangeTier;
    private int _critChanceTier;
    private int _critDamageTier;
    
    public string Name => Definition.itemName;
    
    public int Level => _level;
    
    public int DamageTier => _damageTier;
    public int AttackSpeedTier => _attackSpeedTier;
    public int AttackRangeTier => _attackRangeTier;
    public int CritChanceTier => _critChanceTier;
    public int CritDamageTier => _critDamageTier;

    public WeaponDefinition WeaponDefinition => _definition;
    
    public int Damage => Mathf.RoundToInt(_definition.DamageTiers.GetValue(_damageTier));
    public float AttackSpeed => _definition.AttackSpeedTiers.GetValue(_attackSpeedTier);
    public float AttackRange => _definition.AttackRangeTiers.GetValue(_attackRangeTier);
    public float CritChance => _definition.CritChanceTiers.GetValue(_critChanceTier);
    public float CritDamage => _definition.CritDamageTiers.GetValue(_critDamageTier);

    public int UnlockedSocketCount
    {
        get
        {
            // Level 3 = 1 socket, Level 5 = 2 socket, Level 10 = 3 socket
            return _level switch
            {
                < 3 => 0,
                < 5 => 1,
                < 10 => 2,
                _ => 3
            };
        }
    }
    
    public Weapon(WeaponDefinition definition, int level = 1) : base(definition)
    {
        _definition = definition;
        if (_definition == null) throw new ArgumentNullException(nameof(definition));
        
        _level = Mathf.Clamp(level, 1, MaxLevel);
        RollStats();
    }

    private void RollStats()
    {
        _damageTier = _definition.DamageTiers.Roll();
        _attackSpeedTier = _definition.AttackSpeedTiers.Roll();
        _attackRangeTier = _definition.AttackRangeTiers.Roll();
        _critChanceTier = _definition.CritChanceTiers.Roll();
        _critDamageTier = _definition.CritDamageTiers.Roll();
    }
    
    public void SetLevel(int level) => _level = Mathf.Clamp(level, 1, MaxLevel);

    public override string ToString()
    {
        return $"{_definition.itemName} [Lv.{_level}] Damage T{_damageTier}, AttackSpeed T{_attackSpeedTier}, Range T{_attackRangeTier}, CritChance T{_critChanceTier}, CritDamage T{_critDamageTier}";
    }
}