using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;

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
 
    
    private PlayerDataConfig _config;
    private CosmeticData _selectedCosmetic;

    public event Action StartGameClicked;
    public event Action OptionClicked;
    public event Action QuitClicked;
    public event Action ChangeCosmeticClicked;
    public event Action CloseCosmeticClicked;
    public event Action<CosmeticData> CosmeticPreviewed;
    public event Action<CosmeticData> CosmeticSelected;
    public event Action CosmeticPreviewCanceled;
    public event Action PlayerNameClicked;
    
    public bool IsCosmeticPanelOpen => _changeCosmeticPanel?.resolvedStyle.display == DisplayStyle.Flex;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        QueryElements();
        WireButtons();
        UiAudio.Attach(_root);
    }

    private void OnEnable()
    {
        _root.style.display = DisplayStyle.None;
        _changeCosmeticPanel.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        UnbindData();
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
        
        _config.OnDisplayNameChanged += SetPlayerName;
        _config.OnDataApplied += Refresh;
        if(_config.Currency != null) _config.Currency.OnCurrencyChanged += HandleCurrency;
        Debug.Log($"[MainMenuScreen] Bind data successfully.");
        
        Refresh();
    }

    private void UnbindData()
    {
        if (_config == null) return;
        _config.OnDataApplied -= Refresh;
        _config.OnDisplayNameChanged -= SetPlayerName;
        if (_config.Currency != null) _config.Currency.OnCurrencyChanged -= HandleCurrency;
        
        _config = null;
    }

    private void Refresh()
    {
        if(_config == null) return;   
        _levelDisplay.SetText($"{_config.Level}");
        _goldDisplay.SetAmount(_config.Currency.Gold());
        _gemDisplay.SetAmount(_config.Currency.Gem());
        _playerNameLabel.text = string.IsNullOrEmpty(_config.DisplayName) ? "Unknown" : _config.DisplayName;
    }

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
    private void SetPlayerName(string playerName) => _playerNameLabel.text = playerName;

    public void Show()
    {
        if(_config == null) BindData();
        _root.style.display = DisplayStyle.Flex;
    }
    
    public void Hide() => _root.style.display = DisplayStyle.None;

    public void CleanupOnLogout()
    {
        UnbindData();
        _root.style.display = DisplayStyle.None;
    }

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
            slot.AddToClassList(UiAudio.HoverClass);
            slot.SetItem(cosmetic, cosmetic.cosmeticIcon, quantity: 1, isEquipped: isEquipped, rarityUssClass: null);
            
            if(isUnlocked) slot.SetUnlocked();
            else slot.SetLocked(cosmetic.priceGem > 0 ? $"{cosmetic.priceGem} Gem" : $"{cosmetic.priceGold} Gold");
            
            slot.Clicked += _ => OnCosmeticSlotClicked(cosmetic, isUnlocked);
            _cosmeticGrid.Add(slot);
            
            if (isEquipped && _selectedCosmetic == null) OnCosmeticSlotClicked(cosmetic, isUnlocked);
        }
        
        Debug.Log($"[MainMenuScreen] Cosmetic Grid loaded!");
    }

    private void SelectCosmetic(CosmeticData cosmetic, bool isUnlocked)
    {
        _selectedCosmetic = cosmetic;
        _cosmeticConfirmButton.text = isUnlocked ? "Done" : (cosmetic.priceGem > 0 ? $" Buy {cosmetic.priceGem} Gem" : $"Buy {cosmetic.priceGold} Gold");
        CosmeticPreviewed?.Invoke(cosmetic);
    }

    private void OnCosmeticSlotClicked(CosmeticData cosmetic, bool isUnlocked)
    {
        AudioService.Instance?.PlayUiClick();
        SelectCosmetic(cosmetic, isUnlocked);
    }

    private void OnConfirmCosmeticClicked()
    {
        AudioService.Instance?.PlayUiClick();
        
        if(_selectedCosmetic == null || _config == null) return;
        bool isUnlocked = _config.IsCosmeticUnlocked(_selectedCosmetic);

        if (!isUnlocked)
        {
            bool purchased = _config.PurchaseCosmetic(_selectedCosmetic);
            if (!purchased) return;
        }
        
        _config.EquipCosmetic(_selectedCosmetic);
        CosmeticSelected?.Invoke(_selectedCosmetic);
        
        PopulateSkinGrid();
    }
}