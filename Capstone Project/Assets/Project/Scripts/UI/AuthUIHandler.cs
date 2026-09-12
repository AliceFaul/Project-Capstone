using System;
using System.Threading.Tasks;
using UnityEngine;

public class AuthUIHandler : MonoBehaviour
{
    public static AuthUIHandler Instance { get; private set; }
    
    [Header("Sign in UI Provider")]
    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loadingOverlay;

    private TaskCompletionSource<bool> _tcs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        if(authPanel != null) authPanel.SetActive(false);
        SetLoadingState(false);
    }

    /// <summary>
    /// Call when Startup progress completed and try to log-in
    /// </summary>
    public async Task<bool> TryLogin()
    {
        _tcs = new TaskCompletionSource<bool>();
        SetLoadingState(true);
        bool autoLogin = await TryAutoLogin();

        if (autoLogin)
        {
            OnAuthSuccess();
        }
        else
        {
            SetLoadingState(false);
            ShowAuthPanel();
        }
        
        return await _tcs.Task;
    }

    public void OnAuthSuccess()
    {
        SetLoadingState(false);
        if(authPanel != null) authPanel.SetActive(false);
        EventManager.Instance.Trigger("ON_AUTH_SUCCESS");
        _tcs.TrySetResult(true);
    }

    private async Task<bool> TryAutoLogin()
    {
        if (PlayerPrefs.HasKey("SAVED_EMAIL") && PlayerPrefs.HasKey("SAVED_PASSWORD"))
        {
            string email = PlayerPrefs.GetString("SAVED_EMAIL");
            string password = PlayerPrefs.GetString("SAVED_PASSWORD");
            Debug.Log($"[AuthUIHandler] Found saved credentials: {email}, {password}. Attempting to login...");

            try
            {
                return await StartupProcessor.Instance.GetService<PlayFabServiceManager>()
                    .GetService<PlayFabAuthentication>().EmailLogin(email, password);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthUIHandler] Auto-Login exception: {e.Message}");
                return false;
            }
        }
        
        return false;
    }

    private void ShowAuthPanel()
    {
        if(authPanel != null) authPanel.SetActive(true);
        if(loginPanel != null) loginPanel.SetActive(true);
        if(registerPanel != null) registerPanel.SetActive(false);
    }

    public void SetLoadingState(bool loading)
    {
        if(loadingOverlay != null) loadingOverlay.SetActive(loading);
    }
}