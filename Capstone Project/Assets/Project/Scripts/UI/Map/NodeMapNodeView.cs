using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One node on the route map. NodeMapView sets its state; hovering or selecting it (mouse or keyboard) shows its info.
public class NodeMapNodeView : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    public enum State { Off, Visited, Current, Next }

    [SerializeField] private Image ring;
    [SerializeField] private Image body;
    [SerializeField] private Image icon;
    [SerializeField] private Image halo;
    [SerializeField] private Image selectRing;
    [SerializeField] private TMP_Text glyph;
    [SerializeField] private GameObject check;
    [SerializeField] private GameObject hereTag;
    [SerializeField] private TMP_Text hereText;
    [SerializeField] private CanvasGroup group;

    private static readonly Color VisitedRing = new Color(0.30f, 0.42f, 0.49f);
    private static readonly Color VisitedBody = new Color(0.04f, 0.10f, 0.14f);
    private static readonly Color Muted = new Color(0.62f, 0.71f, 0.75f);
    private static readonly Color NormalBody = new Color(0.05f, 0.13f, 0.19f);
    private static readonly Color CurrentBody = new Color(0.07f, 0.19f, 0.25f);

    public MapNode Node { get; private set; }
    public State CurrentState { get; private set; }
    public event Action<NodeMapNodeView> Highlighted;

    public void Setup(MapNode node, State state, NodeMapStyle.Entry style, float size, string hereLabel)
    {
        Node = node;
        CurrentState = state;
        ((RectTransform)transform).sizeDelta = new Vector2(size, size);

        Color tint = state == State.Visited ? Muted : style.color;
        ring.color = state == State.Visited ? VisitedRing : state == State.Current ? Color.white : style.color;
        body.color = state == State.Visited ? VisitedBody : state == State.Current ? CurrentBody : NormalBody;

        bool hasSprite = style.icon != null;
        icon.enabled = hasSprite;
        icon.sprite = style.icon;
        icon.color = tint;
        glyph.gameObject.SetActive(!hasSprite);
        glyph.text = style.glyph;
        glyph.color = tint;
        glyph.fontSize = size * 0.42f;

        halo.gameObject.SetActive(state == State.Current || state == State.Next);
        halo.color = state == State.Current ? new Color(1f, 1f, 1f, 0.18f) : new Color(style.color.r, style.color.g, style.color.b, 0.25f);

        check.SetActive(state == State.Visited);
        hereTag.SetActive(state == State.Current);
        hereText.text = hereLabel;
        group.alpha = state == State.Off ? 0.42f : 1f;
        selectRing.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected) => selectRing.gameObject.SetActive(selected);

    public void OnSelect(BaseEventData eventData) => Highlighted?.Invoke(this);
    public void OnPointerEnter(PointerEventData eventData) => Highlighted?.Invoke(this);
}