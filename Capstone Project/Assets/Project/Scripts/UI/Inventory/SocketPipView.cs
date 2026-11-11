using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project.Capstone.Inventory;

public enum PipState { Hidden, Locked, Empty, Filled }

[RequireComponent(typeof(Button))]
public class SocketPipView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image fill;
    [SerializeField] private GameObject highlight;
    [SerializeField] private CanvasGroup group;

    public EquipSlot weaponSlot;
    public int socketIndex;

    public event Action<SocketPipView> Highlighted;
    public event Action<SocketPipView> Clicked;

    private bool _pointerOver;
    private bool _pointerDriven;

    private void Awake() => GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));

    private void OnDisable()
    {
        _pointerOver = false;
        _pointerDriven = false;
        highlight.SetActive(false);
    }

    public void SetState(PipState state, Color color)
    {
        gameObject.SetActive(state != PipState.Hidden);
        if (state == PipState.Hidden) return;

        fill.color = state == PipState.Filled ? color : new Color(0.08f, 0.17f, 0.23f, 1f);
        group.alpha = state == PipState.Locked ? 0.45f : 1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerOver = true;

        var eventSystem = EventSystem.current;
        bool alreadySelected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
        if (eventSystem != null) eventSystem.SetSelectedGameObject(gameObject);
        _pointerDriven = true;

        if (alreadySelected)
        {
            highlight.SetActive(true);
            Highlighted?.Invoke(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerOver = false;
        if (_pointerDriven) highlight.SetActive(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        _pointerDriven = _pointerOver;
        highlight.SetActive(true);
        Highlighted?.Invoke(this);
    }

    public void OnDeselect(BaseEventData eventData) => highlight.SetActive(false);
}