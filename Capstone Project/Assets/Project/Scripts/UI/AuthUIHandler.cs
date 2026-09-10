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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        try
        {
            await TryAutoLogin();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task TryAutoLogin()
    {
        if (PlayerPrefs.HasKey("SAVED_EMAIL") && PlayerPrefs.HasKey("SAVED_PASSWORD"))
        {
            string email = PlayerPrefs.GetString("SAVED_EMAIL");
            string password = PlayerPrefs.GetString("SAVED_PASSWORD");
            
            SetLoadingState(true);
            Debug.Log($"[AuthUIHandler] Found saved credentials: {email}, {password}. Attempting to login...");

            try
            {
                await StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().EmailLogin(email, password).ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                        Debug.LogError($"[AuthUIHandler] Auto-Login failed: {task.Exception}");
                    else
                        Debug.Log($"[AuthUIHandler] Auto-Login succeeded: {task.IsCompleted}");
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthUIHandler] Auto-Login exception: {e.Message}");
            }
            
            SetLoadingState(false);
        }
    }

    public void SetLoadingState(bool loading)
    {
        if(loadingOverlay != null) loadingOverlay.SetActive(loading);
    }
}