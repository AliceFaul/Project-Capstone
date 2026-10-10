using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AuthUIHandler : MonoBehaviour
{
    public static AuthUIHandler Instance { get; private set; }
    
    [Header("Main Menu Controller")]
    [SerializeField] private MainMenu mainMenu;
    
    [Header("Sign in UI Provider")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject authPanel;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loadingOverlay;

    private TaskCompletionSource<bool> _tcs;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if(authPanel != null) authPanel.SetActive(false);
        SetLoadingState(false);
    }

    private void Start()
    {
        if(mainMenu == null) mainMenu = FindFirstObjectByType<MainMenu>();
        if(canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _tcs?.TrySetResult(false);
    }

    /// <summary>
    /// Call when Startup progress completed and try to log-in
    /// </summary>
    public async Task<bool> TryLogin()
    {
        _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        
        SetLoadingState(true);
        bool autoLogin = await TryAutoLogin(_cts.Token);

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
        EventManager.Instance?.Trigger("ON_AUTH_SUCCESS");

        if (_tcs != null && !_tcs.Task.IsCompleted) _tcs.TrySetResult(true);
        else _ = StartupProcessor.Instance?.StartWorkflowAfterAuth();
    }

    private async Task<bool> TryAutoLogin(CancellationToken ct)
    {
        if (CryptoUtils.TryLoadCredentials(out string email, out string password))
        {
            Debug.Log($"[AuthUIHandler] Found saved credentials: {email}, {password}. Attempting to login...");

            try
            {
                return await StartupProcessor.Instance.GetService<PlayFabServiceManager>()
                    .GetService<PlayFabAuthentication>().EmailLogin(email, password, ct);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthUIHandler] Auto-Login exception: {e.Message}");
                return false;
            }
        }
        
        return false;
    }

    [ContextMenu("Log out")]
    public void OnUserLogout()
    {
        try
        {
            StartupProcessor.Instance?.EnableUIInput();
            _ = StartupProcessor.Instance?.ResetBlur();
            StartupProcessor.Instance?.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().Logout();

            if (authPanel != null) authPanel.SetActive(true);
            if (loginPanel != null) loginPanel.SetActive(true);
            if (registerPanel != null) registerPanel.SetActive(false);

            SetLoadingState(false);
            Debug.Log("[AuthUIHandler] Logout completed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthUIHandler] Logout failed: {e.Message}");
        }
        
        CryptoUtils.ClearCredentials();
        if(mainMenu == null) mainMenu = FindFirstObjectByType<MainMenu>();
        
        if(mainMenu != null) mainMenu.ReturnToSignIn();
        else Debug.LogWarning("[AuthUIHandler] Main Menu Not Found.");
        
        SetLoadingState(false);
        ShowAuthPanel();
        
        Debug.Log("[AuthUIHandler] Returned to login screen.");
    }

    private void ShowAuthPanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        
        if(authPanel != null) authPanel.SetActive(true);
        if(loginPanel != null) loginPanel.SetActive(true);
        if(registerPanel != null) registerPanel.SetActive(false);
    }

    public void SetLoadingState(bool loading)
    {
        if(loadingOverlay != null) loadingOverlay.SetActive(loading);
    }
}