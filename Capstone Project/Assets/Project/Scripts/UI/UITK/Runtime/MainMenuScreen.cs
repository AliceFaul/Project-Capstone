using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;

[RequireComponent(typeof(UIDocument))]
public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private CosmeticDatabase cosmeticDatabase;
    [SerializeField] private AudioMixer mainAudioMixer;
    
    private VisualElement _root;
    private VisualElement _characterBlock;
    private VisualElement _changeCosmeticPanel;
    private VisualElement _cosmeticGrid;
    private VisualElement _cosmeticConfirmRow;
 
    private IconLabelElement _levelDisplay;
    private IconLabelElement _goldDisplay;
    private IconLabelElement _gemDisplay;
    private Button _playerNameLabel;
 
    private Button _startGameButton;
    private Button _optionButton;
    private Button _quitButton;
    private Button _changeCosmeticButton;
    private Button _closeCosmeticButton;
    private Label _cosmeticSelectedName;
    private Button _cosmeticConfirmButton;
 
    private VisualElement _settingsPanel;
    private Button _tabVideo, _tabAudio, _tabGameplay;
    private VisualElement _pageVideo, _pageAudio, _pageGameplay;
    private DropdownField _qualityDropdown, _fpsDropdown;
    private Toggle _cameraShakeToggle, _damageTextToggle;
    private Slider _masterSlider, _musicSlider, _sfxSlider;
    private Label _masterValue, _musicValue, _sfxValue;
    private Button _closeSettingsButton;
    
    private PlayerConfig _playerConfig;
    private PlayerDataConfig _config;
    private CosmeticData _selectedCosmetic;

    public event Action StartGameClicked;
    public event Action OptionClicked;
    public event Action QuitClicked;
    public event Action ChangeCosmeticClicked;
    public event Action CloseCosmeticClicked;
    public event Action<CosmeticData> CosmeticPreviewed;
    public event Action CosmeticPreviewCanceled;
    public event Action PlayerNameClicked;
    
    public bool IsSettingsPanelOpen => _settingsPanel?.resolvedStyle.display == DisplayStyle.Flex;
    public bool IsCosmeticPanelOpen => _changeCosmeticPanel?.resolvedStyle.display == DisplayStyle.Flex;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        QueryElements();
        WireButtons();
    }

    private void OnEnable()
    {
        _root.style.display = DisplayStyle.None;
        _changeCosmeticPanel.style.display = DisplayStyle.None;
        _settingsPanel.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        if (_config != null)
        {
            _config.OnLevelUp -= HandleLevelUp;
            _config.OnExpChanged -= HandleExpProgress;
            _config.OnDataApplied -= Refresh;
            _config.OnCosmeticEquipped -= HandleCosmeticEquipped;
            if (_config.Currency != null) _config.Currency.OnCurrencyChanged -= HandleCurrency;
        }
    }

    private void QueryElements()
    {
        _characterBlock = _root.Q<VisualElement>("character-block");
        _changeCosmeticPanel = _root.Q<VisualElement>("change-cosmetic-panel");
        _cosmeticGrid = _root.Q<VisualElement>("cosmetic-grid");
        _cosmeticConfirmRow = _root.Q<VisualElement>("cosmetic-confirm-row");
 
        _levelDisplay = _root.Q<IconLabelElement>("level-display");
        _goldDisplay = _root.Q<IconLabelElement>("gold-display");
        _gemDisplay = _root.Q<IconLabelElement>("gem-display");
        _playerNameLabel = _root.Q<Button>("player-name-label");
 
        _startGameButton = _root.Q<Button>("start-game-button");
        _optionButton = _root.Q<Button>("option-button");
        _quitButton = _root.Q<Button>("quit-button");
        _changeCosmeticButton = _root.Q<Button>("change-cosmetic-button");
        _closeCosmeticButton = _root.Q<Button>("close-cosmetic-button");
        _cosmeticSelectedName = _root.Q<Label>("cosmetic-selected-name");
        _cosmeticConfirmButton = _root.Q<Button>("cosmetic-confirm-button");
 
        _settingsPanel = _root.Q<VisualElement>("settings-panel");
        _tabVideo = _root.Q<Button>("tab-video");
        _tabAudio = _root.Q<Button>("tab-audio");
        _tabGameplay = _root.Q<Button>("tab-gameplay");
        _pageVideo = _root.Q<VisualElement>("page-video");
        _pageAudio = _root.Q<VisualElement>("page-audio");
        _pageGameplay = _root.Q<VisualElement>("page-gameplay");
        _qualityDropdown = _root.Q<DropdownField>("quality-dropdown");
        _fpsDropdown = _root.Q<DropdownField>("fps-dropdown");
        _cameraShakeToggle = _root.Q<Toggle>("camera-shake-toggle");
        _damageTextToggle = _root.Q<Toggle>("damage-text-toggle");
        _masterSlider = _root.Q<Slider>("master-slider");
        _musicSlider = _root.Q<Slider>("music-slider");
        _sfxSlider = _root.Q<Slider>("sfx-slider");
        _masterValue = _root.Q<Label>("master-value");
        _musicValue = _root.Q<Label>("music-value");
        _sfxValue = _root.Q<Label>("sfx-value");
        _closeSettingsButton = _root.Q<Button>("close-settings-button");
    }

    private void WireButtons()
    {
        _startGameButton.clicked += () =>  StartGameClicked?.Invoke();
        _optionButton.clicked += () => OptionClicked?.Invoke();
        _quitButton.clicked += () => QuitClicked?.Invoke();
        _changeCosmeticButton.clicked += () => ChangeCosmeticClicked?.Invoke();
        _closeCosmeticButton.clicked += () => CloseCosmeticClicked?.Invoke();

        if (_playerNameLabel != null) _playerNameLabel.clicked += () => PlayerNameClicked?.Invoke();
        _cosmeticConfirmButton.clicked += OnConfirmCosmeticClicked;

        _tabVideo.clicked += () => SelectSettingsTab(0);
        _tabAudio.clicked += () => SelectSettingsTab(1);
        _tabGameplay.clicked += () => SelectSettingsTab(2);
        _closeSettingsButton.clicked += HideSettingsPanel;
        
        _masterSlider.RegisterValueChangedCallback(e => _masterValue.text = $"{e.newValue:P0}");
        _musicSlider.RegisterValueChangedCallback(e => _musicValue.text = $"{e.newValue:P0}");
        _sfxSlider.RegisterValueChangedCallback(e => _sfxValue.text = $"{e.newValue:P0}");
    }

    private void BindData()
    {
        var configMg = StartupProcessor.Instance?.GetService<ConfigManager>();
        _config = configMg != null && configMg.GetConfig(out PlayerDataConfig config) ? config : null;
        
        if (_config == null)
        {
            Debug.LogError($"[MainMenuScreen] Player config missing.");
            return;
        }
        
        _config.OnLevelUp += HandleLevelUp;
        _config.OnExpChanged += HandleExpProgress;
        _config.OnDataApplied += Refresh;
        _config.OnCosmeticEquipped += HandleCosmeticEquipped;
        if(_config.Currency != null) _config.Currency.OnCurrencyChanged += HandleCurrency;
        
        Refresh();
    }

    private void Refresh()
    {
        if(_config == null) return;   
        _levelDisplay.SetText($"{_config.Level}");
        _goldDisplay.SetAmount(_config.Currency.Gold());
        _gemDisplay.SetAmount(_config.Currency.Gem());
        _playerNameLabel.text = string.IsNullOrEmpty(_config.DisplayName) ? "Unknown" : _config.DisplayName;
    }
    
    private void HandleLevelUp(int level) => _levelDisplay.SetText($"{level}");
    private void HandleExpProgress(float current, float toNext) { /*TODO: Add exp progress bar around level text*/ }

    private void HandleCurrency(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                _goldDisplay.SetAmount(amount); break;
            case CurrencyType.Gem:
                _gemDisplay.SetAmount(amount); break;
        }
    }
    
    // ======== API ========
    public void SetPlayerName(string playerName)
    {
        _playerNameLabel.text = playerName;
    }

    public void Show()
    {
        if(_config == null) BindData();
        _root.style.display = DisplayStyle.Flex;
    }
    
    public void Hide() => _root.style.display = DisplayStyle.None;

    public void ShowChangeCosmeticPanel()
    {
        _changeCosmeticPanel.style.display = DisplayStyle.Flex;
        _startGameButton.style.display = DisplayStyle.None;
        _optionButton.style.display = DisplayStyle.None;
        _quitButton.style.display = DisplayStyle.None;
        _characterBlock.style.display = DisplayStyle.None;
        
        _selectedCosmetic = null;
        PopulateSkinGrid();
    }

    public void HideChangeCosmeticPanel()
    {
        _changeCosmeticPanel.style.display = DisplayStyle.None;
        _startGameButton.style.display = DisplayStyle.Flex;
        _optionButton.style.display = DisplayStyle.Flex;
        _quitButton.style.display = DisplayStyle.Flex;
        _characterBlock.style.display = DisplayStyle.Flex;
        
        CosmeticPreviewCanceled?.Invoke();
    }

    private void PopulateSkinGrid()
    {
        _cosmeticGrid.Clear();

        if (cosmeticDatabase == null)
        {
            Debug.LogError($"[MainMenuScreen] Not reference cosmetic database.");
            return;
        }

        foreach (var cosmetic in cosmeticDatabase.cosmetics)
        {
            bool isUnlocked = _config.IsCosmeticUnlocked(cosmetic);
            bool isEquipped = _config.EquippedCosmeticId == cosmetic.cosmeticId;

            var slot = new ItemSlotElement();
            slot.SetItem(cosmetic, cosmetic.cosmeticIcon, quantity: 1, isEquipped: isEquipped, rarityUssClass: null);
            
            if(isUnlocked) slot.SetUnlocked();
            else slot.SetLocked(cosmetic.priceGem > 0 ? $"{cosmetic.priceGem} Gem" : $"{cosmetic.priceGold} Gold");
            
            slot.Clicked += _ => OnCosmeticSlotClicked(cosmetic, isUnlocked);
            _cosmeticGrid.Add(slot);
        }
        
        Debug.Log($"[MainMenuScreen] Cosmetic Grid loaded!");
    }

    private void OnCosmeticSlotClicked(CosmeticData cosmetic, bool isUnlocked)
    {
        //TODO: Play SFX
        _selectedCosmetic = cosmetic;
        _cosmeticConfirmButton.text = isUnlocked ? "Done" : (cosmetic.priceGem > 0 ? $" Buy {cosmetic.priceGem} Gem" : $"Buy {cosmetic.priceGold} Gold");
        
        CosmeticPreviewed?.Invoke(cosmetic);
    }

    private void OnConfirmCosmeticClicked()
    {
        if(_selectedCosmetic == null || _config == null) return;
        bool isUnlocked = _config.IsCosmeticUnlocked(_selectedCosmetic);

        if (!isUnlocked)
        {
            bool purchased = _config.PurchaseCosmetic(_selectedCosmetic);
            if (!purchased) return;
        }
        
        _config.EquipCosmetic(_selectedCosmetic);
        PopulateSkinGrid();
    }

    private void HandleCosmeticEquipped(string cosmeticId)
    {
        if(IsCosmeticPanelOpen) PopulateSkinGrid();
    }

    public void ShowSettingsPanel()
    {
        if (_playerConfig == null)
        {
            var configMg = StartupProcessor.Instance?.GetService<ConfigManager>();
            if(configMg == null || !configMg.GetConfig(out _playerConfig)) return;
            SetupDropdowns();
        }
        
        LoadSettingsFromConfig();
        SelectSettingsTab(0);
        _settingsPanel.style.display = DisplayStyle.Flex;
    }

    public void HideSettingsPanel()
    {
        ApplySettingsToConfig();
        _settingsPanel.style.display = DisplayStyle.None;
    }

    private void SetupDropdowns()
    {
        _qualityDropdown.choices = new List<string>(QualitySettings.names);
        _fpsDropdown.choices = new List<string> { "30", "60", "120", "Unlimited" };
    }

    private void LoadSettingsFromConfig()
    {
        _qualityDropdown.index = Mathf.Clamp(_playerConfig.quality, 0, _qualityDropdown.choices.Count - 1);
        _fpsDropdown.index = _playerConfig.fps switch { 30 => 0, 60 => 1, 120 => 2, _ => 3 };
        _cameraShakeToggle.value = _playerConfig.activeCameraShake;
        _damageTextToggle.value = _playerConfig.showDamageText;
        _masterSlider.value = _playerConfig.masterVolume;
        _musicSlider.value = _playerConfig.musicVolume;
        _sfxSlider.value = _playerConfig.sfxVolume;
        
        _masterValue.text = $"{_playerConfig.masterVolume:P0}";
        _musicValue.text = $"{_playerConfig.musicVolume:P0}";
        _sfxValue.text = $"{_playerConfig.sfxVolume:P0}";
    }

    private void ApplySettingsToConfig()
    {
        _playerConfig.quality = _qualityDropdown.index;
        _playerConfig.fps = _fpsDropdown.index switch { 0 => 30, 1 => 60, 2 => 120, _ => -1 };
        _playerConfig.activeCameraShake = _cameraShakeToggle.value;
        _playerConfig.showDamageText = _damageTextToggle.value;
        _playerConfig.masterVolume = _masterSlider.value;
        _playerConfig.musicVolume = _musicSlider.value;
        _playerConfig.sfxVolume = _sfxSlider.value;

        QualitySettings.SetQualityLevel(_playerConfig.quality);
        Application.targetFrameRate = _playerConfig.fps;

        // Set Audio Mixer Volume
        SetAudioMixer("MasterVolume", _playerConfig.masterVolume);
        SetAudioMixer("MusicVolume", _playerConfig.musicVolume);
        SetAudioMixer("SFXVolume", _playerConfig.sfxVolume);
        
        Debug.Log($"[MainMenuScreen] Applied new settings");
    }

    private void SetAudioMixer(string param, float linearVolume)
    {
        if(mainAudioMixer == null) return;
        float db = Mathf.Log10(Mathf.Max(0.0001f, linearVolume)) * 20f;
        mainAudioMixer.SetFloat(param, db);
    }

    private void SelectSettingsTab(int index)
    {
        _tabVideo.EnableInClassList("main-menu__settings-tab--active", index == 0);
        _tabAudio.EnableInClassList("main-menu__settings-tab--active", index == 1);
        _tabGameplay.EnableInClassList("main-menu__settings-tab--active", index == 2);
        
        _pageVideo.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        _pageAudio.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        _pageGameplay.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
    }
}