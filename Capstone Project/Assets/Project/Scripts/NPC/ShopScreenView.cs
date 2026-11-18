using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public class ShopScreenView : MonoBehaviour
{
    private enum Tab { Buy, Sell }

    private static readonly InventoryCategory[] SellCategories =
    {
        InventoryCategory.Melee, InventoryCategory.Ranged, InventoryCategory.Armor, InventoryCategory.Rune, InventoryCategory.Artifact
    };

    [Header("Root")]
    [SerializeField] private GameObject screenRoot;
    [SerializeField] private Button backButton;

    [Header("Top bar")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private GameObject buyUnderline;
    [SerializeField] private GameObject sellUnderline;

    [Header("Buy tab")]
    [SerializeField] private GameObject buyPanel;
    [SerializeField] private ShopTileView[] tiles;
    [SerializeField] private TMP_Text stockCountText;

    [Header("Sell tab")]
    [SerializeField] private GameObject sellPanel;
    [SerializeField] private Button[] categoryButtons;
    [SerializeField] private GameObject[] categoryUnderlines;
    [SerializeField] private Transform bagContent;
    [SerializeField] private InventorySlotView bagSlotPrefab;
    [SerializeField] private RectTransform selectionFrame;
    [SerializeField] private TMP_Text sellCountText;

    [Header("Detail card")]
    [SerializeField] private InventoryDetailView detail;
    [SerializeField] private GameObject actionGroup;
    [SerializeField] private TMP_Text priceLabel;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private TMP_Text noteText;

    [Header("Merchant box + messages")]
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text toastText;

    private ShopService _shop;
    private ShopSceneDirector _director;
    private PlayerDataConfig _data;
    private Tab _tab;
    private int _sellCategory = 3; // Rune: that is what a merchant mostly sells
    private ShopOffer _selectedOffer;
    private IInventoryItem _selectedItem;
    private bool _armedSell;
    private float _armedUntil;
    private float _toastUntil;
    private bool _subscribed;
    private readonly List<InventorySlotView> _bagViews = new List<InventorySlotView>();
    private readonly List<TMP_Text> _bagPriceTexts = new List<TMP_Text>();
    private Coroutine _focusRoutine;
    private ScrollRect _stockScroll;
    private ScrollRect _sellScroll;

    private void Awake()
    {
        ShopText.RegisterDefaults();
        foreach (var tile in tiles)
        {
            tile.Clicked += OnTileClicked;
            tile.Focused += OnTileFocused;
        }
        _stockScroll = buyPanel.GetComponentInChildren<ScrollRect>(true);
        _sellScroll = sellPanel.GetComponentInChildren<ScrollRect>(true);

        buyTabButton.onClick.AddListener(() => SetTab(Tab.Buy));
        sellTabButton.onClick.AddListener(() => SetTab(Tab.Sell));
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int index = i;
            categoryButtons[i].onClick.AddListener(() => SetSellCategory(index));
        }
        actionButton.onClick.AddListener(OnActionClicked);
        if (backButton != null) backButton.onClick.AddListener(CloseShop);

        screenRoot.SetActive(false);
    }

    private void OnEnable()
    {
        ShopSceneDirector.AnyOpened += OnShopOpened;
        ShopSceneDirector.AnyClosed += OnShopClosed;
    }

    private void OnDisable()
    {
        ShopSceneDirector.AnyOpened -= OnShopOpened;
        ShopSceneDirector.AnyClosed -= OnShopClosed;
        UnsubscribeData();
        CancelFocus();
        if (screenRoot != null) screenRoot.SetActive(false);
        _shop = null;
        _director = null;
    }

    private void OnShopOpened(ShopSceneDirector director, ShopService shop)
    {
        UnsubscribeData();
        _director = director;
        _shop = shop;
        _data = shop.Data;
        _selectedOffer = null;
        _selectedItem = null;
        _armedSell = false;

        SubscribeData();
        screenRoot.SetActive(true);
        toastText.gameObject.SetActive(false);
        SetTab(Tab.Buy);
    }

    private void OnShopClosed(ShopSceneDirector director)
    {
        if (director != _director) return;
        UnsubscribeData();
        CancelFocus();
        var eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null &&
            eventSystem.currentSelectedGameObject.transform.IsChildOf(screenRoot.transform))
            eventSystem.SetSelectedGameObject(null);
        screenRoot.SetActive(false);
        _shop = null;
        _data = null;
        _director = null;
    }

    private void CloseShop()
    {
        if (_director != null) _director.Close();
    }

    private void SubscribeData()
    {
        if (_subscribed || _data == null) return;
        _data.Currency.OnCurrencyChanged += OnCurrencyChanged;
        _data.Inventory.OnBagChanged += OnBagChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        _shop.Changed += RefreshAll;
        _subscribed = true;
    }

    private void UnsubscribeData()
    {
        if (!_subscribed) return;
        _subscribed = false;
        if (_shop != null) _shop.Changed -= RefreshAll;
        if (_data != null)
        {
            _data.Currency.OnCurrencyChanged -= OnCurrencyChanged;
            _data.Inventory.OnBagChanged -= OnBagChanged;
        }
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnCurrencyChanged(CurrencyType type, int amount) => RefreshAll();
    private void OnBagChanged(InventoryCategory category) { if (_tab == Tab.Sell) RefreshAll(); }
    private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        tipText.text = InventoryText.Get(_tab == Tab.Buy ? "SHOP_TIP_BUY" : "SHOP_TIP_SELL");
        RefreshAll();
    }

    private void Update()
    {
        if (_shop == null) return;

        if (toastText.gameObject.activeSelf && Time.unscaledTime > _toastUntil) toastText.gameObject.SetActive(false);
        if (_armedSell && Time.unscaledTime > _armedUntil)
        {
            _armedSell = false;
            RefreshDetail();
        }

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.qKey.wasPressedThisFrame) SetTab(_tab == Tab.Buy ? Tab.Sell : Tab.Buy);
    }

    private void SetTab(Tab tab)
    {
        _tab = tab;
        _selectedOffer = null;
        _selectedItem = null;
        _armedSell = false;

        buyPanel.SetActive(tab == Tab.Buy);
        sellPanel.SetActive(tab == Tab.Sell);
        buyUnderline.SetActive(tab == Tab.Buy);
        sellUnderline.SetActive(tab == Tab.Sell);
        tipText.text = InventoryText.Get(tab == Tab.Buy ? "SHOP_TIP_BUY" : "SHOP_TIP_SELL");

        RefreshAll();
        QueueFocus();
    }

    private void SetSellCategory(int index)
    {
        _sellCategory = Mathf.Clamp(index, 0, SellCategories.Length - 1);
        _selectedItem = null;
        _armedSell = false;
        RefreshAll();
        QueueFocus();
    }

    private void CancelFocus()
    {
        if (_focusRoutine == null) return;
        StopCoroutine(_focusRoutine);
        _focusRoutine = null;
    }

    private void QueueFocus()
    {
        CancelFocus();
        _focusRoutine = StartCoroutine(FocusFirst());
    }

    private IEnumerator FocusFirst()
    {
        yield return null;
        _focusRoutine = null;
        if (_shop == null || !screenRoot.activeInHierarchy) yield break;
        var eventSystem = EventSystem.current;
        if (eventSystem == null) yield break;

        if (_tab == Tab.Buy)
        {
            foreach (var tile in tiles)
            {
                if (!tile.gameObject.activeSelf || tile.Offer != _selectedOffer) continue;
                eventSystem.SetSelectedGameObject(tile.gameObject);
                EnsureVisible((RectTransform)tile.transform, _stockScroll);
                yield break;
            }
            foreach (var tile in tiles)
            {
                if (!tile.gameObject.activeSelf) continue;
                eventSystem.SetSelectedGameObject(tile.gameObject);
                EnsureVisible((RectTransform)tile.transform, _stockScroll);
                yield break;
            }
        }
        else
        {
            foreach (var view in _bagViews)
            {
                if (!view.gameObject.activeSelf || view.Item == null) continue;
                eventSystem.SetSelectedGameObject(view.gameObject);
                EnsureVisible((RectTransform)view.transform, _sellScroll);
                yield break;
            }
        }
        eventSystem.SetSelectedGameObject((_tab == Tab.Buy ? buyTabButton : sellTabButton).gameObject);
    }

    private void OnTileFocused(ShopTileView tile) => EnsureVisible((RectTransform)tile.transform, _stockScroll);
    private void OnBagHighlighted(InventorySlotView slot) => EnsureVisible((RectTransform)slot.transform, _sellScroll);

    private static void EnsureVisible(RectTransform target, ScrollRect scroll)
    {
        if (scroll == null || scroll.viewport == null || scroll.content == null || !target.gameObject.activeInHierarchy) return;
        Canvas.ForceUpdateCanvases();
        var viewport = scroll.viewport;
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
        float shift = 0;
        if (bounds.max.y > viewport.rect.yMax) shift = viewport.rect.yMax - bounds.max.y;
        else if (bounds.min.y < viewport.rect.yMin) shift = viewport.rect.yMin - bounds.min.y;
        if (Mathf.Abs(shift) < .01f) return;
        scroll.StopMovement();
        scroll.content.anchoredPosition += new Vector2(0, shift);
    }

    private void RefreshAll()
    {
        if (_shop == null || _data == null) return;

        goldText.text = _data.Currency.Gold().ToString("N0");
        gemText.text = _data.Currency.Gem().ToString("N0");

        if (_tab == Tab.Buy) RefreshTiles();
        else RebuildSellGrid();

        RefreshDetail();
    }

    private void RefreshTiles()
    {
        var offers = _shop.Offers;
        int gold = _data.Currency.Gold();

        if (_selectedOffer == null)
        {
            foreach (var offer in offers)
            {
                if (offer.Sold) continue;
                _selectedOffer = offer;
                break;
            }
        }

        int left = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            bool has = i < offers.Count;
            tiles[i].gameObject.SetActive(has);
            if (!has) continue;

            tiles[i].Bind(offers[i], gold >= offers[i].Price);
            tiles[i].SetSelected(offers[i] == _selectedOffer);
            if (!offers[i].Sold) left++;
        }
        stockCountText.text = InventoryText.Get("SHOP_LEFT", left, offers.Count);
    }

    private void RebuildSellGrid()
    {
        var slots = _data.Inventory.GetSlots(SellCategories[_sellCategory]);

        while (_bagViews.Count < slots.Count)
        {
            var view = Instantiate(bagSlotPrefab, bagContent);
            view.kind = SlotKind.Bag;
            view.Clicked += OnBagSlotClicked;
            view.Highlighted += OnBagHighlighted;
            _bagViews.Add(view);
            // This overlay belongs to ShopSellSlot, leaving InventorySlotView's level badge intact.
            var priceNode = view.transform.Find("SellPrice/Value");
            _bagPriceTexts.Add(priceNode != null ? priceNode.GetComponent<TMP_Text>() : null);
        }

        if (_selectedItem != null && !_shop.CanSell(_selectedItem))
        {
            _selectedItem = null;
            _armedSell = false;
        }

        int used = 0;
        for (int i = 0; i < _bagViews.Count; i++)
        {
            bool active = i < slots.Count;
            _bagViews[i].gameObject.SetActive(active);
            if (!active) continue;

            var item = slots[i];
            _bagViews[i].Bind(item, item != null ? _shop.DefinitionOf(item) : null);
            var price = _bagPriceTexts[i];
            if (price != null)
            {
                price.transform.parent.gameObject.SetActive(item != null);
                price.text = item != null ? _shop.GetSellPrice(item).ToString("N0") : "";
            }

            if (item == null) continue;
            used++;
            if (_selectedItem == null) _selectedItem = item;
        }

        sellCountText.text = $"{used} / {slots.Count}";
        for (int i = 0; i < categoryUnderlines.Length; i++) categoryUnderlines[i].SetActive(i == _sellCategory);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bagContent);
        PlaceSelectionFrame();
    }

    private void PlaceSelectionFrame()
    {
        bool shown = false;
        if (_selectedItem != null)
        {
            foreach (var view in _bagViews)
            {
                if (!view.gameObject.activeSelf || view.Item == null || view.Item.InstanceId != _selectedItem.InstanceId) continue;
                selectionFrame.gameObject.SetActive(true);
                selectionFrame.SetParent(view.transform, false);
                selectionFrame.anchorMin = selectionFrame.anchorMax = selectionFrame.pivot = new Vector2(.5f, .5f);
                selectionFrame.anchoredPosition = Vector2.zero;
                selectionFrame.sizeDelta = ((RectTransform)view.transform).rect.size + new Vector2(8, 8);
                selectionFrame.SetAsFirstSibling();
                shown = true;
                break;
            }
        }

        if (!shown)
        {
            selectionFrame.gameObject.SetActive(false);
            selectionFrame.SetParent(bagContent, false);
        }
    }

    private void RefreshDetail()
    {
        var equipment = EquipmentManager.Instance;

        if (_tab == Tab.Buy)
        {
            var offer = _selectedOffer;
            if (offer == null)
            {
                HideDetail();
                return;
            }

            detail.Show(offer.Item, offer.Definition, InventoryText.Get("SHOP_FOR_SALE"), equipment);

            bool canBuy = !offer.Sold && _data.Currency.Gold() >= offer.Price;
            ShowAction("SHOP_COST", offer.Price, offer.Sold ? "SHOP_SOLD" : "SHOP_PURCHASE", canBuy, "");
            return;
        }

        var item = _selectedItem;
        if (item == null)
        {
            HideDetail();
            return;
        }

        detail.Show(item, _shop.DefinitionOf(item), InventoryText.Get("DETAIL_IN_BAG"), equipment);
        int sellPrice = _shop.GetSellPrice(item);
        ShowAction("SHOP_SELL_PRICE", sellPrice, _armedSell ? "SHOP_CONFIRM_BUTTON" : "SHOP_SELL_BUTTON", _shop.CanSell(item) && sellPrice > 0,
            InventoryText.Get("SHOP_CONFIRM"));
    }

    private void HideDetail()
    {
        detail.Hide();
        actionGroup.SetActive(false);
    }

    private void ShowAction(string priceLabelKey, int price, string actionKey, bool interactable, string note)
    {
        actionGroup.SetActive(true);
        priceLabel.text = InventoryText.Get(priceLabelKey);
        priceText.text = price.ToString("N0");
        actionLabel.text = InventoryText.Get(actionKey);
        actionButton.interactable = interactable;
        noteText.text = note;
    }

    private void OnTileClicked(ShopTileView tile)
    {
        _selectedOffer = tile.Offer;
        RefreshTiles();
        RefreshDetail();
    }

    private void OnBagSlotClicked(InventorySlotView slot)
    {
        if (slot.Item == null) return;

        _selectedItem = slot.Item;
        _armedSell = false;
        PlaceSelectionFrame();
        RefreshDetail();
    }

    private void OnActionClicked()
    {
        if (_shop == null) return;

        if (_tab == Tab.Buy)
        {
            var offer = _selectedOffer;
            if (offer == null) return;

            var result = _shop.TryBuy(offer);
            Toast(BuyMessage(result, offer));
            RefreshAll();
            return;
        }

        var item = _selectedItem;
        if (item == null) return;

        if (!_armedSell)
        {
            _armedSell = true;
            _armedUntil = Time.unscaledTime + 3f;
            RefreshDetail();
            return;
        }

        _armedSell = false;
        var definition = _shop.DefinitionOf(item);
        var sold = _shop.TrySell(item, out int price);
        Toast(sold == ShopResult.Success
            ? InventoryText.Get("SHOP_SOLD_TOAST", definition != null ? definition.itemName : "?", price)
            : InventoryText.Get("SHOP_CANNOT_SELL"));

        _selectedItem = null;
        RefreshAll();
    }

    private static string BuyMessage(ShopResult result, ShopOffer offer)
    {
        switch (result)
        {
            case ShopResult.Success: return InventoryText.Get("SHOP_BOUGHT", offer.Definition != null ? offer.Definition.itemName : "?");
            case ShopResult.NotEnoughGold: return InventoryText.Get("SHOP_NOT_ENOUGH_GOLD");
            case ShopResult.BagFull: return InventoryText.Get("SHOP_BAG_FULL");
            case ShopResult.AlreadySold: return InventoryText.Get("SHOP_ALREADY_SOLD");
            default: return InventoryText.Get("TOAST_GENERIC_FAIL");
        }
    }

    private void Toast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        _toastUntil = Time.unscaledTime + 2.5f;
    }
}