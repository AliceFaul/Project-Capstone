using UnityEngine.Localization;

public interface ILoading
{
    void Show();
    void Hide();
    void ShowProgressBar();
    void HideProgressBar();
    void SetProgress(float progress, LocalizedString message = null);
    void SetMessage(LocalizedString message);
}