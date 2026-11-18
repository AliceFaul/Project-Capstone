using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

[RequireComponent(typeof(Button))]
public class ShopTileView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static readonly Color PriceOk = Color.white;
    private static readonly Color PriceNo = new Color(1f, 0.6f, 0.54f);
    private static readonly Color PriceSold = new Color(0.62f, 0.71f, 0.75f);

    [SerializeField] private Image icon;
    [SerializeField] private Image strip;
    [SerializeField] private Image rarityTint;
    
    [Tooltip("Melee, Ranged, Armor, Rune, Artifact. Used only when the item's own icon is missing.")]
    [SerializeField] private Sprite[] categoryFallbackIcons;
    [SerializeField] private GameObject coin;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject highlight;
    [SerializeField] private CanvasGroup group;

    public ShopOffer Offer { get; private set; }
    public event Action<ShopTileView> Clicked;
    public event Action<ShopTileView> Focused;

    private bool _hover;
    private bool _focus;
    private bool _selected;

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    private void OnDisable()
    {
        _hover = false;
        _focus = false;
        _selected = false;
        RefreshHighlight();
    }

    public void Bind(ShopOffer offer, bool canAfford)
    {
        ShopText.RegisterDefaults();
        Offer = offer;
        if (offer == null)
        {
            if (icon != null) icon.enabled = false;
            if (strip != null) strip.enabled = false;
            if (rarityTint != null) rarityTint.enabled = false;
            if (coin != null) coin.SetActive(false);
            if (priceText != null) priceText.text = "";
            SetSelected(false);
            return;
        }

        var rarity = offer.Definition != null ? offer.Definition.rarity : Rarity.Common;
        var color = InventoryUIUtil.RarityColor(rarity);
        strip.enabled = true;
        strip.color = color;
        if (rarityTint != null)
        {
            rarityTint.enabled = true;
            rarityTint.color = new Color(color.r, color.g, color.b, .18f);
        }

        bool hasSprite = offer.Definition != null && offer.Definition.icon != null;
        icon.sprite = hasSprite ? offer.Definition.icon : FallbackFor(offer);
        icon.enabled = icon.sprite != null;
        icon.color = hasSprite ? Color.white : new Color(1, 1, 1, .65f);

        coin.SetActive(!offer.Sold);
        priceText.text = offer.Sold ? InventoryText.Get("SHOP_SOLD") : offer.Price.ToString("N0");
        priceText.color = offer.Sold ? PriceSold : (canAfford ? PriceOk : PriceNo);
        group.alpha = offer.Sold ? 0.5f : 1f;
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        RefreshHighlight();
    }

    private Sprite FallbackFor(ShopOffer offer)
    {
        if (offer.Item == null || categoryFallbackIcons == null) return null;
        int index;
        switch (offer.Item.Category)
        {
            case InventoryCategory.Melee: index = 0; break;
            case InventoryCategory.Ranged: index = 1; break;
            case InventoryCategory.Armor: index = 2; break;
            case InventoryCategory.Rune: index = 3; break;
            case InventoryCategory.Artifact: index = 4; break;
            default: return null;
        }
        return index < categoryFallbackIcons.Length ? categoryFallbackIcons[index] : null;
    }

    private void RefreshHighlight()
    {
        if (highlight == null) return;
        highlight.SetActive(_selected || _hover || _focus);
        var image = highlight.GetComponent<Image>();
        if (image != null) image.color = _selected ? new Color(1, .78f, .32f, 1) : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData) { _hover = true; RefreshHighlight(); }
    public void OnPointerExit(PointerEventData eventData) { _hover = false; RefreshHighlight(); }
    public void OnSelect(BaseEventData eventData) { _focus = true; RefreshHighlight(); Focused?.Invoke(this); }
    public void OnDeselect(BaseEventData eventData) { _focus = false; RefreshHighlight(); }
}
