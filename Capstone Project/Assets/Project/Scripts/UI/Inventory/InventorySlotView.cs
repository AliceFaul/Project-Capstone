using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public enum SlotKind { Equipped, Bag }

// One slot (equipped or in a bag). Button = keyboard navigation + click. Hover/focus -> Highlighted.
[RequireComponent(typeof(Button))]
public class InventorySlotView : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
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

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

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
            icon.color = hasSprite ? Color.white : color; // placeholder = flat rarity colour
        }

        int level = item is EquipmentInstance equipment ? equipment.level : 0;
        levelBadge.SetActive(level > 0);
        if (level > 0) levelText.text = level.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        highlight.SetActive(true);
        Highlighted?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData) => highlight.SetActive(false);
}
