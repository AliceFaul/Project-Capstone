using System;
using UnityEngine;

public enum BonusStat { Health, Defense, Damage, AttackRange, AttackSpeed, MoveSpeed, CritChance, CritDamage }

// Base class for all runtime entity attributes
public sealed class CharacterRuntime : MonoBehaviour, ICharacterRuntime
{
    [Header("Character Attributes")] 
    [SerializeField] private int level;
    public int Level => level;
    
    [Header("Character Bonus Stats")] private int bonusHealth = 0;
    private int bonusDamage = 0;
    private int bonusArmor = 0;
    private float bonusSpeed = 0f;
    private float bonusAttackSpeed = 0f;
    private float bonusAttackRange = 0f;
    private float bonusCritChance = 0f;
    private float bonusCritDamage = 0f;
    
    public int BonusHealth => bonusHealth;
    public int BonusDamage => bonusDamage;
    public int BonusArmor => bonusArmor;
    public float BonusSpeed => bonusSpeed;
    public float BonusAttackSpeed => bonusAttackSpeed;
    public float BonusAttackRange => bonusAttackRange;
    public float BonusCritChance => bonusCritChance;
    public float BonusCritDamage => bonusCritDamage;

    [Header("Character Total Stats")] private int totalHealth => characterData.baseHealth + bonusHealth;
    private int totalDamage => characterData.baseDamage + bonusDamage;
    private int totalDefense => characterData.baseDefense + bonusArmor;
    private float totalSpeed => characterData.baseSpeed + bonusSpeed;
    private float totalAttackSpeed => characterData.baseAttackSpeed + bonusAttackSpeed;
    private float totalAttackRange => characterData.baseAttackRange + bonusAttackRange;
    private float totalCritChance => characterData.baseCritChance + bonusCritChance;
    private float totalCritDamage => characterData.baseCritDamage + bonusCritDamage;
    
    public int TotalHealth => totalHealth;
    public int TotalDamage => totalDamage;
    public int TotalArmor => totalDefense;
    public float TotalSpeed => totalSpeed;
    public float TotalAttackSpeed => totalAttackSpeed;
    public float TotalAttackRange => totalAttackRange;
    public float TotalCritChance => totalCritChance;
    public float TotalCritDamage => totalCritDamage;

    private int Hp;
    public int Health => Hp;
    public event Action<int> OnHpChanged;
    public event Action OnHit;

    private CharacterData characterData;
    
    private Material _flashMaterial;
    private DamageFlasher _damageFlash;

    public void Init()
    {
        
    }
    
    public void TakeDamage(float damage)
    {
        
    }
    
    public void OnTakeDamage()
    {
        
    }

    public void Revive()
    {
        
    }

    public void Die()
    {

    }
}