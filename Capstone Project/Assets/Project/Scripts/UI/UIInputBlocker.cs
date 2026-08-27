using UnityEngine;
using UnityEngine.UIElements;

public static class UIInputBlocker
{
    public static bool IsPointerOverUI(Vector2 screenPosition)
    {
        var documents = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        foreach (var doc in documents)
        {
            if (doc == null || !doc.isActiveAndEnabled)
                continue;

            var panel = doc.rootVisualElement?.panel;
            if (panel == null)
                continue;

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            VisualElement picked = panel.Pick(panelPosition);

            if (picked != null)
                return true;
        }

        return false;
    }
}
