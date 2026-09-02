using UnityEngine.Localization;

public interface ILoading
{
    void Show();
    void Hide();
    void SetProgress(float progress, LocalizedString message = null);
    void SetMessage(LocalizedString message);
}