using UnityEngine;
using System;

public enum BonusStat { Health, Defense, Damage, AttackRange, AttackSpeed, MoveSpeed, CritChance, CritDamage }

public class PlayerRuntime : MonoBehaviour, IAttackable
{
    [Header("Character Base Stats")]
    [SerializeField] private int baseHealth = 100;
    [SerializeField] private int baseDefense = 1;
    [SerializeField] private int baseDamage = 5;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseAttackRange = 1.2f;
    [SerializeField] private float baseCritChance = 5f;
    [SerializeField] private float baseCritDamage = 100f;
    
    private EquipmentManager _equipmentManager;
    
    // === EQUIPMENT STATS === (Help debug)
    private int _equipmentDamage;
    private int _equipmentDefense;
    
    private float _equipmentMoveSpeed;
    private float _equipmentAttackSpeed;
    private float _equipmentAttackRange;
    
    private float _equipmentCritChance;
    private float _equipmentCritDamage;
    
    // === BONUS STATS ===
    private int _bonusHealth; // Reserved for future potion/buff system
    private int _bonusDamage;
    private int _bonusDefense;
    
    private float _bonusMoveSpeed;
    private float _bonusAttackSpeed;
    private float _bonusAttackRange;
    
    private float _bonusCritChance;
    private float _bonusCritDamage;

    public event Action OnStatsChanged; // On Stats Changed event
    public event Action<int> OnHPChanged; // On Health Changed event
    public event Action OnHit;
    private int _currentHealth;
    
    // === CURRENT STATS ===
    public int Health => _currentHealth;
    
    public int Damage { get; private set; }
    public int Defense { get; private set; }
    
    public float MoveSpeed { get; private set; }
    public float AttackSpeed { get; private set; }
    public float AttackRange { get; private set; }
    
    public float CritChance { get; private set; }
    public float CritDamage { get; private set; }

    private void Awake()
    {
        Init();
        RefreshStats();
        EquipmentManager.Instance.OnEquipmentChanged += RefreshStats;
    }
    
    private void Start()
    {
        Debug.Log($"Damage = {Damage}");
        Debug.Log($"Range = {AttackRange}");
        Debug.Log($"Speed = {AttackSpeed}");
    }

    private void Init()
    {
        _equipmentManager = EquipmentManager.Instance;
        _currentHealth = baseHealth;
        OnHPChanged?.Invoke(_currentHealth);
    }

    private void ApplyEquipment(EquipmentData equipment)
    {
        if(equipment == null)
            return;

        _equipmentDamage += equipment.damageModifier;
        _equipmentDefense += equipment.armorModifier;

        _equipmentMoveSpeed += equipment.speedModifier;
        _equipmentAttackSpeed += equipment.attackSpeedModifier;
        _equipmentAttackRange += equipment.attackRangeModifier;
        
        _equipmentCritChance += equipment.critChanceModifier;
        _equipmentCritDamage += equipment.critDamageModifier;
    }

    public void ApplyBonusStat(BonusStat bonusStat, float amount)
    {
        switch (bonusStat)
        {
            case BonusStat.Health:
                _bonusHealth += (int)amount; break;
            case BonusStat.Damage:
                _bonusDamage += (int)amount; break;
            case BonusStat.Defense:
                _bonusDefense += (int)amount; break;
            case BonusStat.MoveSpeed:
                _bonusMoveSpeed += amount; break;
            case BonusStat.AttackSpeed:
                _bonusAttackSpeed += amount; break;
            case BonusStat.AttackRange:
                _bonusAttackRange += amount; break;
            case BonusStat.CritChance:
                _bonusCritChance += amount; break;
            case BonusStat.CritDamage:
                _bonusCritDamage += amount; break;
        }
    }

    private void CalculateTotalStats()
    {
        Damage = baseDamage + (_equipmentDamage + _bonusDamage);
        Defense = baseDefense + (_equipmentDefense + _bonusDefense);
        
        MoveSpeed = baseMoveSpeed + (_equipmentMoveSpeed + _bonusMoveSpeed);
        AttackSpeed = baseAttackSpeed + (_equipmentAttackSpeed + _bonusAttackSpeed);
        AttackRange = baseAttackRange + (_equipmentAttackRange + _bonusAttackRange);
        
        CritChance = baseCritChance + (_equipmentCritChance + _bonusCritChance);
        CritDamage = baseCritDamage + (_equipmentCritDamage + _bonusCritDamage);
        
        OnStatsChanged?.Invoke();
        
        Debug.Log($"Move:{MoveSpeed} Damage:{Damage} AttackSpeed:{AttackSpeed} AttackRange: {AttackRange}");
    }

    private void RefreshStats()
    {
        ResetEquipmentStats(); // Reset
        
        if (_equipmentManager == null)
        {
            Debug.LogWarning("No EquipmentManager found");
            return;
        }
        
        ApplyEquipment(_equipmentManager.Melee);
        ApplyEquipment(_equipmentManager.Ranged);
        ApplyEquipment(_equipmentManager.Armor);

        foreach (var artifact in _equipmentManager.Artifacts)
        {
            ApplyEquipment(artifact);
        }
        
        CalculateTotalStats();
    }

    private void ResetEquipmentStats()
    {
        _equipmentDamage = 0;
        _equipmentDefense = 0;

        _equipmentMoveSpeed = 0;
        _equipmentAttackSpeed = 0;
        _equipmentAttackRange = 0;

        _equipmentCritChance = 0;
        _equipmentCritDamage = 0;
    }
    
    public void TakeDamage(float damage)
    {
        if(this == null)
            return;
        
        DamageReduceCal damageCal = new DamageReduceCal();
        float finalDamage = damageCal.Calculate(damage, Defense);
        
        OnTakeDamage(finalDamage);
        
        _currentHealth -= (int)finalDamage;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, baseHealth);
        
        OnHPChanged?.Invoke(_currentHealth);
        OnHit?.Invoke();

        if (_currentHealth <= 0)
        {
            Die();
        }

        Debug.Log($"{gameObject} took {finalDamage} damage, remaining {_currentHealth} health");
    }

    private void OnTakeDamage(float damage)
    {
        // Update UI
        // Trigger damage flash
        // Damage Popup
        // etc...
    }

    private void Die()
    {
        // Change State
        // Disable/Destroy object
    }

    public void Revive()
    {
        _currentHealth = baseHealth;
        OnHPChanged?.Invoke(_currentHealth);
    }
}