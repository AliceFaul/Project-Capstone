using System;

public interface ICharacterRuntime : IAttackable
{
    int Health { get; }
    int TotalHealth { get; }
    int TotalDamage { get; }
    int TotalArmor { get; }
    float TotalSpeed { get; }
    float TotalAttackSpeed { get; }
    float TotalAttackRange { get; }
    float TotalCritChance { get; }
    float TotalCritDamage { get; }

    event Action<int> OnHpChanged;
    event Action OnHit;

    int BonusHealth { get; }
    int BonusDamage { get; }
    int BonusArmor { get; }
    float BonusSpeed { get; }
    float BonusAttackSpeed { get; }
    float BonusAttackRange { get; }
    float BonusCritChance { get; }
    float BonusCritDamage { get; }

    void Init();
    void OnTakeDamage();
    void Revive();
    void Die();
}