using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UiSfx { None, Click, Hover, PopupOpen, PopupClose, Confirm, Cancel, Error }

[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public class UiSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private UiSfx clickSound = UiSfx.Click;
    [SerializeField] private UiSfx hoverSound = UiSfx.Hover;
    
    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData) => Play(hoverSound);

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left) Play(clickSound);
    }

    // Keyboard / gamepad "submit" on a focused button.
    public void OnSubmit(BaseEventData eventData) => Play(clickSound);

    private void Play(UiSfx sfx)
    {
        if(sfx == UiSfx.None) return;
        if(_selectable == null || !_selectable.IsInteractable()) return;
        AudioService.Instance?.PlayUi(sfx);
    }

    // Which selectables get sounds by default. Sliders, scrollbars and input fields are left out on purpose.
    public static bool IsSoundTarget(Selectable selectable) => selectable is Button or Toggle or Dropdown or TMP_Dropdown;

    // Adds UiSound to every sound target under 'root' (inactive ones includes) that does not have one yet.
    public static void AttachAll(GameObject root)
    {
        if(root == null) return;

        foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            if(IsSoundTarget(selectable) && !selectable.TryGetComponent<UiSound>(out _))
                selectable.gameObject.AddComponent<UiSound>();
        }
    }
}