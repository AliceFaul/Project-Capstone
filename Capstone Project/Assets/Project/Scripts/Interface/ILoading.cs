using UnityEngine.Localization;
using System.Threading.Tasks;

public interface ILoading
{
    Task Show();
    Task Hide();
    Task ShowProgressBar();
    Task HideProgressBar();
    void SetProgress(float progress, LocalizedString message = null);
    void SetMessage(LocalizedString message);
}