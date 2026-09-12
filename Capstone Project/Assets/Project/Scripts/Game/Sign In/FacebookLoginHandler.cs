using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public class FacebookLoginHandler : MonoBehaviour
{
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        if (!FB.IsInitialized) FB.Init(InitCallback, OnHideUnity);
        else FB.ActivateApp();
        if(loginButton == null) loginButton = GetComponent<Button>();
        loginButton?.onClick.AddListener(FacebookLogin);
    }

    private void InitCallback()
    {
        if (FB.IsInitialized) FB.ActivateApp();
        else Debug.LogError("[FacebookLoginHandler] Failed to initialize the Facebook SDK. Check logs for details.");
    }

    private void OnHideUnity(bool isGameShown) => Time.timeScale = isGameShown ? 1 : 0;

    private void AuthCallback(ILoginResult result)
    {
        if(loginButton != null) loginButton.interactable = true;
        if (FB.IsLoggedIn && result != null && !string.IsNullOrEmpty(result.AccessToken?.TokenString))
        {
            string aToken = result.AccessToken?.TokenString;
            Debug.Log($"[FacebookLoginHandler] Facebook login successfully {aToken}");
            _ = HandleLoginAsync(aToken);
        }
        else
        {
            Debug.LogError($"[FacebookLoginHandler] FB login cancelled or failed: {result?.Error}");
        }
    }

    private async Task HandleLoginAsync(string aToken)
    {
        AuthUIHandler.Instance.SetLoadingState(true);

        try
        {
            var authService = StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>();
            bool success = await authService.FacebookLogin(aToken);
            if (success) AuthUIHandler.Instance.OnAuthSuccess();
            else AuthUIHandler.Instance.SetLoadingState(false);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FacebookLoginHandler] Login exception: {e.Message}");
            AuthUIHandler.Instance.SetLoadingState(false);
        }
    }

    private void FacebookLogin()
    {
        if(!FB.IsInitialized) return;
        if(loginButton != null) loginButton.interactable = false; // Avoid duplicate request
        var perm = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perm, AuthCallback);
    }
}