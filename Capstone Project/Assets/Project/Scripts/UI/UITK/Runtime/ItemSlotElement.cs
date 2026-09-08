using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 1 o slot dung chung cho Inventory grid VA Upgrade screen (list equipment dang mac).
// Xuat hien san trong thu vien UI Builder (nho UxmlFactory) - keo tha truc tiep vao layout,
// khong can code them gi de dung o man khac.
public class ItemSlotElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<ItemSlotElement, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits { }

    private readonly VisualElement _iconImage;
    private readonly Label _quantityLabel;
    private readonly VisualElement _equippedBadge;
    private readonly VisualElement _rarityRibbon;
    private readonly Label _typeLabel;
    private string _currentRarityClass;
    private bool _isSelected;

    // Click luon ban (dung de hien Detail Panel). DoubleClicked chi ban them khi click 2 lan lien
    // tiep (Unity tinh san clickCount trong ClickEvent theo double-click-time cua he thong) -
    // dung de trigger swap/equip theo dung logic ban mo ta.
    public event Action<ItemSlotElement> Clicked;
    public event Action<ItemSlotElement> DoubleClicked;

    // Item dang gan cho slot nay - de kieu object tam thoi, thay bang ItemInstance/EquipmentData
    // thuc te cua ban khi tich hop (xem SetItem).
    public object BoundItem { get; private set; }

    public ItemSlotElement()
    {
        AddToClassList("item-slot");

        _iconImage = new VisualElement();
        _iconImage.AddToClassList("item-slot__icon");
        Add(_iconImage);

        // Hien khi slot dang TRONG - vi du "Melee", "Armor", "Artifact"... An di ngay khi co item.
        _typeLabel = new Label();
        _typeLabel.AddToClassList("item-slot__type-label");
        _typeLabel.style.display = DisplayStyle.None;
        Add(_typeLabel);

        // Dai mau goc tren-trai the hien do hiem, giong style Minecraft Dungeons - an khi slot
        // trong hoac khi item chua co rarity gan (xem SetRarity).
        _rarityRibbon = new VisualElement();
        _rarityRibbon.AddToClassList("item-slot__rarity-ribbon");
        _rarityRibbon.style.display = DisplayStyle.None;
        Add(_rarityRibbon);

        _quantityLabel = new Label();
        _quantityLabel.AddToClassList("item-slot__quantity");
        _quantityLabel.style.display = DisplayStyle.None;
        Add(_quantityLabel);

        _equippedBadge = new VisualElement();
        _equippedBadge.AddToClassList("item-slot__equipped-badge");
        _equippedBadge.style.display = DisplayStyle.None;
        Add(_equippedBadge);

        RegisterCallback<ClickEvent>(OnClick);
    }

    // Vien xanh sang khi slot dang duoc chon (giong o "Pickaxe"/"Sword" vien xanh trong anh mau) -
    // goi tu ben ngoai (InventoryScreenController) khi 1 slot duoc click, va SetSelected(false)
    // cho slot duoc chon truoc do.
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        EnableInClassList("item-slot--selected", selected);
    }

    // Goi 1 lan luc setup slot (Melee/Armor/Ranged/Artifact/Rune) - text nay se tu an di khi
    // SetItem() duoc goi va tu hien lai khi SetEmpty() duoc goi.
    public void SetSlotTypeLabel(string label)
    {
        _typeLabel.text = label;
    }

    private void OnClick(ClickEvent evt)
    {
        Clicked?.Invoke(this);
        if (evt.clickCount == 2)
        {
            DoubleClicked?.Invoke(this);
        }
    }

    // Goi ham nay khi bind data thuc te (thay tham so 'object item' bang kieu ItemInstance cua ban).
    public void SetItem(object item, Sprite icon, int quantity, bool isEquipped, string rarityUssClass)
    {
        BoundItem = item;
        SetIcon(icon);
        SetQuantity(quantity);
        SetEquipped(isEquipped);
        SetRarity(rarityUssClass);
        _typeLabel.style.display = DisplayStyle.None;
    }

    public void SetEmpty()
    {
        BoundItem = null;
        _iconImage.style.backgroundImage = default(StyleBackground);
        _quantityLabel.style.display = DisplayStyle.None;
        _equippedBadge.style.display = DisplayStyle.None;
        ClearRarity();
        if (!string.IsNullOrEmpty(_typeLabel.text))
        {
            _typeLabel.style.display = DisplayStyle.Flex;
        }
    }

    public void SetIcon(Sprite icon)
    {
        _iconImage.style.backgroundImage = icon != null ? new StyleBackground(icon) : default(StyleBackground);
    }

    public void SetQuantity(int amount)
    {
        bool show = amount > 1;
        _quantityLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        _quantityLabel.text = amount.ToString();
    }

    public void SetEquipped(bool isEquipped)
    {
        _equippedBadge.style.display = isEquipped ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // rarityUssClass vi du: "item-slot--rarity-common", "item-slot--rarity-legendary"...
    // Dinh nghia mau vien + mau dai ribbon goc tuong ung trong USS.
    public void SetRarity(string rarityUssClass)
    {
        ClearRarity();
        if (string.IsNullOrEmpty(rarityUssClass))
            return;

        _currentRarityClass = rarityUssClass;
        AddToClassList(rarityUssClass);
        _rarityRibbon.AddToClassList(rarityUssClass + "-ribbon");
        _rarityRibbon.style.display = DisplayStyle.Flex;
    }

    private void ClearRarity()
    {
        if (!string.IsNullOrEmpty(_currentRarityClass))
        {
            RemoveFromClassList(_currentRarityClass);
            _rarityRibbon.RemoveFromClassList(_currentRarityClass + "-ribbon");
            _rarityRibbon.style.display = DisplayStyle.None;
            _currentRarityClass = null;
        }
    }
}
