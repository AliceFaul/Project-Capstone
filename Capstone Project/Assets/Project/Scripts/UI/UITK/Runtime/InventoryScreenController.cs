using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Script demo/khoi dau cho man Inventory + Equipment - dung DU LIEU TEST de ban thay UI hoat
// dong ngay trong Play Mode. Tim phan "TEST DATA" o cuoi file, xoa/thay bang he thong
// Inventory/EquipmentManager THAT cua ban khi san sang (goi RefreshInventoryGrid voi data that).
[RequireComponent(typeof(UIDocument))]
public class InventoryScreenController : MonoBehaviour
{
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

    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement;

        QueryElements();
        WireEquipSlotLabels();
        WireCloseButton();
        WireEquipSlotClicks();
        WireModelClick();

        ShowEmptyDetail();

        // ===== TEST DATA - xoa 2 dong duoi khi noi voi he thong that =====
        PopulateTestInventory();
        SetCurrency(gold: 1250, gem: 34);
        SetLevel(7);
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
    }

    private void WireEquipSlotLabels()
    {
        _slotMelee.SetSlotTypeLabel("Melee");
        _slotMelee.SetEmpty();

        _slotArmor.SetSlotTypeLabel("Armor");
        _slotArmor.SetEmpty();

        _slotRanged.SetSlotTypeLabel("Ranged");
        _slotRanged.SetEmpty();

        foreach (var slot in _artifactSlots)
        {
            slot.SetSlotTypeLabel("Artifact");
            slot.SetEmpty();
        }

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
        _slotMelee.Clicked += ShowDetail;
        _slotArmor.Clicked += ShowDetail;
        _slotRanged.Clicked += ShowDetail;

        foreach (var slot in _artifactSlots)
            slot.Clicked += ShowDetail;
    }

    private void WireModelClick()
    {
        // Model player chua phai VisualElement co san co the RegisterCallback<ClickEvent> binh
        // thuong - viewport nay tam thoi la 1 khung trong, dang cho ban quyet dinh dung
        // RenderTexture hay cach nao khac. Van dang ky click de test logic ShowPlayerStats().
        _modelViewport.RegisterCallback<ClickEvent>(_ => ShowPlayerStats());
    }

    public void Close()
    {
        // Doi thanh cach ban dang quan ly mo/dong man hinh (vi du UIManager.CloseScreen(this))
        gameObject.SetActive(false);
    }

    public void SetCurrency(int gold, int gem)
    {
        _goldDisplay.SetAmount(gold);
        _gemDisplay.SetAmount(gem);
    }

    public void SetLevel(int level)
    {
        _levelLabel.text = $"Lv. {level}";
    }

    // ===================== INVENTORY GRID =====================

    // Goi ham nay voi danh sach item THAT khi tich hop voi InventorySystem cua ban - thay
    // InventoryTestItem bang kieu ItemInstance thuc te.
    public void RefreshInventoryGrid(IReadOnlyList<InventoryTestItem> items)
    {
        _inventoryGrid.Clear();

        foreach (var item in items)
        {
            var slot = new ItemSlotElement();
            slot.SetItem(item, item.icon, item.quantity, isEquipped: false, item.rarityUssClass);
            slot.Clicked += ShowDetail;
            slot.DoubleClicked += OnGridSlotDoubleClicked;

            _inventoryGrid.Add(slot);
        }
    }

    private void OnGridSlotDoubleClicked(ItemSlotElement slot)
    {
        if (slot.BoundItem is not InventoryTestItem item)
            return;

        // TODO: thay bang logic that - kiem tra item.equipmentType, tim equip slot tuong ung
        // (Melee/Armor/Ranged), neu slot do dang co item thi swap, khong thi equip thang vao.
        Debug.Log($"[InventoryScreen] Equip/Swap requested: {item.itemName}");
    }

    // ===================== DETAIL PANEL =====================

    private void ShowDetail(ItemSlotElement slot)
    {
        if (slot.BoundItem is not InventoryTestItem item)
        {
            ShowEmptyDetail();
            return;
        }

        _detailEmptyHint.style.display = DisplayStyle.None;

        _detailRarity.text = item.rarityLabel;
        _detailName.text = item.itemName;
        _detailDescription.text = item.description;
        _detailIcon.style.backgroundImage = item.icon != null ? new StyleBackground(item.icon) : default(StyleBackground);

        _detailStats.style.display = item.isEquipment ? DisplayStyle.Flex : DisplayStyle.None;

        if (item.isEquipment)
        {
            _statDamage.text = $"Damage/Armor: {item.damageOrArmor}";
            _statSpeed.text = item.hasSpeed ? $"Speed: {item.speed}" : "";
            _statCritChance.text = $"Crit Chance: {item.critChance}%";
            _statCritDamage.text = $"Crit Damage: {item.critDamage}%";
        }
    }

    // Goi ham nay khi nguoi choi click vao model player - doi detail panel sang hien stat tong
    // cua nhan vat thay vi 1 item cu the.
    public void ShowPlayerStats()
    {
        _detailEmptyHint.style.display = DisplayStyle.None;
        _detailRarity.text = "";
        _detailName.text = "Player Stats";
        _detailDescription.text = "";
        _detailIcon.style.backgroundImage = default(StyleBackground);
        _detailStats.style.display = DisplayStyle.Flex;

        // TODO: noi voi PlayerRuntime that, vi du:
        // _statDamage.text = $"Damage: {playerRuntime.TotalDamage}";
        // _statSpeed.text = $"Speed: {playerRuntime.TotalSpeed}";
        // _statCritChance.text = $"Crit Chance: {playerRuntime.TotalCritChance}%";
        // _statCritDamage.text = $"Crit Damage: {playerRuntime.TotalCritDamage}%";
    }

    private void ShowEmptyDetail()
    {
        _detailRarity.text = "";
        _detailName.text = "";
        _detailDescription.text = "";
        _detailIcon.style.backgroundImage = default(StyleBackground);
        _detailStats.style.display = DisplayStyle.None;
        _detailEmptyHint.style.display = DisplayStyle.Flex;
    }

    // ===================== TEST DATA (xoa khi tich hop that) =====================

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

        RefreshInventoryGrid(testItems);
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
