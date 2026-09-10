using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using UnityEngine.UI;
using System;

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

    private async void AuthCallback(ILoginResult result)
    {
        try
        {
            if (FB.IsLoggedIn)
            {
                var aToken = AccessToken.CurrentAccessToken.TokenString;
                Debug.Log($"[FacebookLoginHandler] Logged in as {aToken}");

                try
                {
                    await StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>()
                        .FacebookLogin(aToken).ContinueWith(playFabTask =>
                        {
                            if (playFabTask.IsFaulted || playFabTask.IsCanceled)
                                Debug.LogError($"[FacebookLoginHandler] Login failed: {playFabTask.Exception}");
                            else
                                Debug.Log($"[FacebookLoginHandler] Successfully logged in.");
                        });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FacebookLoginHandler] Exception: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"[FacebookLoginHandler] User cancelled login or error: {result.Error}.");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void FacebookLogin()
    {
        var perm = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perm, AuthCallback);
    }
}