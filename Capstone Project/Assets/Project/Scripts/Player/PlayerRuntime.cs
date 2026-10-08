using UnityEngine;
using System;
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
    
    [Header("Player Bonus Stats")]
    private int bonusCritChance = 0;
    private int bonusCritDamage = 0;
    public int BonusCritChance => bonusCritChance;
    public int BonusCritDamage => bonusCritDamage;
    
    [Header("Player Total Stats")]
    private int totalCritChance => Mathf.RoundToInt(CharacterData.baseCritChance + bonusCritChance);
    private int totalCritDamage => Mathf.RoundToInt(CharacterData.baseCritDamage + bonusCritDamage);
    public float TotalCritChance => totalCritChance;
    public float TotalCritDamage => totalCritDamage;
    
    public Currency Currency => _config?.Currency;
    public PlayerDataConfig Config => _config;
    
    public PlayerArchive playerArchive;
    
    public event Action OnStatsChanged;
    
    private void Awake()
    {
        Init();
    }
    
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

        Hp = TotalHealth;
        HpChanged(Hp);

        var floatingText = UIManager.Instance?.GetFloatingTextService();
        floatingText?.Create("LevelUpText", Guid.NewGuid().ToString(), _localizedText, transform.position + Vector3.up * 1.1f);
        Debug.Log($"[PlayerRuntime] {gameObject.name} level up to level {newLevel}!");
    }

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