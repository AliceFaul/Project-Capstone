using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One possible reward of a level (item icon, rarity colored when it has no sprite). Hovering reports the item to the screen.
public class RewardSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject highlight;

    public ItemData Item { get; private set; }
    public event Action<RewardSlotView, bool> Hovered;

    public void Bind(ItemData item)
    {
        Item = item;
        bool hasSprite = item.icon != null;
        icon.sprite = hasSprite ? item.icon : null;
        icon.color = hasSprite ? Color.white : InventoryUIUtil.RarityColor(item.rarity);
    }

    private void OnDisable() => highlight.SetActive(false);

    public void OnPointerEnter(PointerEventData eventData)
    {
        highlight.SetActive(true);
        Hovered?.Invoke(this, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        highlight.SetActive(false);
        Hovered?.Invoke(this, false);
    }
}