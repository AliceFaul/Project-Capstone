using System;
using UnityEngine;

public enum BonusStat { Health, Defense, Damage, AttackRange, AttackSpeed, MoveSpeed, CritChance, CritDamage }

// Base class for all runtime entity attributes
public class CharacterRuntime : MonoBehaviour, ICharacterRuntime
{
    [Header("Character Attributes")] 
    [SerializeField] protected int level;
    public int Level => level;
    
    [Header("Character Bonus Stats")] 
    private int bonusHealth = 0;
    private int bonusDamage = 0;
    private int bonusArmor = 0;
    private float bonusSpeed = 0f;
    
    public int BonusHealth => bonusHealth;
    public int BonusDamage => bonusDamage;
    public int BonusArmor => bonusArmor;
    public float BonusSpeed => bonusSpeed;

    [Header("Character Total Stats")] 
    private int totalHealth => CharacterData.baseHealth + bonusHealth;
    private int totalDamage => CharacterData.baseDamage + bonusDamage;
    protected int totalDefense => CharacterData.baseDefense + bonusArmor;
    protected float totalSpeed => CharacterData.baseSpeed + bonusSpeed;

    public int TotalHealth => totalHealth;
    public int TotalDamage => totalDamage;
    public int TotalArmor => totalDefense;
    public float TotalSpeed => totalSpeed;

    protected int Hp;
    public int Health => Hp;
    public event Action<int> OnHpChanged;
    public event Action OnHit;

    protected CharacterData CharacterData;

    private CharacterStateType _stateType;
    
    private Material _flashMaterial;
    private DamageFlasher _damageFlash;

    public virtual void Init()
    {
        CharacterData = GetComponent<CharacterInstall>().characterData;
        Hp = totalHealth;
        OnHpChanged?.Invoke(Hp);
        
        _stateType = GetComponent<IStateMachine>().GetCurrentState();
    }

    protected virtual void ApplyBonusStat(BonusStat bonusStat, float amount)
    {
        switch (bonusStat)
        {
            case BonusStat.Health:
                bonusHealth += (int)amount; break;
            case BonusStat.Defense:
                bonusArmor += (int)amount; break;
            case BonusStat.Damage:
                bonusDamage += (int)amount; break;
            case BonusStat.MoveSpeed:
                bonusSpeed += amount; break;
        }
    }
    
    protected virtual void ResetBonusStats()
    {
        bonusHealth = 0;
        bonusDamage = 0;
        bonusArmor = 0;
        bonusSpeed = 0f;
    }

    private static readonly DamageReduceCal DamageReduceCal = new DamageReduceCal();
    
    public virtual void TakeDamage(float damage, ICharacterRuntime runtime)
    {
        if(this == null)
            return;
        
        if (_stateType != CharacterStateType.Dead)
        {
            if(this == null)
                return;
            
            float finalDamage = DamageReduceCal.Calculate(damage, TotalArmor);
            
            OnTakeDamage((int)finalDamage);

            Hp -= (int)finalDamage;
            Hp = Mathf.Clamp(Hp, 0, totalHealth);

            if (runtime is PlayerRuntime player && player.playerArchive != null)
                player.playerArchive.totalDamageDealt += (int)finalDamage;
            
            if(this is PlayerRuntime selfPlayer && selfPlayer.playerArchive != null)
                selfPlayer.playerArchive.totalDamageReceived += (int)finalDamage;
            
            OnHpChanged?.Invoke(Hp);
            OnHit?.Invoke();

            if (Hp <= 0)
            {
                Die();
                if (runtime is IPlayerRuntime playerRuntime)
                {
                    playerRuntime.GainExp(CharacterData.expOnKill);
                    playerRuntime.Currency?.Add(CurrencyType.Gold, (int)CharacterData.goldOnKill);
                    if (playerRuntime is PlayerRuntime playerRuntime2 && playerRuntime2.playerArchive != null)
                    {
                        playerRuntime2.playerArchive.enemyDefeated++;
                    }
                }
            }
            
            Debug.Log($"{gameObject} took {finalDamage} damage, health remaining {Hp}");
        }
    }

    public virtual void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    protected virtual void OnTakeDamage(float damage)
    {
        if (_flashMaterial == null)
        {
            _flashMaterial = ResourceManager.Instance.GetAsset<Material>("DamageFlashMaterial");
            _damageFlash = new DamageFlasher(GetComponentInChildren<Renderer>(), _flashMaterial);
        }
        
        _damageFlash.Trigger();
        var floatingText = UIManager.Instance.GetFloatingTextService();
        floatingText.Create("DamageText", $"dmg{Time.time}_{UnityEngine.Random.Range(0, 99999)}", damage.ToString("F1"), transform.position + Vector3.up * 0.8f);
    }

    public void Revive()
    {
        Hp = totalHealth;
        OnHpChanged?.Invoke(Hp);
        Debug.Log($"{gameObject} has been revived!");
    }

    public virtual void Die()
    {
        gameObject.GetComponent<IStateMachine>().ChangeState(CharacterStateType.Dead);
        Debug.Log($"{gameObject} has been died!");
    }

    protected void HpChanged(int value)
    {
        if (OnHpChanged != null) OnHpChanged.Invoke(value);
    }
}