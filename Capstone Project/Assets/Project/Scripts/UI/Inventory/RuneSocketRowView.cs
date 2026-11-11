using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SocketRowState { Locked, Empty, Filled }

// One socket row inside the rune socket popup. Root Button = select this socket; RemoveButton = remove its rune.
[RequireComponent(typeof(Button))]
public class RuneSocketRowView : MonoBehaviour
{
    private static readonly Color Border = new Color(0.145f, 0.275f, 0.353f, 1f);
    private static readonly Color SelectedBorder = new Color(0.94f, 0.50f, 0.16f, 1f);
    private static readonly Color EmptyIcon = new Color(0.055f, 0.133f, 0.188f, 1f);

    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text iconText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button removeButton;
    [SerializeField] private TMP_Text removeLabel;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private CanvasGroup group;

    [Header("Visual icons")]
    [SerializeField] private Image glyph;
    [SerializeField] private Sprite emptySprite, lockedSprite, runeSprite;
    [SerializeField] private TMP_Text socketText;
    [SerializeField] private TMP_Text stateText;

    [NonSerialized] public int index;

    public event Action<RuneSocketRowView> Clicked;
    public event Action<RuneSocketRowView> RemoveClicked;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke(this));
        removeButton.onClick.AddListener(() => RemoveClicked?.Invoke(this));
    }

    public void Bind(SocketRowState state, string title, string detail, Color iconColor, string iconLabel, string cost, bool selected)
    {
        group.alpha = state == SocketRowState.Locked ? 0.55f : 1f;
        frame.color = selected ? SelectedBorder : Border;
        icon.color = state == SocketRowState.Filled ? iconColor : EmptyIcon;
        iconText.text = iconLabel;
        if (socketText != null) socketText.text = InventoryText.Get("SOCKET_HEADER", index + 1);
        if (stateText != null) stateText.gameObject.SetActive(false);
        if (glyph != null)
        {
            glyph.sprite = state == SocketRowState.Locked ? lockedSprite : state == SocketRowState.Empty ? emptySprite : runeSprite;
            glyph.enabled = glyph.sprite != null;
            glyph.color = Color.white;
        }
        nameText.text = title;
        detailText.text = detail;

        removeButton.gameObject.SetActive(state == SocketRowState.Filled);
        costText.gameObject.SetActive(state == SocketRowState.Filled);
        costText.text = cost;
        SetRemoveArmed(false);
    }

    public void SetRuneVisual(Sprite sprite, bool active)
    {
        if (glyph != null) { glyph.sprite = sprite != null ? sprite : runeSprite; glyph.enabled = glyph.sprite != null; }
        if (stateText != null)
        {
            stateText.gameObject.SetActive(true);
            stateText.text = InventoryText.Get(active ? "POPUP_STATE_ACTIVE" : "POPUP_STATE_INACTIVE");
            stateText.color = active ? new Color(.2f,.85f,.95f) : new Color(1f,.74f,.25f);
        }
    }

    public void SetRemoveArmed(bool armed) => removeLabel.text = InventoryText.Get(armed ? "POPUP_CONFIRM" : "POPUP_REMOVE");
}
