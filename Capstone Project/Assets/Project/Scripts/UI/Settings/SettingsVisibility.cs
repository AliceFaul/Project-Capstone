using UnityEngine;
using UnityEngine.UI;

public class SettingsVisibility : MonoBehaviour
{
    public enum Setting { ShowFps }

    [SerializeField] private Setting setting = Setting.ShowFps;
    
    [Tooltip("Must not be this object itself (an inactive object cannot listen).")]
    [SerializeField] private GameObject target;

    private void OnEnable()
    {
        GameSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => GameSettings.Changed -= Refresh;

    private void Refresh()
    {
        bool visible = setting == Setting.ShowFps && GameSettings.ShowFps;

        if (target != null && target != gameObject) target.SetActive(visible);
        else if (TryGetComponent(out Graphic graphic)) graphic.enabled = visible;
    }
}