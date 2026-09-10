using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using UnityEngine.UI;
using Firebase.Extensions;

public class FacebookLoginHandler : MonoBehaviour
{
    [SerializeField] private Button loginButton;
    private Firebase.Auth.FirebaseAuth _auth;

    private void Awake()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(InitCallback, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }
        
        if(loginButton == null)
            loginButton = GetComponent<Button>();
        
        loginButton?.onClick.AddListener(FacebookLogin);
    }

    private void Start()
    {
        _auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            Debug.LogError("[FacebookLoginHandler] Failed to initialize the Facebook SDK. Check logs for details.");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Time.timeScale = isGameShown ? 1 : 0;
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            var aToken = AccessToken.CurrentAccessToken;
            FacebookAuth(aToken.TokenString);
        }
        else
        {
            Debug.Log($"[FacebookLoginHandler] User cancelled login.");
        }
    }

    private void FacebookAuth(string aToken)
    {
        var credential = Firebase.Auth.FacebookAuthProvider.GetCredential(aToken);
        _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError($"[FacebookLoginHandler] User cancelled login.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"[FacebookLoginHandler] Error: {task.Exception}");
                return;
            }
            
            var user = task.Result;
            Debug.Log($"[FacebookLoginHandler] Successfully logged in as {user.DisplayName} ({user.UserId})");
            
            StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().FacebookLogin(aToken).ContinueWith(playFabTask =>
            {
                if(playFabTask.IsFaulted || playFabTask.IsCanceled)
                    Debug.LogError($"[FacebookLoginHandler] Login failed: {playFabTask.Exception}");
                else
                    Debug.Log($"[FacebookLoginHandler] Successfully logged in.");
            });
        });
    }

    private void FacebookLogin()
    {
        var perm = new List<string>() { "public_profile", "email" };
        FB.LogInWithReadPermissions(perm, AuthCallback);
    }
}