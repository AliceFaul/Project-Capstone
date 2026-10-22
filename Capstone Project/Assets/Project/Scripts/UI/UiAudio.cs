using UnityEngine.UIElements;

public static class UiAudio
{
    public const string SilentClass = "ui-silent";

    private static readonly EventCallback<ClickEvent> s_onClick = OnClick;

    public static void Attach(VisualElement root)
    {
        // Registering the same callback twice on the same element is ignored by UI Toolkit.
        root?.RegisterCallback(s_onClick, TrickleDown.TrickleDown);
    }

    public static void Detach(VisualElement root)
    {
        root?.UnregisterCallback(s_onClick, TrickleDown.TrickleDown);
    }

    private static void OnClick(ClickEvent evt)
    {
        var element = evt.target as VisualElement;
        var root = evt.currentTarget as VisualElement;

        // The click may land on a child of a Button, so walk up until we find one.
        while (element != null)
        {
            if (element is Button button)
            {
                if (button.enabledInHierarchy && !button.ClassListContains(SilentClass))
                    AudioService.Instance?.PlayUiClick();
                return;
            }

            if (element == root) return;
            element = element.parent;
        }
    }
}