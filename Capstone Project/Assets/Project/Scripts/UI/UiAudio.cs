using UnityEngine.UIElements;

public static class UiAudio
{
    public const string SilentClass = "ui-silent";
    public const string HoverClass = "ui-hover";

    private static readonly EventCallback<ClickEvent> s_onClick = OnClick;
    private static readonly EventCallback<PointerOverEvent> s_onOver = OnPointerOver;
    private static readonly EventCallback<PointerLeaveEvent> s_onLeave = OnPointerLeave;

    private static VisualElement s_hovered;

    public static void Attach(VisualElement root)
    {
        if (root == null) return;

        // Registering the same callback twice on the same element is ignored by UI Toolkit.
        root.RegisterCallback(s_onClick, TrickleDown.TrickleDown);
        // PointerOver (unlike PointerEnter) bubbles/trickles, so one callback on the root sees every element.
        root.RegisterCallback(s_onOver, TrickleDown.TrickleDown);
        root.RegisterCallback(s_onLeave);
    }

    public static void Detach(VisualElement root)
    {
        if (root == null) return;

        root.UnregisterCallback(s_onClick, TrickleDown.TrickleDown);
        root.UnregisterCallback(s_onOver, TrickleDown.TrickleDown);
        root.UnregisterCallback(s_onLeave);
        s_hovered = null;
    }

    private static void OnClick(ClickEvent evt)
    {
        VisualElement target = FindTarget(evt.target as VisualElement, evt.currentTarget as VisualElement, false);
        if (target is { enabledInHierarchy: true }) AudioService.Instance?.PlayUiClick();
    }

    private static void OnPointerOver(PointerOverEvent evt)
    {
        VisualElement target = FindTarget(evt.target as VisualElement, evt.currentTarget as VisualElement, true);
        if (target == s_hovered) return;

        s_hovered = target;
        if (target is { enabledInHierarchy: true }) AudioService.Instance?.PlayUiHover();
    }

    private static void OnPointerLeave(PointerLeaveEvent evt)
    {
        // Pointer left the whole root: allow the same element to play hover again next time.
        s_hovered = null;
    }

    // The pointer may be over a child of a Button, so walk up until we find one (or ui-hover element).
    private static VisualElement FindTarget(VisualElement start, VisualElement root, bool allowHoverClass)
    {
        for (VisualElement e = start; e != null; e = e.parent)
        {
            if (e is Button || (allowHoverClass && e.ClassListContains(HoverClass)))
                return e.ClassListContains(SilentClass) ? null : e;

            if (e == root) break;
        }

        return null;
    }
}