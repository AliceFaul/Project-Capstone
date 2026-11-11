using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public enum SlotKind { Equipped, Bag }

[RequireComponent(typeof(Button))]
public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image emptyPlaceholder;
    [SerializeField] private Image rarityTint;
    [SerializeField] private GameObject equippedBadge;
    [SerializeField] private Image rarityStrip;
    [SerializeField] private GameObject levelBadge;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject hotkeyBadge;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private GameObject highlight;
    [SerializeField] private Color pinnedHighlightColor = new Color(1f, 0.78f, 0.32f, 1f);

    public SlotKind kind;
    public EquipSlot equipSlot;   // Equipped only
    public int artifactIndex;     // Equipped artifact slots only

    public IInventoryItem Item { get; private set; }
    public ItemData Definition { get; private set; }

    public event Action<InventorySlotView> Highlighted;
    public event Action<InventorySlotView> Clicked;
    public event Action<InventorySlotView> Submitted;

    private bool _pointerOver;
    private bool _pointerDriven;
    private bool _selected;
    private bool _pinned;
    private Button _button;
    private Image _highlightImage;
    private Color _normalHighlightColor;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(() => Clicked?.Invoke(this));
        if (highlight != null) _highlightImage = highlight.GetComponent<Image>();
        if (_highlightImage != null) _normalHighlightColor = _highlightImage.color;
        RefreshHighlight();
    }

    private void OnDisable()
    {
        _pointerOver = false;
        _pointerDriven = false;
        _selected = false;
        _pinned = false;
        RefreshHighlight();
    }

    public void Bind(IInventoryItem item, ItemData definition)
    {
        Item = item;
        Definition = definition;
        bool has = item != null;

        icon.enabled = has;
        if (emptyPlaceholder != null) emptyPlaceholder.enabled = !has && emptyPlaceholder.sprite != null;
        if (rarityTint != null) rarityTint.enabled = has;
        if (equippedBadge != null) equippedBadge.SetActive(has && kind == SlotKind.Equipped);
        rarityStrip.enabled = has;

        if (has)
        {
            var color = InventoryUIUtil.RarityColor(definition != null ? definition.rarity : Rarity.Common);
            rarityStrip.color = color;
            if (rarityTint != null) rarityTint.color = new Color(color.r, color.g, color.b, .18f);

            bool hasSprite = definition != null && definition.icon != null;
            icon.sprite = hasSprite ? definition.icon : null;
            icon.color = hasSprite ? Color.white : color;
        }

        int level = item is EquipmentInstance equipment ? equipment.level : 0;
        levelBadge.SetActive(level > 0);
        if (level > 0) levelText.text = level.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_button == null || !_button.IsInteractable()) return;
        _pointerOver = true;

        var eventSystem = EventSystem.current;
        bool alreadySelected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
        if (eventSystem != null) eventSystem.SetSelectedGameObject(gameObject);
        _pointerDriven = true;
        RefreshHighlight();

        if (alreadySelected) // OnSelect will not fire again
        {
            Highlighted?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOver = false;
        RefreshHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_button == null || !_button.IsInteractable()) return;
        _selected = true;
        _pointerDriven = _pointerOver;
        RefreshHighlight();
        Highlighted?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        RefreshHighlight();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (!isActiveAndEnabled || _button == null || !_button.IsInteractable()) return;
        Submitted?.Invoke(this);
    }

    public void SetPinned(bool pinned)
    {
        if (_pinned == pinned) return;
        _pinned = pinned;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (highlight == null) return;
        highlight.SetActive(_pinned || _pointerOver || (_selected && !_pointerDriven));
        if (_highlightImage != null)
            _highlightImage.color = _pinned ? pinnedHighlightColor : _normalHighlightColor;
    }
}
