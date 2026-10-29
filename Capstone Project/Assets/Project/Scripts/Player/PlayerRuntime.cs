using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Localization;

public class PlayerRuntime : CharacterRuntime, IPlayerRuntime
{
    private PlayerDataConfig _config;
    private PlayerController _controller;
    
    [Header("Experience")] 
    protected override int Level => _config != null ? _config.Level : base.Level;
    public float CurrentExp => _config != null ? _config.CurrentExp : 0f;
    public float ExpToNextLevel => _config != null ? _config.ExpToNextLevel : 100f;
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnExpChanged;
    
    [Header("Audio")]
    [SerializeField] private AudioCue levelUpCue;
    
    [Header("Player Bonus Stats")]
    private int bonusCritChance = 0;
    private int bonusCritDamage = 0;
    public int BonusCritChance => bonusCritChance;
    public int BonusCritDamage => bonusCritDamage;
    
    [Header("Player Total Stats")]
    private int totalCritChance => Mathf.RoundToInt((CharacterData.baseCritChance + bonusCritChance) * (1f + GetPercentBonus(BonusStat.CritChance)));
    private int totalCritDamage => Mathf.RoundToInt((CharacterData.baseCritDamage + bonusCritDamage) * (1f + GetPercentBonus(BonusStat.CritDamage)));
    public float TotalCritChance => totalCritChance;
    public float TotalCritDamage => totalCritDamage;
    
    public Currency Currency => _config?.Currency;
    public PlayerDataConfig Config => _config;
    
    public PlayerArchive playerArchive;

    private static readonly int StatCount = Enum.GetValues(typeof(BonusStat)).Length;
    private readonly Dictionary<string, List<StatModifier>> _modifierSources = new Dictionary<string, List<StatModifier>>();
    private readonly float[] _flatBonus = new float[StatCount];
    private readonly float[] _percentBonus = new float[StatCount];
    
    public event Action OnStatsChanged;
    
    private void Awake() => Init();
    
    public override void Init()
    {
        base.Init();
        
       _controller = GetComponent<PlayerController>(); 
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
        
        if(playerArchive == null) playerArchive = GetComponent<PlayerArchive>();
    }

    private void OnDestroy()
    {
        if (_config != null)
        {
            _config.OnLevelUp -= LevelUp;
            _config.OnExpChanged -= ExpChanged;
            if(_config.Currency != null) _config.Currency.OnCurrencyGained -= OnGoldObtained;
        }
    }

    public void GainExp(float amount) => _config?.GainExp(amount);
    private void ExpChanged(float exp, float toNext) => OnExpChanged?.Invoke(exp, toNext);
    
    private readonly LocalizedString _localizedText = new LocalizedString("UI", "LevelUp");
    protected virtual void LevelUp(int newLevel)
    {
        OnLevelUp?.Invoke(newLevel); // This event use for ui driven
        if(levelUpCue != null) AudioService.Instance?.PlaySfx(levelUpCue);

        Hp = TotalHealth;
        HpChanged(Hp);

        var floatingText = UIManager.Instance?.GetFloatingTextService();
        floatingText?.Create("LevelUpText", Guid.NewGuid().ToString(), _localizedText, transform.position + Vector3.up * 1.1f);
        Debug.Log($"[PlayerRuntime] {gameObject.name} level up to level {newLevel}!");
    }

    public void SetModifierSource(string sourceId, IEnumerable<StatModifier> modifiers)
    {
        _modifierSources[sourceId] = new List<StatModifier>(modifiers);
        RebuildModifiers();
    }

    public void RemoveModifierSource(string sourceId)
    {
        if(_modifierSources.Remove(sourceId)) RebuildModifiers();
    }

    protected override float GetPercentBonus(BonusStat stat) => _percentBonus[(int)stat] / 100f;
    
    // Dung cho chi so ting o NOI KHAC ma CharacterRuntime khong quan ly (AttackSpeed cua vu khi...)
    // KHONG dung cho Health/Damage/Defense/MoveSpeed/Crit - nhung chi so do da nam trong Total*.
    public float ModifyStat(BonusStat stat, float baseValue)
        => (baseValue + _flatBonus[(int)stat]) * (1f + _percentBonus[(int)stat] / 100f);

    public override void TakeDamage(float damage, ICharacterRuntime runtime)
    {
        if (_controller != null && _controller.PlayerModifier.IsInvincible)
        {
            Debug.Log($"[PlayerRuntime] {gameObject.name} in I-FRAMES (Roll)! Block {damage} damage.");
            return;
        }
        
        base.TakeDamage(damage, runtime);
    }

    protected override void ApplyBonusStat(BonusStat bonusStat, float amount)
    {
        base.ApplyBonusStat(bonusStat, amount);
        
        switch (bonusStat)
        {
            case BonusStat.CritChance:
                bonusCritChance += Mathf.RoundToInt(amount); break;
            case BonusStat.CritDamage:
                bonusCritDamage += Mathf.RoundToInt(amount); break;
        }
    }

    protected override void ResetBonusStats()
    {
        base.ResetBonusStats();
        bonusCritChance = 0;
        bonusCritDamage = 0;
    }

    private void RebuildModifiers()
    {
        ResetBonusStats();
        Array.Clear(_flatBonus, 0, _flatBonus.Length);
        Array.Clear(_percentBonus, 0, _percentBonus.Length);

        foreach (var source in _modifierSources.Values)
        {
            foreach (var modifier in source)
            {
                if (modifier.Mode == StatMode.Flat)
                {
                    _flatBonus[(int)modifier.Stat] += modifier.Value;
                    ApplyBonusStat(modifier.Stat, modifier.Value);
                }
                else
                {
                    _percentBonus[(int)modifier.Stat] += modifier.Value;
                }
            }
        }

        if (Hp > TotalHealth) Hp = TotalHealth;
        HpChanged(Hp);
        OnStatsChanged?.Invoke();
    }

    private void OnGoldObtained(CurrencyType type, int amount)
    {
        if(playerArchive == null) return;
        
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
    
    public void NotifyStatsChanged() => OnStatsChanged?.Invoke();
}