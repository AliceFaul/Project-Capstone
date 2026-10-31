using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public enum SlotKind { Equipped, Bag }

// One slot (equipped or in a bag). Button = keyboard navigation + click.
// Hover shows the highlight only while the pointer is over the slot; keyboard focus keeps it until focus moves.
[RequireComponent(typeof(Button))]
public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image rarityStrip;
    [SerializeField] private GameObject levelBadge;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject hotkeyBadge;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private GameObject highlight;

    public SlotKind kind;
    public EquipSlot equipSlot;   // Equipped only
    public int artifactIndex;     // Equipped artifact slots only

    public IInventoryItem Item { get; private set; }
    public ItemData Definition { get; private set; }

    public event Action<InventorySlotView> Highlighted;
    public event Action<InventorySlotView> Clicked;

    private bool _pointerOver;
    private bool _pointerDriven;

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    private void OnDisable()
    {
        _pointerOver = false;
        _pointerDriven = false;
        highlight.SetActive(false);
    }

    public void Bind(IInventoryItem item, ItemData definition)
    {
        Item = item;
        Definition = definition;
        bool has = item != null;

        icon.enabled = has;
        rarityStrip.enabled = has;

        if (has)
        {
            var color = InventoryUIUtil.RarityColor(definition != null ? definition.rarity : Rarity.Common);
            rarityStrip.color = color;

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
        _pointerOver = true;

        var eventSystem = EventSystem.current;
        bool alreadySelected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
        if (eventSystem != null) eventSystem.SetSelectedGameObject(gameObject);
        _pointerDriven = true;

        if (alreadySelected) // OnSelect will not fire again
        {
            highlight.SetActive(true);
            Highlighted?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOver = false;
        if (_pointerDriven) highlight.SetActive(false); // selection stays so keyboard navigation can continue from here
    }

    public void OnSelect(BaseEventData eventData)
    {
        _pointerDriven = _pointerOver;
        highlight.SetActive(true);
        Highlighted?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData) => highlight.SetActive(false);
}