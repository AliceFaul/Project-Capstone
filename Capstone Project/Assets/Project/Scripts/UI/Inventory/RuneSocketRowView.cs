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
        nameText.text = title;
        detailText.text = detail;

        removeButton.gameObject.SetActive(state == SocketRowState.Filled);
        costText.gameObject.SetActive(state == SocketRowState.Filled);
        costText.text = cost;
        SetRemoveArmed(false);
    }

    public void SetRemoveArmed(bool armed) => removeLabel.text = InventoryText.Get(armed ? "POPUP_CONFIRM" : "POPUP_REMOVE");
}
