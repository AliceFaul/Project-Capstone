using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class MainMenuScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _characterBlock;
    private VisualElement _changeSkinPanel;
    private VisualElement _skinGrid;
 
    private IconLabelElement _levelDisplay;
    private IconLabelElement _goldDisplay;
    private IconLabelElement _gemDisplay;
    private Label _playerNameLabel;
 
    private Button _startGameButton;
    private Button _optionButton;
    private Button _quitButton;
    private Button _changeSkinButton;
    private Button _closeSkinButton;

    private PlayerDataConfig _config;

    public event Action StartGameClicked;
    public event Action OptionClicked;
    public event Action QuitClicked;
    public event Action ChangeSkinClicked;
    public event Action CloseSkinClicked;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        
        QueryElements();
        WireButtons();

        // Deactivate main menu in first, enable when startup process completed
        _root.style.display = DisplayStyle.None;
        _changeSkinPanel.style.display = DisplayStyle.None;
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
        _changeSkinPanel = _root.Q<VisualElement>("change-skin-panel");
        _skinGrid = _root.Q<VisualElement>("skin-grid");
 
        _levelDisplay = _root.Q<IconLabelElement>("level-display");
        _goldDisplay = _root.Q<IconLabelElement>("gold-display");
        _gemDisplay = _root.Q<IconLabelElement>("gem-display");
        _playerNameLabel = _root.Q<Label>("player-name-label");
 
        _startGameButton = _root.Q<Button>("start-game-button");
        _optionButton = _root.Q<Button>("option-button");
        _quitButton = _root.Q<Button>("quit-button");
        _changeSkinButton = _root.Q<Button>("change-skin-button");
        _closeSkinButton = _root.Q<Button>("close-skin-button");
    }

    private void WireButtons()
    {
        _startGameButton.clicked += () =>  StartGameClicked?.Invoke();
        _optionButton.clicked += () => OptionClicked?.Invoke();
        _quitButton.clicked += () => QuitClicked?.Invoke();
        _changeSkinButton.clicked += () => ChangeSkinClicked?.Invoke();
        _closeSkinButton.clicked += () => CloseSkinClicked?.Invoke();
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
        _levelDisplay.SetText($"Lv. {_config.Level}");
        _goldDisplay.SetAmount(_config.Currency.Gold());
        _gemDisplay.SetAmount(_config.Currency.Gem());
    }
    
    private void HandleLevelUp(int level) => _levelDisplay.SetText($"Lv. {level}");
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

    public void ToggleChangeSkinPanel(bool shown)
    {
        _changeSkinPanel.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        _startGameButton.style.display = shown ? DisplayStyle.None : DisplayStyle.Flex;
        _optionButton.style.display = shown ? DisplayStyle.None : DisplayStyle.Flex;
        _quitButton.style.display = shown ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void PopulateSkinGrid(IEnumerable<(string name, Sprite icon)> skins)
    {
        _skinGrid.Clear();

        foreach (var (item, icon) in skins)
        {
            var slot = new ItemSlotElement();
            slot.SetItem(item, icon, quantity: 1, isEquipped: false, rarityUssClass: null);
            _skinGrid.Add(slot);
        }
    }
}
