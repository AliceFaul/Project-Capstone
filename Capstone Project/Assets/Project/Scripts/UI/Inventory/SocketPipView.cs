using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public enum PipState { Hidden, Locked, Empty, Filled }

// One rune socket marker under a weapon slot. Hover/focus -> detail, click -> insert/remove flow (handled by InventoryScreenView).
[RequireComponent(typeof(Button))]
public class SocketPipView : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image fill;
    [SerializeField] private GameObject highlight;
    [SerializeField] private CanvasGroup group;

    public EquipSlot weaponSlot;
    public int socketIndex;

    public event Action<SocketPipView> Highlighted;
    public event Action<SocketPipView> Clicked;

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    public void SetState(PipState state, Color color)
    {
        gameObject.SetActive(state != PipState.Hidden);
        if (state == PipState.Hidden) return;

        fill.color = state == PipState.Filled ? color : new Color(0.08f, 0.17f, 0.23f, 1f);
        group.alpha = state == PipState.Locked ? 0.45f : 1f;
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
