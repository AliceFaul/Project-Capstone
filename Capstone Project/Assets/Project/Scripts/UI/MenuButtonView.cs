using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text subLabel;
    [SerializeField] private Color normalBackground = new Color(0.027f, 0.082f, 0.118f, 0.7f);
    [SerializeField] private Color selectedBackground = new Color(0.94f, 0.50f, 0.16f, 1f);
    [SerializeField] private Color normalLabel = new Color(0.91f, 0.94f, 0.96f, 1f);
    [SerializeField] private Color selectedLabel = new Color(0.04f, 0.086f, 0.125f, 1f);

    private void OnEnable() => Apply(false);

    private void Apply(bool selected)
    {
        if (background != null) background.color = selected ? selectedBackground : normalBackground;
        if (label != null) label.color = selected ? selectedLabel : normalLabel;
        if (subLabel != null) subLabel.color = selected ? selectedLabel : new Color(0.62f, 0.71f, 0.75f, 1f);
    }

    public void OnSelect(BaseEventData eventData) => Apply(true);
    public void OnDeselect(BaseEventData eventData) => Apply(false);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
    }
}