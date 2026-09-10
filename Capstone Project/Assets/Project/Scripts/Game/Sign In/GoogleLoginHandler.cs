using System;
using System.Threading.Tasks;
using Google;
using UnityEngine;
using UnityEngine.UI;

public class GoogleLoginHandler : MonoBehaviour
{
    [SerializeField] private Button loginButton;

    private void Awake()
    {
        if(loginButton == null) loginButton = GetComponent<Button>();
        loginButton?.onClick.AddListener(GoogleLogin);
    }

    private async void GoogleLogin()
    {
        try
        {
            string authCode = await GetGoogleAuthCode();
        
            if (string.IsNullOrEmpty(authCode))
            {
                Debug.LogError($"[GoogleLoginHandler] Can't get auth code from Google SDK.");
                return;
            }
        
            await StartupProcessor.Instance.GetService<PlayFabServiceManager>().GetService<PlayFabAuthentication>().GoogleLogin(authCode).ContinueWith(playFabTask =>
            {
                if (playFabTask.IsFaulted || playFabTask.IsCanceled)
                    Debug.LogError($"[GoogleLoginHandler] Login failed: {playFabTask.Exception}");
                else
                    Debug.Log($"[GoogleLoginHandler] Login succeeded: {playFabTask.Result}");
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[GoogleLoginHandler] User cancelled login or error: {e.Message}");
        }
    }

    private Task<string> GetGoogleAuthCode()
    {
        var tcs = new TaskCompletionSource<string>();
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[EmailLoginHandler] Login failed: {task.Exception}");
                tcs.SetResult(null);
            }
            else
            {
                tcs.TrySetResult(task.Result.AuthCode);
            }
        });
        
        return tcs.Task;
    }
}