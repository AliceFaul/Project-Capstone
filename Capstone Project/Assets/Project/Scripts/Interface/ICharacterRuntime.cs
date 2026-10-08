using System;

public interface ICharacterRuntime : IAttackable
{
    int Health { get; }
    int TotalHealth { get; }
    int TotalDamage { get; }
    int TotalArmor { get; }
    float TotalSpeed { get; }

    event Action<int> OnHpChanged;
    event Action OnHit;

    int BonusHealth { get; }
    int BonusDamage { get; }
    int BonusArmor { get; }
    float BonusSpeed { get; }

    void Init();
    void Revive();
    void Die();
}