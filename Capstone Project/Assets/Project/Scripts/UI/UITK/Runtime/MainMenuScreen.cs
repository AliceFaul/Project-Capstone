using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class MainMenuScreen : MonoBehaviour
{
    [SerializeField] private CosmeticDatabase cosmeticDatabase;
    
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
    private VisualElement _settingsTabs;
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
    private bool _isNameMandatory;

    public event Action StartGameClicked;
    public event Action OptionClicked;
    public event Action QuitClicked;
    public event Action ChangeCosmeticClicked;
    public event Action CloseCosmeticClicked;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        
        QueryElements();
        WireButtons();

        // Deactivate main menu in first, enable when startup process completed
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
            if(_config.Currency != null) _config.Currency.OnCurrencyChanged -= HandleCurrency;
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
        _config.Currency.OnCurrencyChanged += HandleCurrency;
        
        Refresh();
    }

    private void Refresh()
    {
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
    public void SetPlayerName(string playerName) => _playerNameLabel.text = playerName;

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
        _selectedCosmetic = null;
        _cosmeticConfirmRow.style.display = DisplayStyle.None;
        PopulateSkinGrid();
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
            
        }
    }
}
