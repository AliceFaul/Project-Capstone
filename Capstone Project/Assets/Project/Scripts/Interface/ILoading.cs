public interface ILoading
{
    void Show();
    void Hide();
    void SetProgress(float progress, string message = null);
}