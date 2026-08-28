using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryScreenController : MonoBehaviour
{
    private PlayerRuntime _playerRuntime;
    
    private VisualElement _root;
    private VisualElement _inventoryGrid;
    private VisualElement _detailStats;
    private VisualElement _detailIcon;
    private Label _detailRarity;
    private Label _detailName;
    private Label _detailDescription;
    private Label _detailEmptyHint;
    private Label _statDamage;
    private Label _statSpeed;
    private Label _statCritChance;
    private Label _statCritDamage;
    private Label _levelLabel;

    private IconLabelElement _goldDisplay;
    private IconLabelElement _gemDisplay;

    private ItemSlotElement _slotMelee;
    private ItemSlotElement _slotArmor;
    private ItemSlotElement _slotRanged;
    private ItemSlotElement[] _artifactSlots;
    private ItemSlotElement[] _runeSlots;
    private VisualElement _modelViewport;

    private ItemSlotElement _selectedSlot;

    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement;

        QueryElements();
        WireCloseButton();
        WireEquipSlotClicks();
        WireModelClick();

        _playerRuntime = FindFirstObjectByType<PlayerRuntime>();

        SubscribeEvents();
        ShowEmptyDetail();
        RefreshInventoryGrid();
        RefreshEquipSlots();
        RefreshPlayerHeader();

        // ===== TEST DATA - xoa 2 dong duoi khi noi voi he thong that =====
        /*PopulateTestInventory();
        SetCurrency(gold: 1250, gem: 34);
        SetLevel(7);*/
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        PlayerInventory.OnInventoryChanged += RefreshInventoryGrid;

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        }

        if (_playerRuntime != null)
        {
            _playerRuntime.OnExpChanged += HandleExpChanged;
            _playerRuntime.OnLevelUp += HandleLevelUpChanged;
            if (_playerRuntime.Currency != null)
            {
                _playerRuntime.Currency.OnCurrencyChanged += HandleCurrencyChanged;
            }
        }
    }

    private void UnsubscribeEvents()
    {
        PlayerInventory.OnInventoryChanged -= RefreshInventoryGrid;

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        if (_playerRuntime != null)
        {
            _playerRuntime.OnExpChanged -= HandleExpChanged;
            _playerRuntime.OnLevelUp -= HandleLevelUpChanged;
            if (_playerRuntime.Currency != null)
            {
                _playerRuntime.Currency.OnCurrencyChanged -= HandleCurrencyChanged;
            }
        }
    }

    private void QueryElements()
    {
        _inventoryGrid = _root.Q<VisualElement>("inventory-grid");
        _detailStats = _root.Q<VisualElement>("detail-stats");
        _detailIcon = _root.Q<VisualElement>("detail-icon");
        _detailRarity = _root.Q<Label>("detail-rarity");
        _detailName = _root.Q<Label>("detail-name");
        _detailDescription = _root.Q<Label>("detail-description");
        _detailEmptyHint = _root.Q<Label>("detail-empty-hint");
        _statDamage = _root.Q<Label>("stat-damage");
        _statSpeed = _root.Q<Label>("stat-speed");
        _statCritChance = _root.Q<Label>("stat-crit-chance");
        _statCritDamage = _root.Q<Label>("stat-crit-damage");
        _levelLabel = _root.Q<Label>("level-label");
        _modelViewport = _root.Q<VisualElement>("model-viewport");

        _goldDisplay = _root.Q<IconLabelElement>("gold-display");
        _gemDisplay = _root.Q<IconLabelElement>("gem-display");

        _slotMelee = _root.Q<ItemSlotElement>("slot-melee");
        _slotArmor = _root.Q<ItemSlotElement>("slot-armor");
        _slotRanged = _root.Q<ItemSlotElement>("slot-ranged");

        _artifactSlots = new[]
        {
            _root.Q<ItemSlotElement>("slot-artifact-0"),
            _root.Q<ItemSlotElement>("slot-artifact-1"),
            _root.Q<ItemSlotElement>("slot-artifact-2"),
        };

        _runeSlots = new[]
        {
            _root.Q<ItemSlotElement>("rune-slot-0"),
            _root.Q<ItemSlotElement>("rune-slot-1"),
            _root.Q<ItemSlotElement>("rune-slot-2"),
        };

        foreach (var slot in _runeSlots)
        {
            slot.SetSlotTypeLabel("Rune");
            slot.SetEmpty();
        }
    }

    private void WireCloseButton()
    {
        var closeButton = _root.Q<Button>("close-button");
        closeButton.clicked += Close;
    }

    private void WireEquipSlotClicks()
    {
        _slotMelee.SetSlotTypeLabel("Melee");
        _slotMelee.Clicked += SelectSlot;
        _slotMelee.DoubleClicked += _ => EquipmentManager.Instance?.Unequip(EquipmentType.MeleeWeapon);
        
        _slotArmor.SetSlotTypeLabel("Armor");
        _slotArmor.Clicked += SelectSlot;
        _slotArmor.DoubleClicked += _ => EquipmentManager.Instance?.Unequip(EquipmentType.Armor);
        
        _slotRanged.SetSlotTypeLabel("Ranged");
        _slotRanged.Clicked += SelectSlot;
        _slotRanged.DoubleClicked += _ => EquipmentManager.Instance?.Unequip(EquipmentType.RangedWeapon);

        foreach (var slot in _artifactSlots)
        {
            slot.SetSlotTypeLabel("Artifact");
            slot.Clicked += SelectSlot;
            slot.DoubleClicked += _ => EquipmentManager.Instance?.Unequip(EquipmentType.Artifact);
        }
    }

    private void WireModelClick()
    {
        // Model player chua phai VisualElement co san co the RegisterCallback<ClickEvent> binh
        // thuong - viewport nay tam thoi la 1 khung trong, dang cho ban quyet dinh dung
        // RenderTexture hay cach nao khac. Van dang ky click de test logic ShowPlayerStats().
        _modelViewport.RegisterCallback<ClickEvent>(_ => ShowPlayerStats());
    }

    public void Close()
        => gameObject.SetActive(false);

    private void RefreshPlayerHeader()
    {
        if (_playerRuntime == null)
        {
            Debug.LogError($"[InventoryScreenController] Player Runtime not found in scene!");
            return;
        }
        
        SetLevel(_playerRuntime.Level);

        if (_playerRuntime.Currency != null)
            SetCurrency(_playerRuntime.Currency.Gold(), _playerRuntime.Currency.Gem());
    }
    
    private void SetCurrency(int gold, int gem)
    {
        _goldDisplay.SetAmount(gold);
        _gemDisplay.SetAmount(gem);
    }

    private void SetLevel(int level)
    {
        _levelLabel.text = $"Lv. {level}";
    }

    private void HandleExpChanged(float current, float toNext)
    {
        // TODO: Update process EXP in UXML, need Painter2D
    }

    private void HandleLevelUpChanged(int level)
        => SetLevel(level);

    private void HandleCurrencyChanged(CurrencyType type, int amount)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                _goldDisplay.SetAmount(amount); break;
            case CurrencyType.Gem:
                _gemDisplay.SetAmount(amount); break;
        }
    }
    
    // ===================== EQUIPMENT =====================

    private void HandleEquipmentChanged(EquipmentChangedEventArgs args)
        => RefreshEquipSlots();
    
    private void RefreshEquipSlots()
    {
        var manager = EquipmentManager.Instance;
        if(manager == null)
            return;
        
        SetEquipSlot(_slotMelee, manager.Melee);
        SetEquipSlot(_slotArmor, manager.Armor);
        SetEquipSlot(_slotRanged, manager.Ranged);

        for (int i = 0; i < _artifactSlots.Length; i++)
        {
            var artifact = i < manager.Artifacts.Length ? manager.Artifacts[i] : null;
            SetEquipSlot(_artifactSlots[i], artifact);
        }
    }

    private void SetEquipSlot(ItemSlotElement slot, EquipmentData equipment)
    {
        if (equipment == null)
        {
            slot.SetEmpty();
            return;
        }
        
        slot.SetItem(equipment, equipment.icon, quantity: 1, isEquipped: true, GetItemRarity(equipment));
    }

    // ===================== INVENTORY GRID =====================

    public void RefreshInventoryGrid()
    {
        _inventoryGrid.Clear();

        if(PlayerInventory.Instance == null)
            return;

        foreach (var invSlot in PlayerInventory.Instance.slots)
        {
            if (invSlot.itemData == null)
                continue;

            var slot = new ItemSlotElement();
            slot.SetItem(invSlot, invSlot.itemData.icon, invSlot.stackSize, isEquipped: false, GetItemRarity(invSlot.itemData));
            slot.Clicked += SelectSlot;
            slot.DoubleClicked += OnGridSlotDoubleClicked;
            
            _inventoryGrid.Add(slot);
        }
    }

    private void OnGridSlotDoubleClicked(ItemSlotElement slot)
    {
        if (slot.BoundItem is not InventorySlot invSlot || invSlot.itemData == null)
            return;
        
        // Equip/Swap player equipment
        invSlot.itemData.Use();
    }

    // ===================== DETAIL PANEL =====================

    private void SelectSlot(ItemSlotElement slot)
    {
        if(_selectedSlot != null)
            _selectedSlot.SetSelected(false);
        
        _selectedSlot = slot;
        slot.SetSelected(true);
        
        ShowDetail(slot);
    }
    
    private void ShowDetail(ItemSlotElement slot)
    {
        ItemData item = slot.BoundItem switch
        {
            InventorySlot invSlot => invSlot.itemData,
            EquipmentData equipment => equipment,
            _ => null
        };

        if (item == null)
        {
            ShowEmptyDetail();
            return;
        }

        // _detailEmptyHint.style.display = DisplayStyle.None;

        _detailRarity.text = item.rarity.ToString();
        RemoveRarityBadge();
        _detailRarity.AddToClassList(GetItemRarity(item));
        
        _detailName.text = item.itemName;
        _detailDescription.text = item.itemDescription;
        _detailIcon.style.backgroundImage = item.icon != null ? new StyleBackground(item.icon) : default(StyleBackground);
        
        bool isEquipment = item.itemType == ItemType.Equipment;
        
        _detailStats.style.display = isEquipment ? DisplayStyle.Flex : DisplayStyle.None;

        if (isEquipment && item is EquipmentData equipmentData)
        {
            int totalDamageOrArmor = equipmentData.attributes.damage + equipmentData.damageModifier;
            float totalCritChance = equipmentData.attributes.critChance + equipmentData.critChanceModifier;
            float totalCritDamage = equipmentData.attributes.critDamage + equipmentData.critDamageModifier;
            
            _statDamage.text = $"Damage/Armor: {totalDamageOrArmor}";
            _statSpeed.text = equipmentData.equipmentType == EquipmentType.Armor ? "" : $"Speed: {equipmentData.attackSpeedModifier}";
            _statCritChance.text = $"Crit Chance: {totalCritChance}%";
            _statCritDamage.text = $"Crit Damage: {totalCritDamage}%";
        }
    }

    // Click vao model player - hien detail panel total stats cua player
    public void ShowPlayerStats()
    {
        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(false);
            _selectedSlot = null;
        }
        
        // _detailEmptyHint.style.display = DisplayStyle.None;
        RemoveRarityBadge();
        _detailRarity.text = "";
        _detailName.text = "TOTAL STATS";
        _detailDescription.text = "";
        _detailIcon.style.backgroundImage = default(StyleBackground);
        _detailStats.style.display = DisplayStyle.Flex;

        if (_playerRuntime == null) 
            return;
        
        _statDamage.text = $"Damage: {_playerRuntime.TotalDamage}";
        _statSpeed.text = $"Speed: {_playerRuntime.TotalSpeed:0.0}";
        _statCritChance.text = $"Crit Chance: {_playerRuntime.TotalCritChance}%";
        _statCritDamage.text = $"Crit Damage: {_playerRuntime.TotalCritDamage}%";
    }

    private void ShowEmptyDetail()
    {
        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(false);
            _selectedSlot = null;
        }
        
        RemoveRarityBadge();
        _detailRarity.text = "";
        _detailName.text = "";
        _detailDescription.text = "";
        _detailIcon.style.backgroundImage = default(StyleBackground);
        _detailStats.style.display = DisplayStyle.None;
        //_detailEmptyHint.style.display = DisplayStyle.Flex;
    }

    private void RemoveRarityBadge()
    {
        _detailRarity.RemoveFromClassList("item-slot--rarity-common");
        _detailRarity.RemoveFromClassList("item-slot--rarity-uncommon");
        _detailRarity.RemoveFromClassList("item-slot--rarity-rare");
        _detailRarity.RemoveFromClassList("item-slot--rarity-legendary");
    }

    private string GetItemRarity(ItemData item)
    {
        switch (item.rarity)
        {
            case Rarity.Common: return "item-slot--rarity-common";
            case Rarity.Uncommon: return "item-slot--rarity-uncommon";
            case Rarity.Rare: return "item-slot--rarity-rare";
            case Rarity.Legendary: return "item-slot--rarity-legendary";
            default: return "item-slot--rarity-common";
        }
    }

    // ===================== TEST DATA =====================

    private void PopulateTestInventory()
    {
        var testItems = new List<InventoryTestItem>
        {
            new InventoryTestItem
            {
                itemName = "Iron Sword", description = "Mot thanh kiem sat co ban.",
                rarityLabel = "Common", rarityUssClass = "item-slot--rarity-common",
                quantity = 1, isEquipment = true, damageOrArmor = 12,
                hasSpeed = true, speed = 1.0f, critChance = 5, critDamage = 150
            },
            new InventoryTestItem
            {
                itemName = "Health Potion", description = "Hoi 30% mau khi su dung.",
                rarityLabel = "Common", rarityUssClass = "item-slot--rarity-common",
                quantity = 5, isEquipment = false
            },
            new InventoryTestItem
            {
                itemName = "Dragon Scale Armor", description = "Giap lam tu vay rong, khang lua cao.",
                rarityLabel = "Epic", rarityUssClass = "item-slot--rarity-epic",
                quantity = 1, isEquipment = true, damageOrArmor = 40
            },
            new InventoryTestItem
            {
                itemName = "Fighter's Bindings", description = "Gang tay danh nhanh, cham 2 muc tieu.",
                rarityLabel = "Legendary", rarityUssClass = "item-slot--rarity-legendary",
                quantity = 1, isEquipment = true, damageOrArmor = 8,
                hasSpeed = true, speed = 1.6f, critChance = 15, critDamage = 200
            },
        };

        //RefreshInventoryGrid(testItems);
    }
}

// Du lieu TAM cho muc dich test - thay bang ItemInstance/EquipmentData that cua ban khi tich hop.
[System.Serializable]
public class InventoryTestItem
{
    public string itemName;
    public string description;
    public string rarityLabel;
    public string rarityUssClass;
    public int quantity = 1;
    public Sprite icon;
    public bool isEquipment;
    public int damageOrArmor;
    public bool hasSpeed;
    public float speed;
    public int critChance;
    public int critDamage;
}
