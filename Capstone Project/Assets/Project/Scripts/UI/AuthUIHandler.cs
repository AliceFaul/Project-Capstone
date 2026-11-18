using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization; // Đảm bảo dự án đã cài gói Localization

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

    [Header("System Popup Configuration")]
    [SerializeField] private string errorPopupPrefabId = "DefaultErrorPopup";
    [SerializeField] private LocalizedString loginFailedLocalizedString = new LocalizedString("UI", "LOGIN_FAILED_CONTENT");
    [SerializeField] private LocalizedString loadCloudFailedLocalizedString = new LocalizedString("UI", "load_cloud_failed_content");
    [SerializeField] private LocalizedString saveCloudFailedLocalizedString = new LocalizedString("UI", "save_cloud_failed_content");
    [SerializeField] private LocalizedString localSaveFailedLocalizedString = new LocalizedString("UI", "local_save_failed_content");
    [SerializeField] private LocalizedString autoSaveSuccessLocalizedString = new LocalizedString("UI", "AUTO_SAVE_SUCCESS_CONTENT");

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

    private void OnEnable()
    {
        EventManager.Instance?.AddListener("ON_AUTH_FAILED", ShowLoginFailedPopup);
        EventManager.Instance?.AddListener("ON_LOAD_CLOUD_FAILED", ShowLoadCloudFailedPopup);
        EventManager.Instance?.AddListener("ON_SAVE_CLOUD_FAILED", ShowSaveCloudFailedPopup);
        EventManager.Instance?.AddListener("ON_LOCAL_SAVE_FAILED", ShowLocalSaveFailedPopup);
        EventManager.Instance?.AddListener("ON_AUTO_SAVE_SUCCESS", HandleAutoSaveSuccess);
    }

    private void OnDisable()
    {
        EventManager.Instance?.RemoveListener("ON_AUTH_FAILED", ShowLoginFailedPopup);
        EventManager.Instance?.RemoveListener("ON_LOAD_CLOUD_FAILED", ShowLoadCloudFailedPopup);
        EventManager.Instance?.RemoveListener("ON_SAVE_CLOUD_FAILED", ShowSaveCloudFailedPopup);
        EventManager.Instance?.RemoveListener("ON_LOCAL_SAVE_FAILED", ShowLocalSaveFailedPopup);
        EventManager.Instance?.RemoveListener("ON_AUTO_SAVE_SUCCESS", HandleAutoSaveSuccess);
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
            ShowLoginFailedPopup();
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
            
            if (StartupProcessor.Instance != null && StartupProcessor.Instance.GetService(out SaveService saveService))
            {
                saveService.Dispose();
                Debug.Log("[AuthUIHandler] Auto-save loop disposed clean on logout.");
            }
        }
        catch(Exception e) { Debug.LogError($"[AuthUIHandler] Logout failed: {e.Message}"); }
        
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

    private void ShowLoginFailedPopup()
    {
        if(UIManager.Instance == null) return;
        UIManager.Instance.GetPopupService().Create(errorPopupPrefabId, "popup_login_failed", loginFailedLocalizedString, () => true);
    }

    private void ShowLoadCloudFailedPopup()
    {
        if(UIManager.Instance == null) return;
        UIManager.Instance.GetPopupService().Create(errorPopupPrefabId, "popup_load_cloud_failed", loadCloudFailedLocalizedString, () => true);
    }

    private void ShowSaveCloudFailedPopup()
    {
        if(UIManager.Instance == null) return;
        UIManager.Instance.GetPopupService().Create(errorPopupPrefabId, "popup_save_cloud_failed", saveCloudFailedLocalizedString, () => true);
    }

    private void ShowLocalSaveFailedPopup()
    {
        if(UIManager.Instance == null) return;
        UIManager.Instance.GetPopupService().Create(errorPopupPrefabId, "popup_local_save_failed", localSaveFailedLocalizedString, () => true);
    }

    private void HandleAutoSaveSuccess()
    {
        if(UIManager.Instance == null) return; 

        UIManager.Instance.GetFloatingTextService().Create(
            "AutoSaveText", 
            "toast_autosave_" + DateTime.UtcNow.Ticks, 
            autoSaveSuccessLocalizedString
        );
    }
}
